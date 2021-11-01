#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    #if defined(_NORMAL_MAP)
        float4 tangentOS : TANGENT;
    #endif
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS_SS : SV_Position;
    float3 positionWS : F_Position;
    float3 normalWS : F_NORMAL;
    #if defined(_NORMAL_MAP)
        float4 tangentWS : F_TANGENT;
    #endif
    float2 baseUV : F_BASE_UV;
    #if defined(_DETAIL_MAP)
        float2 detailUV : F_DETAIL_UV;
    #endif
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{   
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    #if defined(_NORMAL_MAP)
        output.tangentWS = float4(
            TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w
        );
    #endif
        output.baseUV = TransformBaseUV(input.baseUV);
    #if defined(_DETAIL_MAP)
        output.detailUV = TransformDetailUV(input.baseUV);
    #endif
    return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);

    #if defined(LOD_FADE_CROSSFADE)
        ClipLOD(config.fragment, unity_LODFade.x);
    #endif

    #if defined(_MASK_MAP)
		config.useMask = true;
	#endif

    #if defined(_DETAIL_MAP)
        config.detailUV = input.detailUV;
        config.useDetail = true;
    #endif

    float4 base = GetBase(config);

    #if defined(_CLIPPING)
        clip(base.a - GetCutoff(config));
    #endif

    Surface surface;
    surface.position = input.positionWS;
    #if defined(_NORMAL_MAP)
        surface.interpolatedNormal = normalize(input.normalWS);
        surface.normal = NormalTangentToWorld(
            GetNormalTS(config), input.normalWS, input.tangentWS
        );
    #else
        surface.interpolatedNormal = normalize(input.normalWS);
        surface.normal = surface.interpolatedNormal;
	#endif
    surface.viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.color = base.rgb;
    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.alpha = base.a;
    surface.metallic = GetMetallic(config);
    surface.occlusion = GetOcclusion(config);
    surface.smoothness = GetSmoothness(config);
    surface.fresnelStrength = GetFresnel(config);
    surface.dither = InterleavedGradientNoise(config.fragment.positionSS, 0);

    #if defined(_PREMULTIPLY_ALPHA)
        BRDF brdf = GetBRDF(surface, true);
    #else
        BRDF brdf = GetBRDF(surface);
    #endif

    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
        
    float3 color = GetLighting(surface, brdf, gi);
    color += GetEmission(config);

    return float4(color, surface.alpha);
}

#endif