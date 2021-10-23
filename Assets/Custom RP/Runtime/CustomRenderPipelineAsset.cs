using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset 
{
    [SerializeField]
    bool useDynamicBatching = true, 
        useGPUInstancing = true, 
        useSRPBatcher = true,
        useLightsPerObject = true;

    [SerializeField]
    ShadowSettings shadowSettings = default;

    [SerializeField]
    PostFXSettings postFXSettings = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            useDynamicBatching, useGPUInstancing, useSRPBatcher, 
            useLightsPerObject, shadowSettings, postFXSettings   
        );
    }
}

public partial class CustomRenderPipeline : RenderPipeline 
{
    CameraRenderer renderer = new CameraRenderer();

    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSettings;

    PostFXSettings postFXSettings;

    public CustomRenderPipeline(
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        bool useLightsPerObject, ShadowSettings shadowSettings,
        PostFXSettings postFXSettings
    )
    {
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        
        InitializeForEditor();
    }

    protected override void Render(
        ScriptableRenderContext context,
        Camera[] cameras
    )
    {
        foreach (Camera cam in cameras)
        {
            renderer.Render(
                context, cam, 
                useDynamicBatching, useGPUInstancing, useLightsPerObject,
                shadowSettings,
                postFXSettings
            );
        }
    }
}

