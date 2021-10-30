#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

struct Attributes
{
    float3 positionOS : POSITION;
    float4 color : COLOR;
    #if defined(_FLIPBOOK_BLENDING)
        float4 baseUV : TEXCOORD0;
        float flipbookBlend : TEXCOORD1;
    #else
        float2 baseUV : TEXCOORD0;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_Position;
    #if defined(_VERTEX_COLORS)
        float4 color : VAR_COLOR;
    #endif
    float2 baseUV : VAR_BASE_UV;
    #if defined(_FLIPBOOK_BLENDING)
        float3 flipbookUVB : VAR_FLIPBOOK;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{   
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionCS = TransformObjectToHClip(input.positionOS);
    
    #if defined(_VERTEX_COLORS)
        output.color = input.color;
    #endif

    output.baseUV = TransformBaseUV(input.baseUV.xy);
    #if defined(_FLIPBOOK_BLENDING)
        output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
        output.flipbookUVB.z = input.flipbookBlend;
    #endif
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.baseUV);

    #if defined(_VERTEX_COLORS)
        config.color = input.color;
    #endif

    #if defined(_FLIPBOOK_BLENDING)
        config.flipbookUVB = input.flipbookUVB;
        config.flipbookBlending = true;
    #endif

    float4 base = GetBase(config);

    #if defined(_CLIPPING)
        float cutoff = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
        clip(base.a - cutoff);
    #endif

    return base;
}

#endif