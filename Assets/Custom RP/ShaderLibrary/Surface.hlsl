#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface
{
    float3 position; // microfacet pos
    float3 interpolatedNormal;
    float3 normal;
    float3 viewDir;
    float3 color; // albedo
    float depth;
    float alpha;
    float metallic;
    float occlusion;
    float smoothness;
    float fresnelStrength;
    float dither;
};

#endif