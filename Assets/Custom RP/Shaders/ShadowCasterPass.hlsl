#ifndef CUSTOM_SHADOW_CASTER_PASS_INCLUDED
#define CUSTOM_SHADOW_CASTER_PASS_INCLUDED

bool _ShadowPancking;

struct Attributes
{
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS_SS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ShadowCasterPassVertex (Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(positionWS);

    if (_ShadowPancking)
    {
#if UNITY_REVERSED_Z
        output.positionCS_SS.z = 
            min(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
#else
        output.positionCS_SS.z =
                max(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
#endif
    }

    // float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    // output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 ShadowCasterPassFragment (Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);


    InputConfig config = GetInputConfig(input.positionCS_SS, input.baseUV);
    ClipLOD(config.fragment, unity_LODFade.x);
    float4 base = GetBase(config);
    #if defined(_SHADOWS_CLIP)
        clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #elif defined(_SHADOWS_DITHER)
        float dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
        clip(base.a - dither);
    #endif
    return base;
}

#endif
