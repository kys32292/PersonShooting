// 2026-11-23: 연기와 먼지의 부드러운 노이즈 윤곽을 외부 텍스처 없이 생성합니다.
Shader "PersonShooting/VFX/BlastSmoke"
{
    Properties
    {
        _NoiseScale ("Noise Scale", Float) = 5
        _Distortion ("Edge Distortion", Range(0, 1)) = 0.45
        _Age ("Animation Time", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _NoiseScale, _Distortion, _Age;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            // 2026-11-23: 두 주파수의 값 노이즈로 둥근 구름의 가장자리와 내부 농도를 다르게 표현합니다.
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 cell = floor(p), t = frac(p);
                t = t * t * (3.0 - 2.0 * t);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), t.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), t.x), t.y);
            }
            // 2026-11-23: 같은 셰이더를 쓰는 연기와 먼지의 위치/색/투명도는 각 파티클 정점에서 받습니다.
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            // 2026-11-23: 위로 흐르는 노이즈와 부드러운 원형 마스크를 곱해 사각 파티클의 테두리를 숨깁니다.
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * 2.0 - 1.0;
                float2 flow = input.uv * _NoiseScale + float2(_Age * 0.15, -_Age);
                float noise = Noise(flow) * 0.7 + Noise(flow * 2.1 - _Age * 0.2) * 0.3;
                float radial = length(uv);
                float body = 1.0 - radial + (noise - 0.5) * _Distortion;
                float opacity = smoothstep(0.0, 0.55, body) * (1.0 - smoothstep(0.8, 1.0, radial));
                // 2026-11-23: 발광을 넣지 않고 중심/가장자리 명암만 주어 검은 연기와 흙먼지로 보이게 합니다.
                half shade = lerp(0.55h, 1.15h, noise) * lerp(0.8h, 1.05h, saturate(input.uv.y));
                return half4(input.color.rgb * shade, input.color.a * opacity);
            }
            ENDHLSL
        }
    }
}
