Shader "DrillCorp/BuffOutline"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.2, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _BlinkSpeed ("Blink Speed", Range(0, 10)) = 3
        _MinAlpha ("Min Alpha", Range(0, 1)) = 0.3

        [Toggle] _OutlineEnabled ("Outline Enabled", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // Pass 1: Outline (뒤에 그림)
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _BlinkSpeed;
                float _MinAlpha;
                float _OutlineEnabled;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 아웃라인 비활성화 시 버텍스를 0으로 (안 보이게)
                if (_OutlineEnabled < 0.5)
                {
                    output.positionCS = float4(0, 0, 0, 0);
                    return output;
                }

                // 노멀 방향으로 버텍스 확장
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += normalWS * _OutlineWidth;

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 깜빡임 효과 (sin 파형)
                float blink = sin(_Time.y * _BlinkSpeed) * 0.5 + 0.5; // 0~1
                float alpha = lerp(_MinAlpha, 1.0, blink);

                return half4(_OutlineColor.rgb, alpha);
            }
            ENDHLSL
        }

        // Pass 2: Base (기본 렌더링)
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _BlinkSpeed;
                float _MinAlpha;
                float _OutlineEnabled;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 color = baseMap * _BaseColor;

                // 간단한 디퓨즈 라이팅
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 diffuse = mainLight.color * NdotL;

                // 앰비언트
                float3 ambient = SampleSH(normalWS);

                color.rgb *= (diffuse + ambient);

                return color;
            }
            ENDHLSL
        }

        // Pass 3: ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
