// 11/19 - 보스 장판 가시 외곽선 (BossSpikes.cs가 Resources.Load로 사용)
// 가시 몸체는 면이 각진(노멀이 갈라진) 메시라 셀 셰이더 외곽선을 쓰면 모서리가 끊어짐
// -> 같은 모양의 부드러운 노멀 메시를 앞면 컬링으로 월드 기준 일정 두께만큼 부풀려 검은 외곽선으로 그림
Shader "Boss/SpikeOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width (world)", Float) = 0.025
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "OUTLINE"
            Cull Front
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
            };

            float _OutlineWidth;
            fixed4 _OutlineColor;

            v2f vert (appdata v)
            {
                v2f o;
                // 가시마다 크기(비균등 스케일)가 달라도 두께가 같도록 월드 공간에서 부풀림
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                worldPos += worldNormal * _OutlineWidth;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
