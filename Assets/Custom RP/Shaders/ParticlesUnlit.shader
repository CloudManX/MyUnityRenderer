Shader "CustomRP/Particles/Unlit" 
{

	HLSLINCLUDE
	#include "../ShaderLibrary/Common.hlsl"
	#include "UnlitInput.hlsl"
	ENDHLSL

	Properties 
	{
		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
		_Intensity("Unlit Intensity", Range(0.0, 60.0)) = 1.0
	}
	SubShader
	{
		Pass 
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			HLSLPROGRAM
			#include "UnlitPass.hlsl"
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			ENDHLSL
		}
		Pass 
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}
			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_instancing	
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}
	CustomEditor "CustomShaderGUI"
}
