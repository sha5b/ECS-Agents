Shader "Custom/EnvironmentalVisualization"
{
    Properties
    {
        _VolumeTexture ("Volume Texture", 3D) = "white" {}
        _StepSize ("Ray Step Size", Range(0.01, 0.1)) = 0.05
        _Density ("Density", Range(0.1, 5.0)) = 1.0
        _AlphaThreshold ("Alpha Threshold", Range(0.0, 1.0)) = 0.02
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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            sampler3D _VolumeTexture;
            float _StepSize;
            float _Density;
            float _AlphaThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldCameraPos = _WorldSpaceCameraPos;
                o.viewDir = normalize(o.worldPos - worldCameraPos);
                return o;
            }

            // Ray-Box Intersection
            bool IntersectBox(float3 origin, float3 direction, float3 boxMin, float3 boxMax, 
                            out float tMin, out float tMax)
            {
                float3 invDir = 1.0 / direction;
                float3 t0 = (boxMin - origin) * invDir;
                float3 t1 = (boxMax - origin) * invDir;
                float3 tSmaller = min(t0, t1);
                float3 tBigger = max(t0, t1);
                tMin = max(max(tSmaller.x, tSmaller.y), tSmaller.z);
                tMax = min(min(tBigger.x, tBigger.y), tBigger.z);
                return tMax > tMin && tMax > 0;
            }

            // Volume Ray Marching
            float4 RayMarch(float3 rayOrigin, float3 rayDirection, float tMin, float tMax)
            {
                float3 currentPos = rayOrigin + tMin * rayDirection;
                float stepSize = _StepSize;
                float t = tMin;
                float4 accumColor = float4(0, 0, 0, 0);

                while (t < tMax && accumColor.a < 0.99)
                {
                    // Convert world position to volume texture coordinates (0-1 range)
                    float3 texCoord = mul(unity_WorldToObject, float4(currentPos, 1)).xyz * 0.5 + 0.5;
                    
                    // Sample volume texture
                    float4 sample = tex3D(_VolumeTexture, texCoord);
                    
                    // Apply density factor
                    sample.a *= _Density * stepSize;
                    
                    // Skip samples with very low alpha
                    if (sample.a > _AlphaThreshold)
                    {
                        // Front-to-back compositing
                        float oneMinusAlpha = 1 - accumColor.a;
                        accumColor += sample * oneMinusAlpha;
                    }
                    
                    currentPos += rayDirection * stepSize;
                    t += stepSize;
                }

                return accumColor;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 rayOrigin = mul(unity_WorldToObject, float4(i.worldPos, 1)).xyz;
                float3 rayDirection = normalize(mul((float3x3)unity_WorldToObject, i.viewDir));

                float tMin, tMax;
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3(0.5, 0.5, 0.5);

                if (!IntersectBox(rayOrigin, rayDirection, boxMin, boxMax, tMin, tMax))
                    discard;

                float4 color = RayMarch(rayOrigin, rayDirection, tMin, tMax);
                
                // Apply distance-based fog effect
                float fogFactor = 1.0 - exp(-length(i.worldPos - _WorldSpaceCameraPos) * 0.1);
                color.rgb = lerp(color.rgb, unity_FogColor.rgb, fogFactor * color.a);
                
                return color;
            }
            ENDCG
        }
    }
}
