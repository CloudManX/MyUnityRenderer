using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack
{
    const string bufferName = "Post FX";
    int
        bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
        bloomResultId = Shader.PropertyToID("_BloomResult"),
        bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
        bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
        fxSourceId = Shader.PropertyToID("_PostFXSource"),
        fxSourceAuxId = Shader.PropertyToID("_PostFXSourceAuxiliary"),
        colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
        colorFilterId = Shader.PropertyToID("_ColorFilter"),
        whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),
        splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows"),
        splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights"),
        channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed"),
        channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen"),
        channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue"),
        smhShadowsId = Shader.PropertyToID("_SMHShadows"),
        smhMidtonesId = Shader.PropertyToID("_SMHMidtones"),
        smhHighlightsId = Shader.PropertyToID("_SMHHighlights"),
        smhRangeId = Shader.PropertyToID("_SMHRange"),
        colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT"),
        colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
        colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC"),
        finalSrcBlendId = Shader.PropertyToID("_FinalSrcBlend"),
        finalDstBlendId = Shader.PropertyToID("_FinalDstBlend"),
        colorGradingResultId = Shader.PropertyToID("_ColorGradingResult"),
        finalResultId = Shader.PropertyToID("_FinalResult"),
        copyBicubicId = Shader.PropertyToID("_CopyBicubic"),
        fxaaConfigId = Shader.PropertyToID("_FXAAConfig");


    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    enum Pass
    {
        Copy,
        BloomHorizontal,
        BloomVertical,
        BloomAdd,
        BloomScatter,
        BloomFinal,
        BloomPrefilter,
        BloomPrefilterFadeFireflies,
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        ApplyColorGrading,
        ApplyColorGradingWithLuma,
        FinalRescale,
        FXAA,
        FXAAWithLuma
    }

    ScriptableRenderContext context;

    Camera camera;

    PostFXSettings settings;

    public bool IsActive => settings != null;

    // LDR Blooming
    const int maxBloomPyramidLevels = 16;
    int bloomPyramidId;

    // HDR
    bool useHDR;

    // LUT
    int colorLUTResolution;
    static string lutBandingKeyword = "_LUT_BANDING";

    Vector2Int bufferSize;

    CameraBufferSettings.BicubicRescalingMode bicubicRescaling;

    CameraBufferSettings.FXAA fxaa;
    bool keepAlpha;

    const string
        fxaaQualityLowKeyword = "FXAA_QUALITY_LOW",
        fxaaQualityMediumKeyword = "FXAA_QUALITY_MEDIUM";

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 0; i < maxBloomPyramidLevels * 2; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(
        ScriptableRenderContext context, Camera camera, Vector2Int bufferSize,
        PostFXSettings settings, 
        bool useHDR, int colorLUTResolution,
        CameraBufferSettings .BicubicRescalingMode bicubicRescaling,
        CameraBufferSettings.FXAA fxaa, bool keepAlpha
    )
    {
        this.bufferSize = bufferSize;
        this.useHDR = useHDR;
        this.colorLUTResolution = colorLUTResolution;
        this.context = context;
        this.camera = camera;
        this.settings =
            camera.cameraType <= CameraType.SceneView ? settings : null;
        this.bicubicRescaling = bicubicRescaling;
        this.fxaa = fxaa;
        this.keepAlpha = keepAlpha;

        if (this.settings && this.settings.LUTBanding)
        {
            Shader.EnableKeyword(lutBandingKeyword);
        }
        else
        {
            Shader.DisableKeyword(lutBandingKeyword);
        }

        ApplySceneViewState();
    }

    void ConfigureFXAA () {
		if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Low) {
			buffer.EnableShaderKeyword(fxaaQualityLowKeyword);
			buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
		}
		else if (fxaa.quality == CameraBufferSettings.FXAA.Quality.Medium) {
			buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
			buffer.EnableShaderKeyword(fxaaQualityMediumKeyword);
		}
		else {
			buffer.DisableShaderKeyword(fxaaQualityLowKeyword);
			buffer.DisableShaderKeyword(fxaaQualityMediumKeyword);
		}
		buffer.SetGlobalVector(fxaaConfigId, new Vector4(
			fxaa.fixedThreshold, fxaa.relativeThreshold, fxaa.subpixelBlending
		));
	}

    public void Render(int sourceId)
    {
        // buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        // Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        if (DrawBloom(sourceId))
        {
            ApplyColorGradingAndToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            ApplyColorGradingAndToneMapping(sourceId);
        }
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void Draw(
        RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass
    )
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(
            to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        buffer.DrawProcedural(
            Matrix4x4.identity, settings.Material, (int)pass,
            MeshTopology.Triangles, 3
        );
    }

    void DrawFinal(RenderTargetIdentifier from, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(
            BuiltinRenderTextureType.CameraTarget, 
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        buffer.SetViewport(camera.pixelRect);
        buffer.DrawProcedural(
            Matrix4x4.identity, settings.Material, (int)pass,
            MeshTopology.Triangles, 3
        );
    }

    bool DrawBloom(int sourceId)
    {
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width, height;
        Vector4 thresholdVec;
        float t = Mathf.GammaToLinearSpace(bloom.threshold);
        float tk = t * bloom.thresholdKnee;
        thresholdVec.x = t;
        thresholdVec.y = -t + tk;
        thresholdVec.z = 2f * tk;
        thresholdVec.w = 1f / (4f * tk + 0.00001f);
        buffer.SetGlobalVector(bloomThresholdId, thresholdVec);        

        RenderTextureFormat format = useHDR ? 
            RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        
        // Half Resolution of Blooming, increase rendering speed by reducing bloom loops
        if (bloom.ignoreRenderScale)
        {
            width = camera.pixelWidth / 2;
            height = camera.pixelHeight / 2;
        }
        else
        {
            width = bufferSize.x / 2;
            height = bufferSize.y / 2;
        }
        if (height == 0 || width == 0 ||
            bloom.maxIterations == 0 || 
            bloom.intensity <= 0 ||
            height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2
        )
        {
            return false;
        }

        buffer.BeginSample("Bloom");
        buffer.GetTemporaryRT(
            bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
        );
        Draw(sourceId, bloomPrefilterId, bloom.fadeFireflies ? 
            Pass.BloomPrefilterFadeFireflies : Pass.BloomPrefilter);
        width /= 2;
        height /= 2;

        int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloom.maxIterations; ++i)
        { 
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit) {
				break;
			}

            int midId = toId - 1;
            buffer.GetTemporaryRT(
                midId, width, height, 0, FilterMode.Bilinear, format
            );
            buffer.GetTemporaryRT(
                toId, width, height, 0, FilterMode.Bilinear, format
            );
            
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);

            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }

        buffer.ReleaseTemporaryRT(fromId - 1); 
        toId -= 5; // e.g. when i == 2, toId = 7;
        buffer.SetGlobalFloat(
            bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f
        );
 
        Pass combinePass, finalPass;
        if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomFinal;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
        }

        if (i > 1)
        {
            for (i -= 1; i > 0; --i)
            {
                buffer.SetGlobalTexture(fxSourceAuxId, toId + 1);
                Draw(fromId, toId, combinePass);
                buffer.ReleaseTemporaryRT(fromId); // fxSource
                buffer.ReleaseTemporaryRT(toId + 1); // fxSourceAux
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }

        buffer.SetGlobalFloat(bloomIntensityId, bloom.intensity);
        buffer.SetGlobalTexture(fxSourceAuxId, sourceId);
        buffer.GetTemporaryRT(
            bloomResultId, bufferSize.x, bufferSize.y, 0,
            FilterMode.Bilinear, format
        );
        Draw(fromId, bloomResultId, finalPass);
        buffer.ReleaseTemporaryRT(fromId);
        buffer.ReleaseTemporaryRT(bloomPrefilterId);

        buffer.EndSample("Bloom");
        return true;
    }

    void ConfigureColorAdjustments()
    {
        ColorAdjustmentSettings colorAdjustments = settings.ColorAdjustments;
        buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
            Mathf.Pow(2f, colorAdjustments.postExposure),
            colorAdjustments.constrast * 0.01f + 1f, // range 0-2
            colorAdjustments.hueShift / 360f, // range -1-1
            colorAdjustments.saturation * 0.01f + 1f // range 0-2
        ));

        buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
    }

    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
        buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(
            whiteBalance.temperature, whiteBalance.tint
        ));
    }

    void ConfigureSplitToning()
    {
        SplitToningSettings splitToning = settings.SplitToning;
        Color splitColor = splitToning.shadows;
        splitColor.a = splitToning.balance * 0.01f;
        buffer.SetGlobalColor(splitToningShadowsId, splitColor);
        buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
    }

    void ConfigureChannelMixer()
    {
        ChannelMixerSettings channelMixer = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
        buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
        buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
    }

    void ConfigureShadowsMidtonesHighlights()
    {
        ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
        buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
        buffer.SetGlobalColor(smhRangeId, new Vector4(
            smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
        ));
    }

    void ApplyColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();
        ConfigureChannelMixer();
        ConfigureShadowsMidtonesHighlights();

        int lutHeight = colorLUTResolution;
        int lutWidth = lutHeight * lutHeight;
        buffer.GetTemporaryRT(
            colorGradingLUTId, lutWidth, lutHeight, 0,
            FilterMode.Bilinear, RenderTextureFormat.DefaultHDR
        );
        buffer.BeginSample("Color Grading and Tone Mapping");
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)
        ));

        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass colorGradingPass = Pass.ColorGradingNone + (int)mode;
        buffer.SetGlobalFloat(
            colorGradingLUTInLogId, useHDR && 
                colorGradingPass != Pass.ColorGradingNone ? 1f : 0f
        );

        Draw(sourceId, colorGradingLUTId, colorGradingPass);

        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
            1f / lutWidth, 1f / lutHeight, lutHeight - 1f
        ));

        buffer.SetGlobalFloat(finalSrcBlendId, 1f);
        buffer.SetGlobalFloat(finalDstBlendId, 0f);

        if (fxaa.enabled)
        {
            ConfigureFXAA();
            buffer.GetTemporaryRT(
                colorGradingResultId, bufferSize.x, bufferSize.y, 0,
                FilterMode.Bilinear, RenderTextureFormat.Default
            );
            Draw(
                sourceId, colorGradingResultId, 
                keepAlpha ? Pass.ApplyColorGradingWithLuma : Pass.ApplyColorGrading
            );
            buffer.ReleaseTemporaryRT(colorGradingLUTId);
        }

        if (bufferSize.x == camera.pixelWidth)
        {
            if (fxaa.enabled)
            {
                DrawFinal(
                    colorGradingResultId, 
                    keepAlpha ? Pass.FXAAWithLuma : Pass.FXAA
                );
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                DrawFinal(sourceId, Pass.ApplyColorGrading);
                buffer.ReleaseTemporaryRT(colorGradingLUTId);
            }
        }
        else
        {
            // Rescale
            buffer.GetTemporaryRT(
                finalResultId, bufferSize.x, bufferSize.y, 0,
                FilterMode.Bilinear, RenderTextureFormat.Default
            );

            if (fxaa.enabled)
            {
                Draw(
                    colorGradingResultId, finalResultId, 
                    keepAlpha ? Pass.FXAAWithLuma : Pass.FXAA
                );
                buffer.ReleaseTemporaryRT(colorGradingResultId);
            }
            else
            {
                Draw(sourceId, finalResultId, Pass.ApplyColorGrading);
            }

            bool bicubicSampling =
				bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpAndDown ||
				bicubicRescaling == CameraBufferSettings.BicubicRescalingMode.UpOnly &&
				bufferSize.x < camera.pixelWidth;

            buffer.SetGlobalFloat(copyBicubicId, bicubicSampling ? 1f : 0f);
            DrawFinal(finalResultId, Pass.FinalRescale);
            buffer.ReleaseTemporaryRT(finalResultId);
        }

        buffer.EndSample("Color Grading and Tone Mapping");
    }
}
