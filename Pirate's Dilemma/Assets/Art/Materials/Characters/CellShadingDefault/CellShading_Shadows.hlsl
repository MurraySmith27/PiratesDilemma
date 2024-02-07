void CellShadingShadows_float(in float3 Normal, in float ToonRampSmoothness, in float3 ClipSpacePos, in float3 WorldPos, in float4 ToonRampTinting,
in float ToonRampOffset, out float3 ToonRampOutput, out float3 Direction, out float ToonRamp, out float ShadowAttenuation)
{
    #ifdef SHADERGRAPH_PREVIEW
        ToonRampOutput = float3(0.5, 0.5, 0);
        Direction = float3(0.5, 0.5, 0);
    
        float d = dot(Normal, Direction) * 0.5 + 0.5;
        ToonRamp = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, d);

        ShadowAttenuation = 0.5;
    #else
        #if SHADOWS_SCREEN
            half4 shadowCoord = ComputeScreenPos(ClipSpacePos);
        #else
            float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
        #endif

        #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
            Light light = GetMainLight(shadowCoord);
        #else
            Light light = GetMainLight();
        #endif


        float d = dot(Normal, light.direction) * 0.5 + 0.5;

        float toonRamp = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, d);

        toonRamp *= light.shadowAttenuation;

        ToonRampOutput = light.color * (toonRamp + ToonRampTinting);

        Direction = light.direction;

        ToonRamp = toonRamp;
        
        ShadowAttenuation = light.shadowAttenuation;
    #endif

}