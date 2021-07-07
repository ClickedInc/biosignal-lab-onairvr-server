Shader "onAirXR/Foveation Visualization"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Opacity("Opacity", Float) = 1
        _MidColor("Mid Color", Color) = (1, 1, 0, 0.3)
        _OuterColor("Outer Color", Color) = (1, 0, 1, 0.3)
        _InnerRadii("Inner Radii", Float) = 0.2
        _MidRadii("Mid Radii", Float) = 0.4
        _GazeX("Gaze X", Float) = 0
        _GazeY("Gaze Y", Float) = 0
        _Bound("Bound", Vector) = (-2, 2, 2, -2)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

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

            uniform float _Opacity;
            uniform float4 _MidColor;
            uniform float4 _OuterColor;
            uniform float _InnerRadii;
            uniform float _MidRadii;
            uniform float _GazeX;
            uniform float _GazeY;
            uniform float4 _Bound;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float inrange(float min, float max, float value)
            {
                return step(min, value) - step(max, value);
            }

            fixed4 tintColor(v2f i)
            {
                float2 center = float2(0.5 + _GazeX / 4, 0.5 + _GazeY / 4);
                float distance = length(center - i.uv);

                return inrange(0, _InnerRadii / 4, distance) * fixed4(0, 0, 0, 0) + inrange(_InnerRadii / 4, _MidRadii / 4, distance) * _MidColor +
                        step(_MidRadii / 4, distance) * _OuterColor;
            }

            float tintOpacity(v2f i)
            {
                float4 bound = 0.5 + _Bound / 4;

                return inrange(bound.x, bound.z, i.uv.x) * inrange(bound.w, bound.y, i.uv.y) * _Opacity;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 tint = tintColor(i);
                fixed opacity = tintOpacity(i);

                col = col * (1 - tint.a * opacity) + tint * tint.a * opacity;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
