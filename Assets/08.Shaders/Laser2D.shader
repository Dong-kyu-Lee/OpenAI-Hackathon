Shader "Game/Laser2D"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [HDR][MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _ScrollSpeed("Base Scroll Speed (XY)", Vector) = (-0.6, 0, 0, 0)

        [Space]
        _NoiseMap("Distortion Noise", 2D) = "gray" {}
        _NoiseTiling("Noise Tiling (XY)", Vector) = (1, 1, 0, 0)
        _NoiseScrollSpeed("Noise Scroll Speed (XY)", Vector) = (0.9, 0.35, 0, 0)
        _DistortStrength("Distort Strength", Range(0, 0.5)) = 0.08

        [Space]
        _EdgeSoftness("Edge Softness", Range(0.001, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            // LightMode 태그를 붙이지 않는다. Renderer2D는 SRPDefaultUnlit과 Universal2D 패스만
            // 그리므로(Render2DLightingPass.cs), UniversalForward로 태그하면 화면에 나오지 않는다.
            Name "Laser2DUnlit"

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NoiseMap_ST;
                half4 _BaseColor;
                half4 _EmissionColor;
                float4 _ScrollSpeed;
                float4 _NoiseTiling;
                float4 _NoiseScrollSpeed;
                half _DistortStrength;
                half _EdgeSoftness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 noiseUv = input.uv * _NoiseTiling.xy + _NoiseScrollSpeed.xy * _Time.y;
                half noise = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, noiseUv).r;

                // 노이즈를 -1~1로 펴서 빔 두께 방향으로 UV를 민다. 스크롤이 무늬를 미끄러뜨린다면
                // 이쪽은 무늬 자체를 일렁이게 만든다.
                float2 baseUv = TRANSFORM_TEX(input.uv, _BaseMap) + _ScrollSpeed.xy * _Time.y;
                baseUv.y += (noise * 2.0h - 1.0h) * _DistortStrength;

                half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUv);
                half4 color = texel * _BaseColor * input.color;

                // 텍스처가 두꺼운 곳일수록 코어가 뜨겁게 보이도록 이미션을 얹는다.
                color.rgb += _EmissionColor.rgb * texel.a;

                // 가장자리 페이드는 두께 방향(v)에만 건다. u는 Texture Mode가 Tile이면 0~1이 아니라
                // 길이에 비례해 반복되므로 머리/꼬리 판정에 쓸 수 없다.
                half acrossBeam = 1.0h - abs(input.uv.y * 2.0h - 1.0h);
                color.a *= smoothstep(0.0h, _EdgeSoftness, acrossBeam);

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
