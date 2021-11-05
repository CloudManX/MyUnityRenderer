using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    CullingResults cullingResults;
    Camera camera;

    const string defaultBufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer {
        name = defaultBufferName
    };

    static ShaderTagId
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit");

    // Lights
    Lighting lighting = new Lighting(); // Customly Defined Lighting Settings

    PostFXStack postFXStack = new PostFXStack();

    // static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    static int
        cameraBufferSizeId = Shader.PropertyToID("_CameraBufferSize"),
        colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment"),
        depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment"),
        colorTextureId = Shader.PropertyToID("_CameraColorTexture"),
        depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
        sourceTextureId = Shader.PropertyToID("_SourceTexture");

    bool useHDR, useScaledRendering;
    bool useColorTexture, useDepthTexture, useIntermediateBuffer;

    Material material;
    Texture2D missingTexture;

    Vector2Int cameraBufferSize;

    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }

    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        CameraBufferSettings bufferSettings,
        bool useDyanmicBatching,
        bool useGPUInstancing,
        bool useLightPerObject,
        ShadowSettings shadowSettings,
        PostFXSettings postFXSettings,
        int colorLUTResolution
    )
    {
        this.context = context;
        this.camera = camera;

        float renderScale = bufferSettings.renderScale;
        useScaledRendering = renderScale < 0.99f || renderScale > 1.01f;

        // Pipeline Preparation
        PrepareBuffer(); 

        // Scene Window Preparation
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        if (camera.cameraType == CameraType.Reflection)
        {
            useColorTexture = bufferSettings.copyColorReflection;
            useDepthTexture = bufferSettings.copyDepthReflection;
        }
        else
        {
            useColorTexture = bufferSettings.copyColor;
            useDepthTexture = bufferSettings.copyDepth;
        }

        useHDR = bufferSettings.allowHDR && camera.allowHDR;
        if (useScaledRendering)
        {
            cameraBufferSize.x = (int)(camera.pixelWidth * renderScale);
            cameraBufferSize.y = (int)(camera.pixelHeight * renderScale);
        }
        else
        {
            cameraBufferSize.x = camera.pixelWidth;
            cameraBufferSize.y = camera.pixelHeight;
        }

        buffer.BeginSample(SampleName);
        buffer.SetGlobalVector(cameraBufferSizeId, new Vector4(
            1f / cameraBufferSize.x, 1f / cameraBufferSize.y,
            cameraBufferSize.x, cameraBufferSize.y
        ));
        ExecuteBuffer();
        lighting.Setup(
            context, cullingResults, shadowSettings, useLightPerObject
        );
        postFXStack.Setup(
            context, camera, cameraBufferSize, postFXSettings, useHDR, 
            colorLUTResolution, bufferSettings.bicubicRescaling
        );
        buffer.EndSample(SampleName);

        Setup();

        DrawVisibleGeometry(
            useDyanmicBatching, useGPUInstancing, useLightPerObject
        );

        DrawUnsupportedShaders();
        DrawGizmosBeforeFX();
        if (postFXStack.IsActive)
        {
            postFXStack.Render(colorAttachmentId);
        }
        else if (useIntermediateBuffer)
        {
            Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }
        DrawGizmosAfterFX();
        Cleanup();

        Submit();
    }

    void Draw(
        RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false
    )
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(
            to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        buffer.DrawProcedural(
            Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3
        );
    }

    void DrawVisibleGeometry(
        bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject
    )
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ?
            PerObjectData.LightData | PerObjectData.LightIndices :
            PerObjectData.None;
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        
        // Unlit/Lit Passes
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        )
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            // GI Per Object Data
            perObjectData = 
                PerObjectData.ReflectionProbes | 
                PerObjectData.Lightmaps | 
                PerObjectData.ShadowMask |
                PerObjectData.LightProbe |
                PerObjectData.OcclusionProbe |
                PerObjectData.LightProbeProxyVolume |
                PerObjectData.OcclusionProbeProxyVolume |
                lightsPerObjectFlags
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        // Skybox
        context.DrawSkybox(camera);

        if (useColorTexture || useDepthTexture)
        {   
            CopyAttachments();
        }

        // Draw Transparent Objects after Skybox
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    static bool copyTextureSupported = 
        SystemInfo.copyTextureSupport > CopyTextureSupport.None;

    void CopyAttachments()
    {
        if (useColorTexture)
        {
            buffer.GetTemporaryRT(
                colorTextureId, cameraBufferSize.x, cameraBufferSize.y,
                0, FilterMode.Bilinear, useHDR ? 
                    RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            if (copyTextureSupported)
            {
                buffer.CopyTexture(colorAttachmentId, colorTextureId);
            }
            else
            {
                Draw(colorAttachmentId, colorTextureId);
            }
        }
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(
                depthTextureId, cameraBufferSize.x, cameraBufferSize.y,
                32, FilterMode.Point, RenderTextureFormat.Depth
            );
            if (copyTextureSupported)
            {
                buffer.CopyTexture(depthAttachmentId, depthTextureId);
            }
            else
            {
                Draw(depthAttachmentId, depthTextureId, true);
            }
            buffer.CopyTexture(depthAttachmentId, depthTextureId);
        }    
        if (!copyTextureSupported)
        {

            buffer.SetRenderTarget(
			    colorAttachmentId,
			    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
			    depthAttachmentId,
				RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
		    );
        }
        ExecuteBuffer();
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;

        useIntermediateBuffer = useScaledRendering ||
            useColorTexture || useDepthTexture || postFXStack.IsActive;
        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(
                colorAttachmentId, cameraBufferSize.x, cameraBufferSize.y,
                0, FilterMode.Bilinear, useHDR ? 
                    RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            buffer.GetTemporaryRT(
                depthAttachmentId, cameraBufferSize.x, cameraBufferSize.y,
                32, FilterMode.Point, RenderTextureFormat.Depth
            );
            buffer.SetRenderTarget(
                colorAttachmentId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthAttachmentId,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
        }

        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear
        );
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(colorTextureId, missingTexture);
        buffer.SetGlobalTexture(depthTextureId, missingTexture);
        ExecuteBuffer();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    void Cleanup()
    {
        lighting.Cleanup();
        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);
            if (useColorTexture)
            {
                buffer.ReleaseTemporaryRT(colorTextureId);
            }
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
        }
    }
}
