Shader "Custom/EnvironmentalVisualization"
{
    Properties
    {
        _VolumeTexture ("Volume Texture", 3D) = "white" {}
        _StepSize ("Ray Step Size", Range(0.01, 0.1)) = 0.05
        _Density ("Density", Range(0.1, 5.0)) = 1.0
        _AlphaThreshold ("Alpha Threshold", Range(0.0, 1.0)) = 0.02
        [KeywordEnum(Temperature, Moisture, WindSpeed, Biomes)] _VisType ("Visualization Type", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _VISTYPE_TEMPERATURE _VISTYPE_MOISTURE _VISTYPE_WINDSPEED _VISTYPE_BIOMES
            #include "UnityCG.cginc"

            #define MAX_STEPS 128
            #define STEP_SIZE 0.01

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            sampler3D _VolumeTexture;
            float _Density;
            float _AlphaThreshold;

            float4 TemperatureToColor(float temp)
            {
                // -20 to 40 range
                float t = (temp + 20.0) / 60.0;
                return float4(
                    saturate(t * 2.0),     // Red
                    saturate(1.0 - abs(t * 2.0 - 1.0)), // Green
                    saturate(2.0 - t * 2.0),// Blue
                    saturate(t * 0.8 + 0.2) // Alpha
                );
            }

            float4 MoistureToColor(float moisture)
            {
                return float4(
                    1.0 - moisture * 0.5,  // Red
                    1.0 - moisture * 0.5,  // Green
                    1.0,                   // Blue
                    saturate(moisture * 0.8 + 0.2) // Alpha
                );
            }

            float4 WindSpeedToColor(float speed)
            {
                float normalizedSpeed = speed / 20.0;
                return float4(
                    1.0 - normalizedSpeed * 0.7,  // Red
                    1.0,                          // Green
                    1.0 - normalizedSpeed * 0.7,  // Blue
                    saturate(normalizedSpeed * 0.8 + 0.2) // Alpha
                );
            }

            float4 BiomeToColor(float biomeIndex)
            {
                int index = (int)(biomeIndex * 8.0 + 0.5);
                switch(index)
                {
                    case 0: return float4(0.0, 0.2, 0.8, 0.8);    // Ocean
                    case 1: return float4(0.9, 0.9, 0.6, 0.8);    // Beach
                    case 2: return float4(0.5, 0.8, 0.3, 0.8);    // Plains
                    case 3: return float4(0.2, 0.6, 0.2, 0.8);    // Forest
                    case 4: return float4(0.0, 0.4, 0.0, 0.8);    // Jungle
                    case 5: return float4(0.9, 0.8, 0.2, 0.8);    // Desert
                    case 6: return float4(0.9, 0.9, 0.9, 0.8);    // Tundra
                    case 7: return float4(0.5, 0.5, 0.5, 0.8);    // Mountain
                    default: return float4(1.0, 1.0, 1.0, 0.8);   // Snow Peak
                }
            }

            float4 SampleVolume(float3 pos)
            {
                float4 sample = tex3D(_VolumeTexture, pos);
                
                #if defined(_VISTYPE_TEMPERATURE)
                    return TemperatureToColor(sample.r);
                #elif defined(_VISTYPE_MOISTURE)
                    return MoistureToColor(sample.g);
                #elif defined(_VISTYPE_WINDSPEED)
                    return WindSpeedToColor(sample.b);
                #else // _VISTYPE_BIOMES
                    return BiomeToColor(sample.a);
                #endif
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(worldPos - _WorldSpaceCameraPos);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 rayOrigin = i.localPos;
                float3 rayDir = normalize(mul((float3x3)unity_WorldToObject, i.viewDir));
                
                // Convert to texture space (0-1)
                rayOrigin = rayOrigin * 0.5 + 0.5;
                
                float4 color = float4(0, 0, 0, 0);
                float3 pos = rayOrigin;
                
                // Fixed iteration ray marching
                [unroll(MAX_STEPS)]
                for (int step = 0; step < MAX_STEPS; step++)
                {
                    // Check if we're outside the volume
                    if (any(pos < 0) || any(pos > 1) || color.a > 0.99)
                        break;
                        
                    float4 sample = SampleVolume(pos);
                    sample.a *= _Density * STEP_SIZE;
                    
                    // Skip low alpha samples
                    if (sample.a > _AlphaThreshold)
                    {
                        // Front-to-back compositing
                        float oneMinusAlpha = 1 - color.a;
                        color += sample * oneMinusAlpha;
                    }
                    
                    pos += rayDir * STEP_SIZE;
                }
                
                // Apply distance fade
                float viewDistance = length(i.localPos);
                color.a *= saturate(1 - viewDistance * 0.5);
                
                return color;
            }
            ENDCG
        }
    }
}
