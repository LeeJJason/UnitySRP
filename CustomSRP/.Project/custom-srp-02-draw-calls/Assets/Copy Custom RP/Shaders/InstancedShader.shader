          Shader "Instanced/InstancedShader" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader {

        Pass {

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityShaderVariables.cginc"
            #include "UnityCG.cginc"

            sampler2D _MainTex;

        #if SHADER_TARGET >= 45
            StructuredBuffer<float4> positionBuffer;
        #endif

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float3 color : TEXCOORD1;
            };

            void rotate2D(inout float2 v, float r)
            {
                float s, c;
                sincos(r, s, c);
                v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                float4 data = positionBuffer[instanceID];
            #else
                float4 data = 0;
            #endif

                float rotation = data.w * data.w * _Time.x * 0.5f;
                rotate2D(data.xz, rotation);

                float3 localPosition = v.vertex.xyz * data.w;
                float3 worldPosition = data.xyz + localPosition;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                o.uv_MainTex = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
            #if SHADER_TARGET >= 45
                fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                fixed4 output = fixed4(albedo.rgb * i.color, albedo.w);
                return output;
            #else
                return fixed4(1, 0, 0, 1);
            #endif
            }

            ENDHLSL
        }
    }
}