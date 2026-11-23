// 2026-11-23: 가장자리의 불규칙한 피 얼룩만 옅게 표시하고 중앙 시야를 투명하게 유지합니다.
Shader "PersonShooting/VFX/PlayerBloodEdge"
{
    Properties
    {
        // 2026-11-23: RawImage가 조회하는 기본 텍스처 속성을 제공해 UI 머티리얼 오류를 방지합니다.
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _BloodColor ("Blood Color", Color) = (0.52, 0.015, 0.025, 1)
        _Opacity ("Opacity", Range(0,1)) = 0
        _EdgeWidth ("Edge Width", Range(0.02,0.25)) = 0.17
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _BloodColor;
            float _Opacity, _EdgeWidth;
            // 2026-11-23: 전체 화면 UI 메시를 투영하고 UV를 가장자리 거리 계산에 전달합니다.
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            // 2026-11-23: 고정 난수/보간 노이즈로 매 프레임 깜빡이지 않는 불규칙한 얼룩을 만듭니다.
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p); f = f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            // 2026-11-23: 네 가장자리 중 가까운 거리에만 얼룩과 작은 방울의 알파를 적용합니다.
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float edge = min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
                float irregular = noise(uv*float2(21,15))*0.6 + noise(uv*float2(61,43))*0.4;
                float smear = 1-smoothstep(0,_EdgeWidth*(0.4+irregular*0.6),edge);
                // 2026-11-23: 작은 얼룩도 테두리 안쪽으로만 제한하여 조준 중앙에 묻지 않게 합니다.
                float2 grid = uv*float2(40,25), cell = floor(grid);
                float seed = hash(cell);
                float2 center = float2(hash(cell+5.3),hash(cell+17.1))*0.5+0.25;
                float drops = (1-smoothstep(0.07,0.2,length(frac(grid)-center))) * step(0.65,seed);
                float mask = 1-smoothstep(_EdgeWidth*0.45,_EdgeWidth,edge);
                float alpha = saturate(smear*(0.35+irregular*0.65)+drops*mask*0.65)*_Opacity*_BloodColor.a;
                return fixed4(_BloodColor.rgb,alpha);
            }
            ENDCG
        }
    }
}
