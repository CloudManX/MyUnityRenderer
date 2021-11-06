#ifndef CUSTOM_FXAA_PASS_INCLUDED
#define CUSTOM_FXAA_PASS_INCLUDED

float GetLuma (float2 uv)
{
    #if defined(FXAA_ALPHA_CONTAINS_LUMA)
		return GetSource(uv).a;
	#else
		return GetSource(uv).g;
	#endif
}

float4 FXAAPassFragment (Varyings input) : SV_TARGET {
	return GetLuma(input.screenUV);
}

#endif