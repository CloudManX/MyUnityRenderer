using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]

public class PostFXSettings : ScriptableObject
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
    }

    [SerializeField]
    BloomSettings bloom = default;

    public BloomSettings Bloom => bloom;
}

public partial class PostFXStack
{
    const string bufferName = "Post FX";
    int fxSourceId = Shader.PropertyToID("_PostFXSource");

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    enum Pass
    {
        Copy,
        BloomHorizontal,
        BloomVertical
    }

    ScriptableRenderContext context;

    Camera camera;

    PostFXSettings settings;

    public bool IsActive => settings != null;

    // LDR Blooming
    const int maxBloomPyramidLevels = 16;
    int bloomPyramidId;

    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        for (int i = 0; i < maxBloomPyramidLevels; ++i)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    public void Setup(
        ScriptableRenderContext context, Camera camera, PostFXSettings settings
    )
    {
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
        DrawBloom(sourceId);
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

    void DrawBloom(int sourceId)
    {
        PostFXSettings.BloomSettings bloom = settings.Bloom;

        buffer.BeginSample("Bloom");
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        RenderTextureFormat format = RenderTextureFormat.Default;
        int fromId = sourceId, toId = bloomPyramidId;

        for (int i = 0; i < bloom.maxIterations; ++i)
        {
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
            {
                break;
            }
            buffer.GetTemporaryRT(
                toId, width, height, 0, FilterMode.Bilinear, format
            );
            if (i % 2 == 0)
            {
                Draw(fromId, toId, Pass.BloomHorizontal);
            }
            else
            {
                Draw(fromId, toId, Pass.BloomVertical);

            }
            fromId = toId;
            toId += 1;
            width /= 2;
            height /= 2;
        }
        
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

        for (int i = 0; i < bloom.maxIterations; ++i)
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId + i);
        }
        buffer.EndSample("Bloom");
    }
}