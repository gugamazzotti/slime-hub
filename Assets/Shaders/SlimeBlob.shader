// Bolha unlit, sem keywords. Uma variante só: não puxa o Standard
// (centenas de milhares de variantes) e não fica magenta no WebGL.
Shader "SlimeHub/Blob"
{
    Properties
    {
        _Color ("Color", Color) = (0.5, 0.9, 0.4, 1)
        _Highlight ("Highlight", Color) = (1, 1, 1, 0.7)
        _Round ("Round", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _Highlight;
            float _Round;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float round = saturate(_Round);
                float2 p = i.uv * 2.0 - 1.0;
                float r2 = dot(p, p);
                float circle = saturate((1.0 - r2) * 7.0);
                float alpha = lerp(1.0, circle, round) * _Color.a;
                float2 h = p - float2(-0.32, 0.40);
                float spec = saturate(1.0 - dot(h, h) * 2.8);
                spec = spec * spec * round;
                float shade = lerp(0.86, 1.05, i.uv.y);
                float rim = saturate((r2 - 0.55) * 2.2) * round;
                float3 col = _Color.rgb * shade * (1.0 - 0.32 * rim);
                col = lerp(col, _Highlight.rgb, spec * _Highlight.a);
                clip(alpha - 0.02);
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
