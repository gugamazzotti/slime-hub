Shader "SlimeHub/SlimeJelly"
{
    Properties
    {
        [MainColor] _BaseColor ("Base", Color) = (0.42, 0.95, 0.62, 0.9)
        _RimColor ("Rim", Color) = (0.82, 1, 0.78, 1)
        _FresnelPower ("Fresnel", Range(0.5, 8)) = 2.4
        _WobbleAmp ("Wobble", Range(0, 0.08)) = 0.025
        _WobbleSpeed ("Wobble Speed", Float) = 1.6
        _Glitter ("Glitter", Range(0, 1)) = 0
        _Emission ("Emission", Range(0, 4)) = 0.15
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Pulse ("Pulse", Range(0, 1)) = 0
        _Radius ("Radius", Float) = 0.5
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
            Name "Slime"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _FresnelPower;
                float _WobbleAmp;
                float _WobbleSpeed;
                float _Glitter;
                float _Emission;
                float _Metallic;
                float _Pulse;
                float _Radius;
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
                float3 positionOS : TEXCOORD2;
            };

            float3 JellyWobble(float3 position, float3 normal)
            {
                float t = _Time.y;
                float breath = sin(t * 1.55) * _WobbleAmp * 0.55;
                float ripple = sin(dot(position, float3(2.1, 3.4, 1.6)) + t * _WobbleSpeed);
                ripple += sin(dot(position, float3(4.2, 1.4, 2.7)) - t * _WobbleSpeed * 0.67) * 0.55;
                ripple += sin(dot(position, float3(1.2, 2.2, 5.1)) + t * _WobbleSpeed * 1.37) * 0.28;
                return position * (1.0 + breath) + normal * (ripple * _WobbleAmp * 0.42);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 position = JellyWobble(input.positionOS.xyz, input.normalOS);
                output.positionOS = position;
                output.positionWS = TransformObjectToWorld(position);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float Bubble(float3 p, float3 center, float radius)
            {
                float d = length(p - center);
                return smoothstep(radius, radius * 0.45, d);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float radius = max(_Radius, 0.05);
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float ndv = saturate(dot(normal, viewDir));
                float fresnel = pow(saturate(1.0 - ndv), _FresnelPower);

                float3 lightDir = normalize(float3(0.2, 0.92, -0.32));
                float wrap = saturate(dot(normal, lightDir) * 0.65 + 0.28);
                float thick = pow(ndv, 0.55);
                float3 baseCol = _BaseColor.rgb;
                float3 deep = baseCol * float3(0.1, 0.2, 0.14);
                float3 body = lerp(deep, baseCol, lerp(0.35, 1.0, thick) * wrap);
                float3 rim = _RimColor.rgb;
                body = lerp(body, rim, fresnel * fresnel);

                float3 halfDir = normalize(lightDir + viewDir);
                float gloss = lerp(64.0, 140.0, _Metallic);
                float tight = pow(saturate(dot(normal, halfDir)), gloss);
                float broad = pow(saturate(dot(normal, halfDir)), 8.0);
                body += tight * lerp(1.05, 1.45, _Metallic);
                body += broad * rim * 0.16;

                float3 local = input.positionOS / radius;
                float n = sin(dot(local, float3(3.2, 2.4, 2.8)) + _Time.y * 0.18);
                body *= 1.0 + n * 0.03;

                float t = _Time.y;
                float3 b0 = float3(0.16, frac(0.2 + t * 0.025) * 1.1 - 0.55, 0.04);
                float3 b1 = float3(-0.2, frac(0.62 + t * 0.018) * 1.1 - 0.55, -0.08);
                float bubbles = Bubble(local, b0, 0.12) + Bubble(local, b1, 0.08);
                body = lerp(body, rim * 1.05, saturate(bubbles) * 0.28);

                float3 cell = floor(input.positionWS * lerp(8.0, 18.0, _Glitter) + t * 0.4);
                float hash = frac(sin(dot(cell, float3(127.1, 311.7, 74.7))) * 43758.5453);
                float spark = step(lerp(0.992, 0.72, _Glitter), hash) * _Glitter;
                body += spark * rim * 1.4;

                float pulse = 0.55 + 0.45 * sin(t * 3.2);
                body += _BaseColor.rgb * _Emission * lerp(0.85, pulse, saturate(_Pulse));
                body += rim * _Pulse * pulse * 0.22;

                float alpha = lerp(0.8, 0.93, fresnel) * saturate(_BaseColor.a + 0.06);
                alpha = lerp(alpha, max(alpha, 0.9), _Metallic);
                return half4(body, saturate(alpha));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
