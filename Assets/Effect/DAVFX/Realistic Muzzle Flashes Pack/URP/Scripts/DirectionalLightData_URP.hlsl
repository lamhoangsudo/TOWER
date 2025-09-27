#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

void DirectionalAndPointLightsColor_float(float3 PositionWS, out float3 Directional, out float3 Point)
{
#if SHADERGRAPH_PREVIEW
    Directional = float3(1, 1, 1);
    Point = float3(0, 0, 0);
#else
    float3 dirColor = float3(0, 0, 0);
    float3 pntColor = float3(0, 0, 0);

    Light mainLight = GetMainLight();
    dirColor += mainLight.color * mainLight.distanceAttenuation;

    int lightCount = GetAdditionalLightsCount();
    for (int i = 0; i < lightCount; i++)
    {
        Light additionalLight = GetAdditionalLight(i, PositionWS);
        pntColor += additionalLight.color * additionalLight.distanceAttenuation;
    }

    Directional = dirColor;
    Point = pntColor;
#endif
}

#endif
