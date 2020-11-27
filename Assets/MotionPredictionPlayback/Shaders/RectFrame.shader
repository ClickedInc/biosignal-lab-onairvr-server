Shader "Unlit/RectFrame"
{
    Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _Projection("Projection", Vector) = (-1, 1, 1, -1)
        _Width("Width", Float) = 0.002
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
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

            uniform float4 _Color;
            uniform float4 _Projection;
            uniform float _Width;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float calcAlpha(v2f i) : float 
            {
                return step(0, _Width - i.uv.x) + step(0, _Width - i.uv.y) + step(1, i.uv.x + _Width) + step(1, i.uv.y + _Width);
            }

            float isin(float center, float width, float value) : float 
            {
                return step(center - width / 2, value) - step(center + width / 2, value);
            }

            float inrange(float min, float max, float width, float value) : float 
            {
                return step(min - width / 2, value) - step(max + width / 2, value);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 projection = 0.5 + _Projection / 4;
                float alpha = clamp(
                    isin(projection.x, _Width, i.uv.x) + isin(projection.y, _Width, i.uv.y) + isin(projection.z, _Width, i.uv.x) + isin(projection.w, _Width, i.uv.y)
                    + inrange(projection.x, projection.z, _Width, i.uv.x) + inrange(projection.w, projection.y, _Width, i.uv.y) - 2,
                    0,
                    1
                );

                fixed4 col = fixed4(_Color.r, _Color.g, _Color.b, alpha);
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
