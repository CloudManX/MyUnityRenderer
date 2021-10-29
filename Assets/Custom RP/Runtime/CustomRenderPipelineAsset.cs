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

    [SerializeField]
    bool allowHDR = true;

    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64}

    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            allowHDR,
            useDynamicBatching, useGPUInstancing, useSRPBatcher, 
            useLightsPerObject, shadowSettings, postFXSettings,
            (int)colorLUTResolution
        );
    }
}

public partial class CustomRenderPipeline : RenderPipeline 
{
    CameraRenderer renderer = new CameraRenderer();

    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSettings;

    PostFXSettings postFXSettings;

    bool allowHDR;

    int colorLUTResolution;

    public CustomRenderPipeline(
        bool allowHDR,
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        bool useLightsPerObject, ShadowSettings shadowSettings,
        PostFXSettings postFXSettings,
        int colorLUTResolution
    )
    {
        this.allowHDR = allowHDR;
        this.shadowSettings = shadowSettings;
        this.postFXSettings = postFXSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.useLightsPerObject = useLightsPerObject;
        this.colorLUTResolution = colorLUTResolution; 
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
                context, cam, allowHDR,
                useDynamicBatching, useGPUInstancing, useLightsPerObject,
                shadowSettings, postFXSettings, colorLUTResolution
            );
        }
    }
}

