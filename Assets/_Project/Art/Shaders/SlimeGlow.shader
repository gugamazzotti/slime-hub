Shader "SlimeHub/SlimeGlow"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (0.7, 1, 0.8, 0.22)
        _WobbleAmp ("Wobble", Range(0, 0.08)) = 0.02
        _WobbleSpeed ("Wobble Speed", Float) = 1.6
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
            Name "Glow"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _WobbleAmp;
                float _WobbleSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float t = _Time.y;
                float breath = 1.0 + sin(t * 1.55) * _WobbleAmp * 0.45;
                float3 position = input.positionOS.xyz * breath + input.normalOS * (sin(t * _WobbleSpeed) * _WobbleAmp * 0.15);
                output.positionWS = TransformObjectToWorld(position);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(saturate(1.0 - dot(normal, viewDir)), 2.8);
                float alpha = _BaseColor.a * fresnel * fresnel;
                return half4(_BaseColor.rgb * fresnel, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
