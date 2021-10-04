#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04 // Relectivity of dieletric surfaces, aka F0

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range * (1.0 - metallic);
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse == true)
    {
        brdf.diffuse *= surface.alpha;
    }
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    float perceptualRoughness = 
        PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

#endif