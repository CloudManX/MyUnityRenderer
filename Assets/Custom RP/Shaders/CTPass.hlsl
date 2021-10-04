#ifndef CUSTOM_CT_PASS_INCLUDED
#define CUSTOM_CT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

// Uniform
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// Per Instance Attributes
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST) // float2 scale and float2 translation
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_Position;
    float3 positionWS : F_Position;
    float3 normalWS : F_NORMAL;
    float2 baseUV : F_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings CTPassVertex(Attributes input)
{   
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}


float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1 - F0) * pow(saturate(1.0 - cosTheta), 5.0);
}

float DistributionGGX(float3 N, float3 H, float roughness)
{
    float alpha = roughness * roughness;
    float alpha2 = Square(alpha);
    float NdotH2 = Square(saturate(dot(N, H)));

    float numerator = alpha2;
    float denominator = PI * Square(NdotH2 * (alpha2 - 1.0) + 1.0);
    
    return numerator / denominator;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float k = Square((roughness + 1.0)) / 8.0;

    float numerator = NdotV;
    float denominator = NdotV * (1.0 - k) + k;

    return numerator / denominator;
}


float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float ggx_shadow = GeometrySchlickGGX(saturate(dot(N, L)), roughness);
    float ggx_obstruction = GeometrySchlickGGX(saturate(dot(N, V)), roughness);
    return ggx_shadow * ggx_obstruction;
}

float4 CTPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseMap * baseColor;

    Surface surface;
    surface.normal = normalize(input.normalWS);
    surface.viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);

    float3 Lo = 0.0f;
    float3 F0 = 0.04f;
    F0 = lerp(F0, surface.color, surface.metallic);

    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    // float roughness = 1.0f - surface.smoothness;

    for (int i = 0; i < GetDirectionalLightCount(); ++i)
    {
        ShadowData data;
        Light light = GetDirectionalLight(i, surface, data);
        float3 radiance = light.color;

        float3 H = normalize(light.direction + surface.viewDir);
        float cosTheta = saturate(dot(H, surface.viewDir));


        float3 NDF = DistributionGGX(surface.normal, H, roughness);
        float3 G = GeometrySmith(surface.normal, surface.viewDir, light.direction, roughness);
        float3 Fresnel = FresnelSchlick(cosTheta, F0);

        float3 kS = Fresnel;
        float3 kD = (1.0f - kS) * (1.0 - surface.metallic);

        float3 numerator = NDF * G * Fresnel;
        float3 denominator = 4.0 *
            saturate(dot(surface.viewDir, surface.normal)) *
            saturate(dot(light.direction, surface.normal)) + 0.0001;

        float3 specular = numerator / denominator;
        float3 diffuse = kD * surface.color / PI;
        float3 NdotL = saturate(dot(surface.normal, light.direction));

        Lo += (NDF + diffuse) * radiance * NdotL;
    }

    return float4(Lo, 1.0);
}

#endif