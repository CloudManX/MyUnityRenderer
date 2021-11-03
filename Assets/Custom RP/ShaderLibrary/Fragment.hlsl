#ifndef FRAGMENT_INCLUDED
#define FRAGMENT_INCLUDED

TEXTURE2D(_CameraColorTexture);
SAMPLER(sampler_CameraColorTexture);

TEXTURE2D(_CameraDepthTexture);

struct Fragment
{
    float2 positionSS;
    float2 screenUV;
    float depth;
    float bufferDepth;
};

Fragment GetFragment(float4 positionSS)
{
    Fragment f;
    f.positionSS = positionSS.xy;
    f.screenUV = f.positionSS / _ScreenParams.xy;
    f.depth = IsOrthographicCamera() ?
        OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
    // f.bufferDepth =
    //     SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, f.screenUV, 0);
    f.bufferDepth = LOAD_TEXTURE2D(_CameraDepthTexture, f.positionSS).r;
	f.bufferDepth = IsOrthographicCamera() ?
		OrthographicDepthBufferToLinear(f.bufferDepth) :
		LinearEyeDepth(f.bufferDepth, _ZBufferParams);
    return f;
}

float4 GetBufferColor(Fragment fragment, float2 uvOffset = float2(0.0, 0.0))
{
    float2 uv = fragment.screenUV + uvOffset;
    return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_CameraColorTexture, uv, 0);
    // return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_CameraColorTexture, uv);
}

#endif