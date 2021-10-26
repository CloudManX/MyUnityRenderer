#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED

// Uniform
TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

// Per Instance Attributes
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST) // float2 scale and float2 translation
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Intensity)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float2 TransformBaseUV(float2 baseUV) 
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase(float2 baseUV)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float intensity = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Intensity); 
    return map * color * intensity;
}

float GetCutoff(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
}

float GetSmoothness(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
}

float GetFresnel(float2 baseUV)
{
    return 0.0;
}

float3 GetEmission(float2 baseUV)
{
    return GetBase(baseUV).rgb;
}

float4 GetMask(float2 baseUV)
{
    return 0.0;
}

#endif