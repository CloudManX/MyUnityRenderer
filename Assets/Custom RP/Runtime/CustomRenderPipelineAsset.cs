using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset 
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    [SerializeField]
    ShadowSettings shadowSettings = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            useDynamicBatching, useGPUInstancing, useSRPBatcher, shadowSettings    
        );
    }
}

public partial class CustomRenderPipeline : RenderPipeline 
{
    CameraRenderer renderer = new CameraRenderer();

    bool useDynamicBatching, useGPUInstancing;
    ShadowSettings shadowSettings;

    public CustomRenderPipeline(
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        ShadowSettings shadowSettings
    )
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
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
                context, cam, useDynamicBatching, useGPUInstancing,
                shadowSettings
            );
        }
    }
}

