// 11/19 - 보스 장판 가시 몸체 (BossSpikes.cs가 Resources.Load로 사용)
// 셀 셰이더(CG 조명)로는 URP 씬에서 조명/안개 영향으로 탁하고 흐릿하게 보여서,
// 씬 조명과 무관하게 항상 선명하게 보이는 전용 셰이더로 그림
// - 색: 밑동(_BaseColor) -> 끝(_TipColor) 세로 그라데이션 (가시 메시는 높이 0~1이라 오브젝트 y를 그대로 사용)
// - 음영: 고정된 빛 방향으로 면마다 2단 음영 -> 각진 면이 또렷하게 구분됨
// - 림: 가장자리를 밝게 해 배경과 분리
Shader "Boss/SpikeBody"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.32, 0.02, 0.05, 1)
        _TipColor ("Tip Color", Color) = (1.0, 0.32, 0.22, 1)
        _ShadeColor ("Shade Multiplier", Color) = (0.55, 0.45, 0.5, 1)
        _LightDir ("Light Direction (world)", Vector) = (0.4, 0.8, 0.3, 0)
        _ShadeThreshold ("Shade Threshold", Range(-1, 1)) = 0.15
        _RimColor ("Rim Color", Color) = (1.0, 0.55, 0.45, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _Emission ("Emission", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "BODY"
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float height : TEXCOORD2;
            };

            fixed4 _BaseColor;
            fixed4 _TipColor;
            fixed4 _ShadeColor;
            float4 _LightDir;
            float _ShadeThreshold;
            fixed4 _RimColor;
            float _RimPower;
            float _Emission;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.height = saturate(v.vertex.y);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldNormal);
                float3 l = normalize(_LightDir.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                // 끝으로 갈수록 밝은 붉은색 (끝부분을 조금 더 빨리 밝게)
                fixed3 col = lerp(_BaseColor.rgb, _TipColor.rgb, pow(i.height, 0.7));

                // 2단 음영 (경계를 살짝만 부드럽게)
                float ndl = dot(n, l);
                float lit = smoothstep(_ShadeThreshold - 0.05, _ShadeThreshold + 0.05, ndl);
                col *= lerp(_ShadeColor.rgb, 1.0, lit);

                // 림 라이트
                float rim = pow(1.0 - saturate(dot(viewDir, n)), _RimPower);
                col += _RimColor.rgb * rim * 0.6;

                col += _TipColor.rgb * _Emission * i.height;
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
