#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : META_BASE_UV;
}

Varyings MetaPassVertex(Attributes input)
{
    Varyings output;
    input.positionOS.xy = 
        input.lightMapUV * unity_LightMapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS = TransformWorldToHCliip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 MetaPassFragment(Varyings input) : SV_TARGET
{
    float base = GetBase(input.baseUV);
    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.color = base.rgb;
    surface.metallic = GetMetallic(input.baseUV);
    surface.smoothness = GetSmoothness(input.baseUV);
    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    return meta;
}

#endif