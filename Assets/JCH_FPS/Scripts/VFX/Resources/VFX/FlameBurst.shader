// 2026-11-23: 외부 이미지 없이 화염의 밝은 중심/불규칙한 가장자리/상승 무늬를 생성합니다.
Shader "PersonShooting/VFX/FlameBurst"
{
    Properties
    {
        [HDR] _HotColor ("Hot Core", Color) = (3, 2.5, 1.1, 1)
        [HDR] _FlameColor ("Orange Flame", Color) = (1.8, 0.45, 0.025, 1)
        [HDR] _EdgeColor ("Red Edge", Color) = (0.65, 0.035, 0.005, 0.85)
        _NoiseScale ("Noise Scale", Float) = 5
        _Distortion ("Edge Distortion", Float) = 0.45
        _Age ("Animation Time", Float) = 0
        _Cooling ("Cooling Progress", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
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
                half4 _HotColor, _FlameColor, _EdgeColor;
                float _NoiseScale, _Distortion, _Age, _Cooling;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            // 2026-11-23: 작은 값 노이즈를 두 겹 합성해 텍스처 의존성을 없앱니다.
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 cell = floor(p), t = frac(p);
                t = t * t * (3.0 - 2.0 * t);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), t.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), t.x), t.y);
            }
            // 2026-11-23: 파티클이 정한 크기/회전과 정점 색을 그대로 화면 좌표와 화염 투명도로 전달합니다.
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            // 2026-11-23: 노이즈로 원형 가장자리를 흐트러뜨리고 냉각 진행도에 따라 밝은 중심을 붉게 바꿉니다.
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv * 2.0 - 1.0;
                float2 flow = input.uv * _NoiseScale - float2(0, _Age);
                float noise = Noise(flow) * 0.65 + Noise(flow * 2.3 + _Age * 0.2) * 0.35;
                float radial = length(uv);
                float body = 1.0 - radial + (noise - 0.5) * _Distortion;
                float alpha = smoothstep(0.02, 0.3, body) * (1.0 - smoothstep(0.85, 1.0, radial));
                float heat = saturate(body * 1.6 + noise * 0.35 - _Cooling * 0.65);
                half4 color = lerp(_EdgeColor, _FlameColor, smoothstep(0.1, 0.6, heat));
                color = lerp(color, _HotColor, smoothstep(0.6, 1.0, heat));
                color.rgb *= input.color.rgb;
                color.a *= alpha * input.color.a;
                return color;
            }
            ENDHLSL
        }
    }
}
