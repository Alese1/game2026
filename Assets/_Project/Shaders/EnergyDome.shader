// Translucent energy barrier for the GGO safe zone: fresnel rim, scrolling hex-like grid
// lines and a soft pulse. Rendered double-sided so it is visible from inside and outside.
Shader "SwordArt/EnergyDome"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.3, 0.7, 1.6, 1)
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.06
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _GridScale ("Grid Scale", Float) = 0.35
        _GridWidth ("Grid Line Width", Range(0.001, 0.2)) = 0.04
        _ScrollSpeed ("Scroll Speed", Float) = 0.25
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "EnergyDome"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _BaseAlpha;
                half _RimPower;
                float _GridScale;
                half _GridWidth;
                half _ScrollSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(i.normalWS) * (frontFace ? 1.0 : -1.0);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1.0 - saturate(abs(dot(n, viewDir))), _RimPower);

                float t = _Time.y * _ScrollSpeed;
                float2 g = float2(i.positionWS.x + i.positionWS.z * 0.5, i.positionWS.y + t * 4.0) * _GridScale;
                float2 f = abs(frac(g) - 0.5);
                half line_ = saturate(1.0 - min(f.x, f.y) / _GridWidth);
                half band = 0.5 + 0.5 * sin(i.positionWS.y * 0.6 - t * 6.0);
                half pulse = 0.85 + 0.15 * sin(_Time.y * 2.0);

                half alpha = (_BaseAlpha + rim * 0.55 + line_ * 0.22 * band) * pulse;
                return half4(_Color.rgb, saturate(alpha) * _Color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
