using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
public partial class PostFXSettings : ScriptableObject
{
    [SerializeField]
    Shader shader = default;

    [System.NonSerialized]
    Material material;

    public Material Material
    {
        get
        {
            if (material == null && shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }

    // Bloom Settings
    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)]
        public int maxIterations;

        [Min(1f)]
        public int downscaleLimit;

        public bool bicubicUpsampling;

        [Min(0f)]
        public float threshold;

        [Range(0f, 1f)]
        public float thresholdKnee;

        // [Min(0f)]
        [Range(0f, 30f)]
        public float intensity;

        public bool fadeFireflies;

        public enum Mode {Additive, Scattering}

        public Mode mode;

        [Range(0.05f, 0.95f)]
        public float scatter;
    }

    [SerializeField]
    BloomSettings bloom = new BloomSettings
    {
        scatter = 0.7f
    };

    [System.Serializable]

    public struct ToneMappingSettings
    {
        public enum Mode { None, ACES, Neutral, Reinhard }

        public Mode mode;
    }

    [SerializeField]
    ToneMappingSettings toneMapping = default;

    public ToneMappingSettings ToneMapping => toneMapping;

    public BloomSettings Bloom => bloom;
}

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
        splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");

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
        ToneMappingNone,
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard
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

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 0; i < maxBloomPyramidLevels * 2; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(
        ScriptableRenderContext context, Camera camera, PostFXSettings settings, 
        bool useHDR
    )
    {
        this.useHDR = useHDR;
        this.context = context;
        this.camera = camera;
        this.settings =
            camera.cameraType <= CameraType.SceneView ? settings : null;
        ApplySceneViewState();
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

    bool DrawBloom(int sourceId)
    {
        PostFXSettings.BloomSettings bloom = settings.Bloom;

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
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
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
            bloomResultId, camera.pixelWidth, camera.pixelHeight, 0,
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

    void ApplyColorGradingAndToneMapping(int sourceId)
    {
        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitToning();

        ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
        Pass toneMappingPass = Pass.ToneMappingNone + (int)mode; 
        buffer.BeginSample("Tone Mapping");
        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, toneMappingPass);
        buffer.EndSample("Tone Mapping");
    }
}


