Shader "Custom/EnvironmentalVisualization"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ArrowTex ("Arrow Texture", 2D) = "white" {}
        _ArrowScale ("Arrow Scale", Range(0.1, 1.0)) = 0.2
        _ArrowSpacing ("Arrow Spacing", Range(0.1, 1.0)) = 0.2
        _ArrowSpeed ("Arrow Animation Speed", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        // Temperature variant
        Pass
        {
            Name "Temperature"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        // Moisture variant
        Pass
        {
            Name "Moisture"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        // Wind variant
        Pass
        {
            Name "Wind"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _ArrowTex;
            float4 _MainTex_ST;
            float _ArrowScale;
            float _ArrowSpacing;
            float _ArrowSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 DrawArrow(float2 uv, float2 direction, float speed)
            {
                float2 gridPos = floor(uv / _ArrowSpacing);
                float2 gridUV = frac(uv / _ArrowSpacing);
                
                float2 offset = direction * _Time.y * _ArrowSpeed * speed;
                gridUV = frac(gridUV - offset);
                
                float2 arrowUV = (gridUV - 0.5) / _ArrowScale + 0.5;
                
                if (all(arrowUV >= 0 && arrowUV <= 1))
                {
                    float angle = atan2(direction.y, direction.x);
                    float2 rotatedUV;
                    sincos(angle, rotatedUV.y, rotatedUV.x);
                    float2x2 rotationMatrix = float2x2(rotatedUV.x, -rotatedUV.y, rotatedUV.y, rotatedUV.x);
                    arrowUV = mul(rotationMatrix, arrowUV - 0.5) + 0.5;
                    
                    return tex2D(_ArrowTex, arrowUV);
                }
                
                return float4(0, 0, 0, 0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float2 windDir = normalize(float2(1, 1));
                float windSpeed = col.r;
                float4 arrow = DrawArrow(i.uv, windDir, windSpeed);
                return lerp(col, arrow, arrow.a);
            }
            ENDCG
        }

        // Biome variant
        Pass
        {
            Name "Biome"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
