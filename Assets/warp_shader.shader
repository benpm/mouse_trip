Shader "Unlit/warp_shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MajorRadius ("Major Radius", Range(0.1, 5)) = 1.0
        _MinorRadius ("Minor Radius", Range(0.05, 2)) = 0.3
        _TorusScale ("Torus Scale", Range(0.1, 5)) = 1.0
        _Interpolate ("Interpolate", Range(0, 1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _MajorRadius;
            float _MinorRadius;
            float _TorusScale;
            float _Interpolate;
            
            v2f vert (appdata v)
            {
                v2f o;
                float2 uv = v.uv;
                float theta = uv.x * UNITY_TWO_PI;
                float phi = uv.y * UNITY_TWO_PI;
                float cosPhi = cos(phi);
                float sinPhi = sin(phi);
                float cosTheta = cos(theta);
                float sinTheta = sin(theta);
                float radial = _MajorRadius + _MinorRadius * cosPhi;
                float3 torusPos = float3(radial * cosTheta, radial * sinTheta, _MinorRadius * sinPhi) * _TorusScale;
                float4 objectPos = float4(torusPos, 1.0);
                float4 blendedPos = lerp(v.vertex, objectPos, _Interpolate);

                o.vertex = UnityObjectToClipPos(blendedPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
