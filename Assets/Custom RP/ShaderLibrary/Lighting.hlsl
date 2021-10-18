#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

// NDF
float SpecularStrength (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDir);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 IncomingLight(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float3 InDirectBRDF(
    Surface surface, BRDF brdf, float3 diffuse, float3 specular
) 
{
    float fresnelStrength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDir)));
    // float3 reflection = specular * brdf.specular;
    float3 reflection = specular * lerp(brdf.specular, 1.0 - brdf.specular, fresnelStrength);
    reflection /= brdf.roughness * brdf.roughness + 1.0;
    return (diffuse * brdf.diffuse + reflection) * surface.occlusion;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting (Surface surfaceWS, BRDF brdf)
{
    ShadowData shadowData = GetShadowData(surfaceWS);
    float3 color = 0.0f;
    for (int i = 0; i < GetDirectionalLightCount(); ++i)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }
    return color;
}

// With GI
float3 GetLighting (Surface surfaceWS, BRDF brdf, GI gi)
{
    ShadowData shadowData = GetShadowData(surfaceWS);
    shadowData.shadowMask = gi.shadowMask;
    // return gi.shadowMask.shadows.rgb;

    float3 color = InDirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);

    for (int i = 0; i < GetDirectionalLightCount(); ++i)
    {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        color += GetLighting(surfaceWS, brdf, light);
    }

    #if defined(_LIGHTS_PER_OBJECT)
        for (int j = 0; j < min(unity_LightData.y, 8); ++j)    
        {
            int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4]; 
            Light light = GetOtherLight(lightIndex, surfaceWS, shadowData);
            color += GetLighting(surfaceWS, brdf, light);
        }
    
    #else
        for (int j = 0; j < GetOtherLightCount(); ++j)
        {
            Light light = GetOtherLight(j, surfaceWS, shadowData);
            color += GetLighting(surfaceWS, brdf, light);
        }
    #endif

    return color;
}

#endif