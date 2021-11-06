Shader "Hidden/Custom RP/Post FX Stack" 
{
	SubShader
	{
		Cull Off		
		ZTest Always
		ZWrite Off

		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "PostFXStackPasses.hlsl"
		ENDHLSL

		Pass
		{
			Name "Copy"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Horizontal"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomHorizontalPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Vertical"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomVerticalPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Add"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomAddPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Scatter"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomScatterPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Final"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomFinalPassFragment
			ENDHLSL

		}

		Pass 
		{
			Name "Bloom Prefilter"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomPrefilterPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Bloom PrefilterFadeFireflies"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomPrefilterFadeFirefliesPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "ColorGrading None"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingNonePassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "ColorGrading ACES"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingACESPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "ColorGrading Neutral"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingNeutralPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "ColorGrading Reinhard"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingReinhardPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Apply Color Grading"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma shader_feature _LUT_BANDING
				#pragma vertex DefaultPassVertex
				#pragma fragment ApplyColorGradingPassFragment
			ENDHLSL
		}
				
		Pass {
			Name "Apply Color Grading With Luma"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ApplyColorGradingWithLumaPassFragment
			ENDHLSL
		}
	
		Pass 
		{
			Name "Final Rescale"

			Blend [_FinalSrcBlend][_FinalDstBlend]

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FinalPassFragmentRescale
			ENDHLSL
		}

		Pass {
			Name "FXAA"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FXAAPassFragment
				#include "FXAAPass.hlsl"
			ENDHLSL
		}

		Pass {
			Name "FXAA With Luma"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
				#define FXAA_ALPHA_CONTAINS_LUMA
				#pragma vertex DefaultPassVertex
				#pragma fragment FXAAPassFragment
				#include "FXAAPass.hlsl"
			ENDHLSL
		}
	}
}
