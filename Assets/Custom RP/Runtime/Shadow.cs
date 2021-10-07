using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class ShadowSettings
{
    [Min(0.001f)]
    public float maxDistance = 100f;

    [Range(0.001f, 1f)]
    public float distanceFade = 0.1f;

    public enum TextureSize
    {
        _256 = 256, _512 = 512, _1024 = 1024,
	    _2048 = 2048, _4096 = 4096, _8192 = 8192
    }

    public enum FilterMode
    {
        PCF2x2, PCF3x3, PCF5x5, PCF7x7
    }

    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;

        public FilterMode filter;

        [Range(1, 4)]
        public int cascadeCount;

        [Range(0f, 1f)]
        public float cascadeRatio1, cascadeRatio2, cascadeRatio3;

        [Range(0.001f, 1f)]
        public float cascadeFade;

        public Vector3 CascadeRatios =>
            new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

        public enum CascadeBlendMode
        {
            Hard, Soft, Dither
        }

        public CascadeBlendMode cascadeBlend;
    }

    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f,
        cascadeBlend = Directional.CascadeBlendMode.Hard
    }; 
}

public class Shadows
{
    const string bufferName = "Shadow";
    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;
    CullingResults cullingResults;
    ShadowSettings settings;

    // Shadow Lighting
    const int maxShadowedDirectionalLightCount = 4, maxCascades = 4;
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float nearPlaneOffset;
    }
    ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    int ShadowedDirectionalLightCount;

    static int
        dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        cascadeDataId = Shader.PropertyToID("_CascadeData"),
        shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
        cascadeCullingSphereId = Shader.PropertyToID("_CascadeCullingSpheres"),
        shadowDistanceId = Shader.PropertyToID("_ShadowDistanceFade");

    static Vector4[]
        cascadeCullingSpheres = new Vector4[maxCascades],
        cascadeData = new Vector4[maxCascades];

    static Matrix4x4[]
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    static string[] directionalFilterKeyWords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    static string[] cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER",
    };

    public void Setup(
        ScriptableRenderContext context, CullingResults cullingResults,
        ShadowSettings settings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
        ShadowedDirectionalLightCount = 0;
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None &&
            light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)
        )
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] =
                new ShadowedDirectionalLight
                {
                    visibleLightIndex = visibleLightIndex,
                    slopeScaleBias = light.shadowBias,
                    nearPlaneOffset = light.shadowNearPlane
                };
            return new Vector3(
                light.shadowStrength, 
                settings.directional.cascadeCount * ShadowedDirectionalLightCount++,
                light.shadowNormalBias
            );
        }
        return Vector3.zero;
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(
                dirShadowAtlasId, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
            );
        }
    }
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(
            dirShadowAtlasId, 
            atlasSize, 
            atlasSize,
            32,
            FilterMode.Bilinear,
            RenderTextureFormat.Shadowmap
        );
        buffer.SetRenderTarget(
            dirShadowAtlasId,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store
        );
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        for (int i = 0; i < ShadowedDirectionalLightCount; ++i)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        // Set shadow related uniforms in Lit Pass
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(
            shadowDistanceId,
            new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade,
                1f / (1f - f * f) 
            )
        );
        buffer.SetGlobalVectorArray(
            cascadeCullingSphereId, cascadeCullingSpheres
        );
        buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
        buffer.SetGlobalVector(
            shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize)
        );
        SetKeyWords(
            directionalFilterKeyWords, (int)settings.directional.filter - 1
        ); // PCF
        SetKeyWords(
            cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1
        ); // Cascaded Blend Mode: Hard, Soft, Dither 

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
     
    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings =
            new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        int casecdeCount = settings.directional.cascadeCount;
        int tileOffset = index * casecdeCount;
        Vector3 ratios = settings.directional.CascadeRatios;

        // Culling bias factor
        float cullingFactor =
            Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);

        for (int i = 0; i < casecdeCount; ++i)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, casecdeCount, ratios, tileSize, 
                light.nearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
            );
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix,
                SetTileViewport(tileIndex, split, tileSize),
                split
            );
            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            //SCVBB, pushing object away from light in shadow caster
            buffer.SetGlobalDepthBias(0, light.slopeScaleBias); 
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        // (1 + ceil(radius of filter) * texelSize)
        float filterSize = texelSize * ((float)settings.directional.filter + 1f);
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        cascadeData[index] = new Vector4(
            1f / cullingSphere.w,
            filterSize * 1.4142136f
        );
        cascadeCullingSpheres[index] = cullingSphere;
    }

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport( 
            new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize)    
        );
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
			m.m21 = -m.m21;
			m.m22 = -m.m22;
			m.m23 = -m.m23;
        }
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
		m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
		m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
		m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
		m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
		m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
		m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
		m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);
        // m *= Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 0.5f));
        // m *= Matrix4x4.Translate(new Vector3(offset.x, offset.y));
        // m *= Matrix4x4.Scale(new Vector3(scale, scale, scale));

        return m;
    }

    void SetKeyWords(string[] keywords, int enabledIndex) 
    {
        for (int i = 0; i < keywords.Length; ++i)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
}
