Shader "Custom/VRBubbleURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.8, 0.9, 1, 0.35)
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)

        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 4
        _FresnelIntensity ("Fresnel Intensity", Range(0, 3)) = 1.2

        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.8

        _ReflectionCubemap ("Reflection Cubemap", Cube) = "" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 2)) = 0.8

        _Opacity ("Opacity", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                        struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _FresnelPower;
                float _FresnelIntensity;
                float _NormalStrength;
                float _ReflectionStrength;
                float _Opacity;
            CBUFFER_END

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            TEXTURECUBE(_ReflectionCubemap);
            SAMPLER(sampler_ReflectionCubemap);
                        Varyings vert (Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS   = normalWS;
                OUT.viewDirWS  = normalize(_WorldSpaceCameraPos - positionWS);
                OUT.uv         = IN.uv;

                return OUT;
            }
                        half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // === Normal Map ===
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv));
                normalTS.xy *= _NormalStrength;
                normalTS = normalize(normalTS);

                float3 normalWS = normalize(IN.normalWS + normalTS);

                // === Fresnel ===
                float fresnel =
                    pow(
                        1.0 - saturate(dot(normalWS, IN.viewDirWS)),
                        _FresnelPower
                    ) * _FresnelIntensity;

                // === Reflection ===
                float3 reflectDir = reflect(-IN.viewDirWS, normalWS);
                float3 reflection =
                    SAMPLE_TEXTURECUBE(
                        _ReflectionCubemap,
                        sampler_ReflectionCubemap,
                        reflectDir
                    ).rgb;

                // === Final Color ===
                // === Fresnel Mask ===
                float fresnelMask = saturate(fresnel);

                // === 彩虹 Fresnel（薄膜干涉假象）===
                float3 rainbow =
                    float3(
                        0.5 + 0.5 * sin(fresnel * 20.28),
                        0.5 + 0.5 * sin(fresnel * 20.28 + 2.0),
                        0.5 + 0.5 * sin(fresnel * 20.28 + 4.0)
                    );

                // === 用彩虹當邊緣顏色 ===
                float3 color =
                    lerp(
                        float3(0,0,0),                      // 中央幾乎透明
                        rainbow + reflection * _ReflectionStrength,
                        fresnelMask
                    );

                // === Alpha：中央透明、邊緣不透明 ===
                float alpha =
                    lerp(
                        _Opacity * 0.15,                   // 中央非常透明
                        _Opacity,                          // 邊緣比較實
                        fresnelMask
                    );

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
