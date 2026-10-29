Shader "SlimeHub/SlimeUnlit"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _GridColor ("Grid", Color) = (0.4, 0.9, 0.8, 1)
        _GridStrength ("Grid Strength", Range(0, 1)) = 0
        _Fresnel ("Fresnel", Range(0, 1)) = 0
        [Toggle] _ZWrite ("ZWrite", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridStrength;
                float _Fresnel;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float gridLine = step(0.94, frac(input.uv.x * 8.0)) + step(0.94, frac(input.uv.y * 8.0));
                float3 color = _BaseColor.rgb + _GridColor.rgb * gridLine * _GridStrength;
                float alpha = _BaseColor.a;
                if (_Fresnel > 0.01)
                {
                    float3 normal = normalize(input.normalWS);
                    float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                    float fresnel = pow(saturate(1.0 - dot(normal, viewDir)), 2.4);
                    color = lerp(color, _GridColor.rgb, fresnel * _Fresnel);
                    alpha = lerp(alpha, saturate(alpha + 0.55), fresnel);
                }
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
