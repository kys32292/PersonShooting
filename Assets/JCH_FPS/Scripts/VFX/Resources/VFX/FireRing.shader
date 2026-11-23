Shader "PersonShooting/VFX/FireRing"
{
    Properties
    {
        [HDR] _HotColor ("Yellow Flame Core", Color) = (1.5, 1.1, 0.12, 1)
        [HDR] _FlameColor ("Orange Flame", Color) = (1.25, 0.25, 0.015, 1)
        [HDR] _EdgeColor ("Red Flame Edge", Color) = (1, 0.025, 0.008, 1)
        _Age ("Animation Time", Float) = 0
        _Opacity ("Opacity", Range(0, 1)) = 1
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
                float _Age, _Opacity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float tail : TEXCOORD1; };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
            }

            // Circular sampling keeps the texture-free animation seamless at UV u=0/1.
            float RingNoise(float u, float v, float frequency, float drift)
            {
                float a = u * 6.2831853;
                float2 circle = float2(cos(a), sin(a)) * frequency;
                return Noise(circle + float2(v * 2.7, drift));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.tail = input.uv2.x;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float v = input.uv.y;
                // Bend the radial flame tongues in the direction of orbit.
                float u = input.uv.x - _Age * 0.12 + (v - 0.5) * 0.045;
                float broad = RingNoise(u, v, 8.0, _Age * 0.6);
                float fine = RingNoise(u + 0.13, v, 24.0, -_Age * 1.3);
                float tongues = broad * 0.7 + fine * 0.3;
                float lower = 0.04 + tongues * 0.26;
                float upper = 0.67 + tongues * 0.3;
                float edge = min(v - lower, upper - v);
                float alpha = smoothstep(0.0, 0.025, edge);
                alpha *= smoothstep(0.0, 0.08, input.tail) * (1.0 - smoothstep(0.98, 1.0, input.tail));

                // A red silhouette surrounds broken yellow streaks, like drawn fire.
                float center = 1.0 - abs(v - 0.48) * 2.6;
                float streak = RingNoise(u + v * 0.025, v * 2.0, 15.0, _Age * 0.9);
                float heat = center + (streak - 0.5) * 0.95;
                float orange = smoothstep(0.015, 0.1, edge);
                half4 color = lerp(_EdgeColor, _FlameColor, orange);
                float yellow = smoothstep(0.48, 0.61, heat) * smoothstep(0.025, 0.1, edge);
                color = lerp(color, _HotColor, yellow);
                color.a *= alpha * saturate(_Opacity);
                return color;
            }
            ENDHLSL
        }
    }
}
