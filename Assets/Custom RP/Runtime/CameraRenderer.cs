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

    static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

    bool useHDR;

    public void Render(
        ScriptableRenderContext context,
        Camera camera,
        bool allowHDR,
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
        
        // Pipeline Preparation
        PrepareBuffer(); 

        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }
        useHDR = allowHDR && camera.allowHDR;

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        lighting.Setup(
            context, cullingResults, shadowSettings, useLightPerObject
        );
        postFXStack.Setup(
            context, camera, postFXSettings, useHDR, colorLUTResolution
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
            postFXStack.Render(frameBufferId);
        }
        DrawGizmosAfterFX();
        Cleanup();

        Submit();
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

        // Draw Transparent Objects after Skybox
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
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

        if (postFXStack.IsActive)
        {
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
            buffer.GetTemporaryRT(
                frameBufferId, camera.pixelWidth, camera.pixelHeight,
                32, FilterMode.Bilinear, useHDR ? 
                    RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            buffer.SetRenderTarget(
                frameBufferId,
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
        if (postFXStack.IsActive)
        {
            buffer.ReleaseTemporaryRT(frameBufferId);
        }
    }
}
