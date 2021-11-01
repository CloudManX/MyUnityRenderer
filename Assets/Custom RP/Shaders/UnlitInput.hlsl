#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED

// Uniform
TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

// Per Instance Attributes
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST) // float2 scale and float2 translation
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
	UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Intensity)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct InputConfig
{
    Fragment fragment;
    float4 color;
    float2 baseUV;
    float3 flipbookUVB;
    bool flipbookBlending;
    bool nearFade;
};

float2 TransformBaseUV(float2 baseUV) 
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

InputConfig GetInputConfig (float4 positionSS, float2 baseUV) {
	InputConfig config;
    config.fragment = GetFragment(positionSS);
    config.color = 1.0;
	config.baseUV = baseUV;
    config.flipbookUVB = 0.0;
    // config.flipbookBlending = false;
    // config.nearFade = false;
	return config;
}

float4 GetBase(InputConfig c)
{
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);

    // if (c.flipbookBlending)
    // { 
    #if defined(_FLIPBOOK_BLENDING)
        baseMap = lerp(
            baseMap, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.flipbookUVB.xy),
            c.flipbookUVB.z
        ); 
    #endif
    // }

    // if (c.nearFade)
    // {
    #if defined(_NEAR_FADE)
        float nearAttenuation = (c.fragment.depth - INPUT_PROP(_NearFadeDistance)) /
            INPUT_PROP(_NearFadeRange);
        baseMap.a *= saturate(nearAttenuation);
    #endif
    // }

    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float intensity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Intensity); 
    return baseMap * baseColor * intensity * c.color;
}

float GetCutoff(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
}

float GetSmoothness(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
}

float GetFresnel(InputConfig c)
{
    return 0.0;
}

float3 GetEmission(InputConfig c)
{
    return GetBase(c).rgb;
}

float4 GetMask(InputConfig c)
{
    return 0.0;
}

#endif