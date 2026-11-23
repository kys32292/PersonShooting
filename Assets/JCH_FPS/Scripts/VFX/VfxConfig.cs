using System;
using UnityEngine;

namespace PesonShooting.VFX
{
    // 2026-11-09: 하이어라키 대신 Project 창에 보관하는 상황별 연출 설정입니다.
    // 자동 로드 대상은 VFX/Resources/VFX/VfxConfig.asset 한 개로 유지합니다.
    [CreateAssetMenu(fileName = "VfxConfig", menuName = "VFX/Config")]
    public sealed class VfxConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            // 2026-11-23: 한국어 표시명/설명만 추가하고 저장 필드명과 기존 수치는 유지합니다.
            [InspectorName("연출 종류")]
            [Tooltip("같은 종류를 여러 개 등록하면 유효한 프리팹 중 하나를 무작위로 재생합니다.")]
            public VfxId id;
            [InspectorName("연출 프리팹")]
            [Tooltip("재생할 파티클 또는 전용 연출 프리팹입니다.")]
            public GameObject prefab;
            [InspectorName("전체 크기 배율")]
            [Tooltip("프리팹 원래 크기에 곱하는 값입니다. 2는 두 배입니다.")]
            [Min(0.01f)] public float scale = 1f;
            [InspectorName("제거 시간 (초)")]
            [Tooltip("생성 후 게임 시간으로 이 시간이 지나면 제거합니다.")]
            [Min(0.01f)] public float lifetime = 2f;
            [InspectorName("표면에서 띄우는 거리")]
            [Tooltip("피격 연출이 표면에 묻히지 않도록 바깥으로 이동하는 거리입니다. 월드 단위입니다.")]
            [Min(0f)] public float surfaceOffset = 0.03f;
            [InspectorName("방향 보정 (도)")]
            [Tooltip("프리팹 원래 방향을 보정하는 로컬 X/Y/Z 회전각입니다.")]
            public Vector3 rotationOffset;
        }

        [Serializable]
        public sealed class BulletTrailSettings
        {
            // 2026-11-23: 한국어 표시명/설명만 추가하고 저장 필드명과 기존 수치는 유지합니다.
            [InspectorName("총알 궤적 표시")]
            [Tooltip("일반 총알 뒤에 남는 선 궤적을 켜거나 끕니다.")]
            public bool enabled = true;
            [InspectorName("최대 궤적 길이")]
            [Tooltip("총알 뒤에 남는 궤적의 최대 길이입니다. 월드 단위입니다.")]
            [Min(0.01f)] public float length = 2f;
            [InspectorName("궤적 머리 굵기")]
            [Tooltip("총알에 가까운 쪽 선의 굵기입니다. 월드 단위입니다.")]
            [Min(0.001f)] public float width = 0.06f;
            [InspectorName("꼬리 굵기 비율")]
            [Tooltip("머리 굵기에 대한 꼬리 굵기의 비율입니다. 0은 뾰족하고 1은 같은 굵기입니다.")]
            [Range(0f, 1f)] public float tailWidthRatio = 0.15f;
            [InspectorName("궤적 머리 색")]
            [Tooltip("총알 쪽 색과 불투명도입니다. 알파 A가 0이면 투명합니다.")]
            [ColorUsage(true, true)] public Color headColor = new Color(0.3f, 0.9f, 1f, 1f);
            [InspectorName("궤적 꼬리 색")]
            [Tooltip("뒤쪽 끝의 색과 불투명도입니다. 알파 A가 0이면 끝이 투명해집니다.")]
            [ColorUsage(true, true)] public Color tailColor = new Color(0.1f, 0.5f, 1f, 0f);
            [InspectorName("소멸 후 사라지는 시간 (초)")]
            [Tooltip("총알이 사라진 뒤 궤적이 서서히 사라지는 시간입니다.")]
            [Min(0f)] public float fadeDuration = 0.12f;
            [InspectorName("궤적 점 사이 최소 거리")]
            [Tooltip("작을수록 곡선이 세밀하지만 궤적 점이 많아집니다. 월드 단위입니다.")]
            [Min(0.001f)] public float minVertexDistance = 0.05f;
            [InspectorName("궤적 머티리얼 (선택)")]
            [Tooltip("비우면 기본 레이저 머티리얼입니다. 지정 시 정점 색과 투명도를 지원해야 합니다.")]
            public Material material;
        }

        // 2026-11-09: 카드 흡수와 총 강화 발광의 수치를 한곳에서 조절합니다. 시간은 일시정지와 무관한 실제 초입니다.
        [Serializable]
        public sealed class PerkAbsorbSettings
        {
            // 2026-11-23: 한국어 표시명/설명만 추가하고 저장 필드명과 기존 수치는 유지합니다.
            [InspectorName("카드 흡수 연출 표시")]
            [Tooltip("카드가 총으로 흡수되는 연출을 켜거나 끕니다.")]
            public bool enabled = true;
            [InspectorName("카드 흡수 시간 (초)")]
            [Tooltip("카드가 총까지 이동하는 시간입니다. 게임 일시정지와 무관한 실제 시간입니다.")]
            [Min(0.01f)] public float absorbDuration = 0.6f;
            [InspectorName("흡수 직전 크기 비율")]
            [Tooltip("원래 카드 크기에 대한 최종 크기 비율입니다. 0.1은 원래 크기의 10%입니다.")]
            [Range(0f, 1f)] public float endScale = 0.015f;
            [InspectorName("이동 곡선 높이 비율")]
            [Tooltip("화면 높이 대비 위로 휘는 높이입니다. 0은 직선, 음수는 아래쪽 곡선입니다.")]
            [Range(-0.5f, 0.5f)] public float arcHeightRatio = 0.07f;
            [InspectorName("흡수 가속 지수")]
            [Tooltip("1은 일정한 진행이며 클수록 후반부에 빨라지면서 축소됩니다.")]
            [Min(0.1f)] public float absorbPower = 2f;
            [InspectorName("카드 페이드 시작 진행률")]
            [Tooltip("0~1 중 카드가 사라지기 시작하는 시점입니다. 0.8은 이동 80% 지점입니다.")]
            [Range(0f, 0.99f)] public float fadeStart = 0.8f;
            [InspectorName("총 발광 시간 (초)")]
            [Tooltip("흡수 후 총이 빛나는 실제 시간입니다. 0이면 발광을 생략합니다.")]
            [Min(0f)] public float flashDuration = 0.38f;
            [InspectorName("총 발광 색")]
            [Tooltip("발광 색과 불투명도입니다. HDR 강도도 조절할 수 있습니다.")]
            [ColorUsage(true, true)] public Color flashColor = new Color(0.3f, 1.5f, 2f, 1f);
            [InspectorName("총 발광 밝기 배율")]
            [Tooltip("발광 색상 밝기에 곱하는 값입니다.")]
            [Min(0f)] public float flashIntensity = 1f;
            [InspectorName("발광 반짝임 횟수")]
            [Tooltip("발광 시간 동안 반짝이는 횟수입니다. 0이면 반짝임 없이 사라집니다.")]
            [Min(0f)] public float flashPulseCount = 2f;
            [InspectorName("발광 반짝임 변화 폭")]
            [Tooltip("반짝일 때 밝기가 변하는 폭입니다. 0이면 밝기 변화가 없습니다.")]
            [Range(0f, 0.5f)] public float flashPulseDepth = 0.35f;
        }

        // 2026-11-23: 중력탄 시제품을 본 게임과 테스트 씬에서 공유하는 시각 효과 설정입니다.
        // 범위/유지 시간/피해/흡입 속도는 PerkData.special을 사용하며, 아래 값은 외형만 바꿉니다.
        [Serializable]
        public sealed class GravityWellSettings
        {
            [Header("전체 / 색상")]
            // 2026-11-23: 한국어 표시명/설명만 추가하고 저장 필드명과 기존 수치는 유지합니다.
            [InspectorName("착탄 중력장 표시")]
            [Tooltip("착탄 후 흡입 입자와 범위 표시, 중심 빛을 켜거나 끕니다. 실제 전투 판정은 유지됩니다.")]
            public bool enabled = true; // 새 중력장 VFX 전체 표시 여부.
            [InspectorName("중력장 전체 색")]
            [Tooltip("입자, 바닥 원, 반구, 중심 빛의 공통 색입니다. 알파 A는 전체 불투명도에도 곱해집니다.")]
            public Color color = new Color(0.6f, 0.3f, 1f, 1f); // 기존 중력장과 같은 보라색.
            [InspectorName("전체 불투명도 배율")]
            [Tooltip("각 요소의 불투명도에 곱하는 값입니다. RGB 발광 강도를 바꾸는 설정은 아닙니다.")]
            [Range(0f, 2f)] public float brightness = 0.7f; // 전체 불투명도 배율.
            [InspectorName("나타나는 시간 (초)")]
            [Tooltip("생성 후 서서히 나타나는 시간입니다. 0은 즉시 표시합니다.")]
            [Min(0f)] public float fadeIn = 0.12f; // 생성 시 나타나는 시간(초).
            [InspectorName("사라지는 시간 (초)")]
            [Tooltip("실제 중력장 수명 끝에서 이 시간만큼 미리 흐려집니다. 유지 시간은 퍽에서 정합니다.")]
            [Min(0f)] public float fadeOut = 0.3f; // 실제 중력장 소멸 직전 사라지는 시간(초).
            [InspectorName("선과 반구 머티리얼 (선택)")]
            [Tooltip("비우면 기본 머티리얼입니다. 지정 시 정점 색과 투명도를 지원해야 합니다.")]
            public Material material; // 선택 사항: 정점 색상과 투명도를 지원하는 선/반구 머티리얼.

            [Header("흡입 입자 (크기와 길이는 월드 단위)")]
            [InspectorName("기존 낮은 흡입 입자 개수")]
            [Tooltip("기존 낮은 높이에서 생성되는 흡입 입자 수입니다. 반구 입자는 아래 항목으로 별도 조절합니다. 다음 착탄부터 적용됩니다.")]
            [Range(0, 256)] public int particleCount = 40; // 동시 흡입 입자 수. 0이면 생략.
            [InspectorName("위쪽 반구 흡입 입자 개수")]
            [Tooltip("위쪽 반구 내부 여러 높이에서 생겨 바닥 중앙으로 내려오는 추가 입자 수입니다. 기존 입자의 굵기·길이·속도·색을 공유합니다. 0이면 추가 입자를 숨깁니다.")]
            [Range(0, 256)] public int hemisphereParticleCount = 32;
            [InspectorName("반구 입자 길이 배율"), Tooltip("기존 입자 길이에 곱해 위쪽에서 내려오는 흐름을 더 잘 보이게 합니다. 기존 낮은 입자는 바뀌지 않습니다."), Min(0f)]
            public float hemisphereLengthScale = 2.5f;
            [InspectorName("입자 굵기 (X 최소 / Y 최대)")]
            [Tooltip("입자의 두께를 키우려면 X와 Y를 함께 올리세요. 각 입자는 이 범위에서 무작위 굵기로 생성됩니다. 월드 단위이며 다음 착탄부터 적용됩니다.")]
            public Vector2 particleWidth = new Vector2(0.012f, 0.028f); // 입자별 무작위 굵기 범위.
            [InspectorName("입자 길이 (X 바깥 / Y 중심)")]
            [Tooltip("X는 범위 바깥에서 시작할 때 길이, Y는 중심에 도달할 때 길이입니다. 무작위 최소/최대가 아닙니다. 둘 다 올리면 입자가 길어집니다. 월드 단위입니다.")]
            public Vector2 particleLength = new Vector2(0.06f, 0.22f); // 바깥쪽/중심 쪽 입자 길이.
            [InspectorName("흡입 반복 속도 (X 최소 / Y 최대)")]
            [Tooltip("각 입자가 초당 중심으로 이동하는 반복 횟수의 무작위 범위입니다. 클수록 빠릅니다. 다음 착탄부터 적용됩니다.")]
            public Vector2 particleSpeed = new Vector2(0.8f, 1.5f); // 입자별 초당 흡입 반복 횟수.
            [InspectorName("입자 시작 높이 (X 최소 / Y 최대)")]
            [Tooltip("입자가 생성되는 높이의 무작위 범위입니다. 실제 중력장 반경 안으로 제한됩니다. 월드 단위이며 다음 착탄부터 적용됩니다.")]
            public Vector2 particleHeight = new Vector2(0.05f, 1.2f); // 생성 높이. 실제 범위 내부로 제한.
            [InspectorName("입자가 멈추는 중심 거리")]
            [Tooltip("입자가 중심에 모일 때 남겨두는 수평 거리입니다. 0이면 중심까지 모입니다. 월드 단위입니다.")]
            [Min(0f)] public float particleEndRadius = 0.12f; // 중심에 도달할 때 남는 거리.
            [InspectorName("흡입 곡선 휘어짐 (라디안)")]
            [Tooltip("이동 중 원주 방향으로 휘는 각도입니다. 0은 직선 흡입이며 음수는 반대 방향으로 휘어집니다.")]
            public float particleBend = 0.28f; // 흡입 중 휘어지는 각도(라디안).
            [InspectorName("입자 불투명도")]
            [Tooltip("0은 투명, 1은 최대 불투명도입니다. 전체 색의 알파와 전체 불투명도 배율도 함께 적용됩니다.")]
            [Range(0f, 1f)] public float particleOpacity = 0.75f; // 입자 불투명도.

            [Header("바닥 원 / 위쪽 반구")]
            [InspectorName("바닥 표시 높이")]
            [Tooltip("바닥 원과 반구가 바닥과 겹치지 않도록 띄우는 높이입니다. 월드 단위이며 다음 착탄부터 적용됩니다.")]
            [Min(0f)] public float groundOffset = 0.025f; // 표시 표면이 바닥과 겹치지 않도록 띄우는 높이.
            [InspectorName("바닥 원 굵기")]
            [Tooltip("범위 바닥 원의 선 두께입니다. 0이면 숨깁니다. 월드 단위이며 다음 착탄부터 적용됩니다.")]
            [Min(0f)] public float ringWidth = 0.018f; // 바닥 원 두께. 0이면 숨김.
            [InspectorName("바닥 원 불투명도")]
            [Tooltip("바닥 원이 보이는 진하기입니다. 0이면 투명합니다.")]
            [Range(0f, 1f)] public float ringOpacity = 0.15f; // 바닥 원 불투명도.
            [InspectorName("위쪽 반구 표면 표시")]
            [Tooltip("반투명 반구 표면을 켜거나 끕니다. 반원 윤곽선은 별도 설정입니다. 다음 착탄부터 적용됩니다.")]
            public bool showDome = true; // 위쪽 반투명 반구 표시.
            [InspectorName("반구 표면 불투명도")]
            [Tooltip("위쪽 반구 표면이 보이는 진하기입니다. 실제 반경은 퍽의 범위를 따릅니다.")]
            [Range(0f, 1f)] public float domeOpacity = 0.1f; // 반구 표면 불투명도.
            [InspectorName("반원 윤곽선 굵기")]
            [Tooltip("위로 솟은 두 반원 선의 두께입니다. 0이면 숨깁니다. 월드 단위이며 다음 착탄부터 적용됩니다.")]
            [Min(0f)] public float arcWidth = 0.012f; // 반원 윤곽선 두께. 0이면 숨김.
            [InspectorName("반원 윤곽선 불투명도")]
            [Tooltip("위로 솟은 두 반원 선이 보이는 진하기입니다.")]
            [Range(0f, 1f)] public float arcOpacity = 0.2f; // 반원 윤곽선 불투명도.

            [Header("중심 빛")]
            [InspectorName("중심 빛 텍스처")]
            [Tooltip("중심에 표시할 빛 모양입니다. 다음 착탄부터 적용됩니다.")]
            public Texture flare; // 중심 빛 텍스처. 기존 피격 VFX의 Flare를 기본 연결.
            [InspectorName("중심 빛 지름")]
            [Tooltip("중심 빛 파티클의 기본 지름입니다. 흡입 입자 크기와 별개이며 0이면 숨깁니다. 월드 단위입니다.")]
            [Min(0f)] public float coreSize = 0.24f; // 중심 빛 지름. 0이면 숨김.
            [InspectorName("중심 빛 높이")]
            [Tooltip("중심 빛과 흡입 입자가 모이는 높이입니다. 실제 반경 내부로 제한됩니다. 월드 단위입니다.")]
            [Min(0f)] public float coreHeight = 0.22f; // 중심 빛과 입자가 모이는 높이.
            [InspectorName("중심 빛 불투명도")]
            [Tooltip("중심 빛의 기본 진하기입니다. 맥동 변화 폭이 여기에 더해집니다.")]
            [Range(0f, 1f)] public float coreOpacity = 0.5f; // 중심 빛 불투명도.
            [InspectorName("중심 빛 크기 변화 폭")]
            [Tooltip("맥동할 때 기본 지름에 더하거나 빼는 크기입니다. 0이면 크기가 고정됩니다. 월드 단위입니다.")]
            [Min(0f)] public float corePulseSize = 0.03f; // 크기 맥동 폭.
            [InspectorName("중심 빛 불투명도 변화 폭")]
            [Tooltip("맥동할 때 기본 불투명도에 더하거나 빼는 값입니다. 0이면 진하기가 고정됩니다.")]
            [Range(0f, 1f)] public float corePulseOpacity = 0.1f; // 밝기 맥동 폭.
            [InspectorName("중심 빛 맥동 속도 (라디안/초)")]
            [Tooltip("빛의 크기와 진하기가 반복해서 변하는 속도입니다. 클수록 빠릅니다.")]
            [Min(0f)] public float corePulseSpeed = 12f; // 맥동 속도(라디안/초).
        }

        // 2026-11-23: 참조 이미지의 어두운 구체/청록 테두리/보라 회전 띠를 조절합니다.
        // 크기와 띠 두께/꼬리 길이는 모두 실제 중력장 반경에 비례합니다. 변경은 다음 발사부터 적용합니다.
        [Serializable]
        public sealed class GravityProjectileSettings
        {
            // 2026-11-23: 한국어 표시명/설명만 추가하고 저장 필드명과 기존 수치는 유지합니다.
            [InspectorName("비행 중력탄 표시")]
            [Tooltip("발사되어 날아가는 탄의 전용 외형을 켜거나 끕니다. 충돌 판정은 유지됩니다.")]
            public bool enabled = true; // 비행 중 전용 VFX 표시 여부.
            [InspectorName("탄 크기의 기준 반경")]
            [Tooltip("실제 중력장 반경이 이 값일 때 아래 기준 지름을 사용합니다. 실제 탄 지름 = 기준 지름 × 실제 반경 ÷ 기준 반경입니다. 다음 발사부터 적용됩니다.")]
            [Min(0.001f)] public float referenceRadius = 6f; // 아래 지름의 기준이 되는 중력장 반경.
            [InspectorName("비행탄 기준 지름")]
            [Tooltip("날아가는 탄 전체를 키우려면 이 값을 올리세요. 구체와 회전 띠, 꼬리가 함께 커집니다. 충돌 판정과 착탄 범위는 바뀌지 않습니다. 월드 단위이며 다음 발사부터 적용됩니다.")]
            [Min(0.001f)] public float diameter = 0.7f; // 기준 반경일 때 탄 전체 지름(월드 단위).
            [InspectorName("중심 구체 색")]
            [Tooltip("날아가는 탄 중심의 어두운 구체 색입니다. 다음 발사부터 적용됩니다.")]
            public Color coreColor = new Color(0.025f, 0.015f, 0.09f, 1f); // 짙은 남보라 중심.
            [InspectorName("중심 구체 크기 비율")]
            [Tooltip("탄 전체 지름에 대한 중심 구체 지름 비율입니다. 다음 발사부터 적용됩니다.")]
            [Range(0.1f, 1f)] public float coreRatio = 0.8f; // 전체 지름 대비 중심 구체 비율.
            [InspectorName("발광 테두리 색")]
            [Tooltip("중심 구체를 감싸는 빛의 색과 불투명도입니다. HDR 강도를 조절할 수 있습니다. 다음 발사부터 적용됩니다.")]
            [ColorUsage(true, true)] public Color rimColor = new Color(0.15f, 1.1f, 1.8f, 0.45f); // 청록 발광 테두리.
            [InspectorName("테두리 크기 배율")]
            [Tooltip("중심 구체 지름에 곱하는 테두리 크기입니다. 1은 구체와 같은 지름입니다. 다음 발사부터 적용됩니다.")]
            [Range(1f, 1.5f)] public float rimRatio = 1.08f; // 중심 구체 대비 테두리 지름.
            [InspectorName("회전 띠 개수")]
            [Tooltip("탄을 감싸는 에너지 띠의 개수입니다. 0이면 숨깁니다. 다음 발사부터 적용됩니다.")]
            [Range(0, 8)] public int ribbonCount = 3; // 구체를 감싸는 에너지 띠 수.
            [InspectorName("회전 띠 굵기 비율")]
            [Tooltip("탄 전체 지름에 대한 띠 두께 비율입니다. 띠만 두껍게 하려면 이 값을 올리세요. 다음 발사부터 적용됩니다.")]
            [Min(0f)] public float ribbonWidth = 0.035f; // 전체 탄 지름 대비 띠 두께 비율.
            [InspectorName("회전 띠 길이 비율")]
            [Tooltip("한 띠가 원을 감싸는 비율입니다. 1은 한 바퀴, 0.5는 반 바퀴입니다. 다음 발사부터 적용됩니다.")]
            [Range(0.1f, 1f)] public float ribbonArc = 0.8f; // 띠 한 줄이 원을 감싸는 비율.
            [InspectorName("회전 속도 (도/초)")]
            [Tooltip("띠와 꼬리가 회전하는 속도입니다. 음수는 반대 방향입니다. 다음 발사부터 적용됩니다.")]
            public float spinSpeed = 240f; // 띠 회전 속도(도/초), 음수는 반대 방향.
            [InspectorName("띠와 꼬리 머리 색")]
            [Tooltip("에너지 띠와 꼬리 시작점의 색과 불투명도입니다. 다음 발사부터 적용됩니다.")]
            [ColorUsage(true, true)] public Color ribbonHead = new Color(0.2f, 1.1f, 1.6f, 0.9f); // 띠 머리 색.
            [InspectorName("띠와 꼬리 끝 색")]
            [Tooltip("에너지 띠와 꼬리 끝의 색과 불투명도입니다. 알파 A가 0이면 끝이 투명합니다. 다음 발사부터 적용됩니다.")]
            [ColorUsage(true, true)] public Color ribbonTail = new Color(0.8f, 0.15f, 1f, 0f); // 띠 끝 색/불투명도.
            [InspectorName("꼬리 길이 비율")]
            [Tooltip("탄 전체 지름에 대한 뒤쪽 꼬리 길이 비율입니다. 0이면 꼬리를 숨깁니다. 다음 발사부터 적용됩니다.")]
            [Min(0f)] public float tailLength = 1.8f; // 전체 탄 지름 대비 뒤쪽 꼬리 길이.
            [InspectorName("꼬리 굵기 비율")]
            [Tooltip("탄 전체 지름에 대한 꼬리 두께 비율입니다. 0이면 꼬리를 숨깁니다. 다음 발사부터 적용됩니다.")]
            [Min(0f)] public float tailWidth = 0.06f; // 전체 탄 지름 대비 꼬리 두께.
        }

        // 2026-11-23: 일반탄의 빨간 궤적과 FlameAmmo 참조의 화염 폭발을 조절합니다.
        // 아래 값은 외형만 변경하며 실제 범위/피해/발사 속도는 폭발탄 퍽 설정을 따릅니다.
        [Serializable]
        public sealed class ExplosiveAmmoSettings
        {
            [Header("일반탄과 같은 궤적 / 빨간색")]
            [InspectorName("빨간 비행 궤적 표시"), Tooltip("일반 총알과 같은 길이에 전용 굵기 배율을 적용합니다. 다음 발사부터 적용됩니다.")]
            public bool projectileEnabled = true;
            [InspectorName("비행 궤적 굵기 배율"), Tooltip("일반 총알 궤적 굵기에 곱합니다. 폭발탄에만 적용됩니다.")]
            [Min(0f)] public float projectileWidthScale = 1.5f;
            [InspectorName("궤적 머리 색"), Tooltip("총알 쪽 빨간색과 불투명도입니다. 일반탄 색은 바뀌지 않습니다.")]
            [ColorUsage(true, true)] public Color projectileHead = new Color(1.6f, 0.08f, 0.03f, 1f);
            [InspectorName("궤적 꼬리 색"), Tooltip("궤적 끝의 색과 불투명도입니다. 알파 A가 0이면 끝이 투명합니다.")]
            [ColorUsage(true, true)] public Color projectileTail = new Color(0.7f, 0.01f, 0f, 0f);

            [Header("착탄 화염구 / 다음 폭발부터 적용")]
            [InspectorName("화염 폭발 표시"), Tooltip("착탄 시 화염과 불씨를 표시합니다. 끄더라도 실제 폭발 피해는 유지됩니다.")]
            public bool impactEnabled = true;
            [InspectorName("폭발 연출 시간 (초)"), Tooltip("불꽃이 팽창하고 사라질 때까지의 게임 시간입니다. 지속 피해 시간은 아닙니다.")]
            [Min(0.05f)] public float duration = 0.7f;
            [InspectorName("폭발 외형 크기 배율"), Tooltip("실제 폭발 반경에 곱하는 외형 배율입니다. 실제 피해 범위는 바뀌지 않습니다.")]
            [Min(0.01f)] public float radiusScale = 1f;
            [InspectorName("불꽃 덩어리 개수"), Tooltip("화염구를 구성하는 불꽃 수입니다. 많을수록 풍성하지만 겹침과 렌더링 비용이 증가합니다.")]
            [Range(1, 64)] public int flameCount = 20;
            [InspectorName("중심 고열 색"), Tooltip("초반 화염구 중심의 밝은 색입니다. HDR 강도로 발광 밝기를 조절합니다.")]
            [ColorUsage(true, true)] public Color hotColor = new Color(3f, 2.5f, 1.1f, 1f);
            [InspectorName("주 불꽃 색"), Tooltip("화염구 중간의 노란 주황색입니다. HDR 강도를 조절할 수 있습니다.")]
            [ColorUsage(true, true)] public Color flameColor = new Color(1.8f, 0.45f, 0.025f, 1f);
            [InspectorName("불꽃 가장자리 색"), Tooltip("화염구 가장자리와 식어가는 불꽃 색입니다. 알파 A는 전체 화염 불투명도에 곱해집니다.")]
            [ColorUsage(true, true)] public Color edgeColor = new Color(0.65f, 0.035f, 0.005f, 0.85f);
            [InspectorName("화염 무늬 촘촘함"), Tooltip("불꽃 표면 요철의 빈도입니다. 클수록 작은 무늬가 많아집니다.")]
            [Range(1f, 16f)] public float noiseScale = 5f;
            [InspectorName("불꽃 윤곽 일렁임"), Tooltip("화염구 가장자리를 불규칙하게 만드는 강도입니다. 0은 매끈한 윤곽입니다.")]
            [Range(0f, 1f)] public float distortion = 0.45f;
            [InspectorName("불꽃 무늬 흐름 속도"), Tooltip("표면 무늬가 위로 흐르는 속도입니다. 게임 일시정지 중에는 멈춥니다.")]
            [Min(0f)] public float flameSpeed = 2.5f;
            [InspectorName("상승 높이 비율"), Tooltip("폭발 반경 대비 불꽃이 위로 떠오르는 높이입니다. 0이면 중심에서 퍼지기만 합니다.")]
            [Min(0f)] public float rise = 0.35f;

            [Header("바깥으로 튀는 불씨")]
            [InspectorName("불씨 개수"), Tooltip("폭발 순간 바깥으로 퍼지는 짧은 불씨 선의 수입니다. 0이면 생략합니다.")]
            [Range(0, 128)] public int sparkCount = 32;
            [InspectorName("불씨 굵기"), Tooltip("불씨 선의 굵기입니다. 월드 단위이며 0이면 보이지 않습니다.")]
            [Min(0f)] public float sparkWidth = 0.035f;
            [InspectorName("불씨 길이 비율"), Tooltip("폭발 반경에 대한 불씨 한 줄의 길이 비율입니다.")]
            [Min(0f)] public float sparkLength = 0.12f;
        }

        // 2026-11-23: GrenadeAmmo 참조의 화염 기둥/검은 연기/바닥 먼지와 초록 궤적을 설정합니다.
        // 실제 피해 반경/포물선/충돌 판정은 유탄 퍽과 투사체 코드가 담당합니다.
        [Serializable]
        public sealed class GrenadeAmmoSettings
        {
            [Header("비행 궤적 / 다음 발사부터 적용")]
            [InspectorName("초록 비행 궤적 표시"), Tooltip("폭발탄과 같은 일반탄 선 궤적을 초록색으로 표시합니다. 길이/굵기는 일반 총알 궤적 설정을 공유합니다.")]
            public bool projectileEnabled = true;
            [InspectorName("궤적 머리 색"), Tooltip("날아가는 탄 쪽의 색과 불투명도입니다. 일반탄과 폭발탄의 색은 바뀌지 않습니다.")]
            [ColorUsage(true, true)] public Color projectileHead = new Color(0.15f, 1.5f, 0.08f, 1f);
            [InspectorName("궤적 꼬리 색"), Tooltip("궤적 끝의 색입니다. 알파 A가 0이면 투명하게 사라집니다.")]
            [ColorUsage(true, true)] public Color projectileTail = new Color(0.02f, 0.5f, 0.01f, 0f);

            [Header("화염 기둥 / 다음 폭발부터 적용")]
            [InspectorName("유탄 폭발 표시"), Tooltip("화염/연기/먼지/불씨를 표시합니다. 끄더라도 기존 범위 피해는 유지됩니다.")]
            public bool impactEnabled = true;
            [InspectorName("전체 연출 시간 (초)"), Tooltip("연기와 먼지까지 사라지는 게임 시간입니다. 화염이 더 길면 화염 종료까지 유지합니다. 지속 피해 시간은 아닙니다.")]
            [Min(0.1f)] public float duration = 1.8f;
            [InspectorName("화염 시간 (초)"), Tooltip("밝은 화염 기둥이 사라지는 시간입니다. 연기는 전체 연출 시간 동안 남습니다.")]
            [Min(0.05f)] public float fireDuration = 0.65f;
            [InspectorName("폭발 외형 크기 배율"), Tooltip("실제 유탄 반경에 곱하는 외형 크기입니다. 작게 보려면 줄이세요. 실제 피해 범위는 바뀌지 않습니다.")]
            [Min(0.01f)] public float radiusScale = 1f; // 2026-11-23: 실제 1레벨 반경에 현재 외형 크기를 반영했으므로 기본 배율은 1입니다.
            [InspectorName("화염 기둥 세로 배율"), Tooltip("불꽃이 위로 솟는 정도입니다. 1은 둥근 폭발, 클수록 위로 긴 화염입니다.")]
            [Min(1f)] public float columnHeight = 2.5f;
            [InspectorName("불꽃 덩어리 개수"), Tooltip("화염을 구성하는 불꽃 수입니다. 많을수록 풍성하고 렌더링 비용도 증가합니다.")]
            [Range(1, 64)] public int flameCount = 20;
            [InspectorName("중심 고열 색"), Tooltip("화염 중심의 밝은 노란 흰색입니다. HDR 강도로 밝기를 조절합니다.")]
            [ColorUsage(true, true)] public Color hotColor = new Color(3f, 2.7f, 1.4f, 1f);
            [InspectorName("주 불꽃 색"), Tooltip("기둥을 이루는 노란 주황색과 불투명도입니다.")]
            [ColorUsage(true, true)] public Color flameColor = new Color(2f, 0.6f, 0.025f, 1f);
            [InspectorName("불꽃 가장자리 색"), Tooltip("화염 가장자리의 붉은 주황색입니다.")]
            [ColorUsage(true, true)] public Color edgeColor = new Color(0.8f, 0.06f, 0.005f, 0.9f);
            [InspectorName("화염 무늬 촘촘함"), Tooltip("클수록 작은 불꽃 무늬가 많아집니다.")]
            [Range(1f, 16f)] public float noiseScale = 5f;
            [InspectorName("불꽃 윤곽 일렁임"), Tooltip("불꽃 윤곽의 불규칙함입니다. 0은 매끈한 윤곽입니다.")]
            [Range(0f, 1f)] public float distortion = 0.45f;
            [InspectorName("화염 무늬 흐름 속도"), Tooltip("불꽃 표면 무늬의 이동 속도입니다. 일시정지 중에는 멈춥니다.")]
            [Min(0f)] public float fireSpeed = 2.5f;

            [Header("검은 연기와 바닥 먼지")]
            [InspectorName("연기 덩어리 개수"), Tooltip("위로 올라가는 연기 수입니다. 0이면 연기를 숨깁니다.")]
            [Range(0, 64)] public int smokeCount = 18;
            [InspectorName("연기 색"), Tooltip("검은 연기의 색과 불투명도입니다. 알파 A를 줄이면 연해집니다.")]
            public Color smokeColor = new Color(0.09f, 0.085f, 0.08f, 0.8f);
            [InspectorName("연기 덩어리 크기 비율"), Tooltip("외형 반경 대비 연기 한 덩어리의 기본 지름입니다.")]
            [Min(0.01f)] public float smokeSize = 0.6f;
            [InspectorName("연기 상승 높이 비율"), Tooltip("외형 반경 대비 연기가 위로 올라가는 거리입니다.")]
            [Min(0f)] public float smokeRise = 1.6f;
            [InspectorName("바닥 먼지 개수"), Tooltip("지면을 따라 바깥으로 퍼지는 먼지 덩어리 수입니다. 0이면 숨깁니다.")]
            [Range(0, 64)] public int dustCount = 12;
            [InspectorName("바닥 먼지 색"), Tooltip("먼지의 갈색/회색과 불투명도입니다. 알파 A를 줄이면 연해집니다.")]
            public Color dustColor = new Color(0.28f, 0.22f, 0.15f, 0.65f);
            [InspectorName("바닥 먼지 확산 비율"), Tooltip("외형 반경 대비 먼지가 수평으로 퍼지는 거리입니다. 실제 전투 범위와 별개입니다.")]
            [Min(0f)] public float dustSpread = 1.15f;

            [Header("튀는 불씨")]
            [InspectorName("불씨 개수"), Tooltip("바깥으로 튀는 불씨 수입니다. 0이면 생략합니다.")]
            [Range(0, 128)] public int sparkCount = 24;
            [InspectorName("불씨 굵기"), Tooltip("불씨 선의 두께입니다. 월드 단위입니다.")]
            [Min(0f)] public float sparkWidth = 0.03f;
            [InspectorName("불씨 길이 비율"), Tooltip("외형 반경에 대한 불씨 한 줄의 길이 비율입니다.")]
            [Min(0f)] public float sparkLength = 0.1f;
        }

        // 2026-11-23: PiercingAmmo 참조의 가는 금빛 광선/별 모양 탄두/작은 반짝임을 설정합니다.
        // 모든 값은 외형 전용이며 실제 관통 횟수, 충돌 반경과 피해는 관통탄 퍽을 사용합니다.
        [Serializable]
        public sealed class PiercingAmmoSettings
        {
            [InspectorName("관통탄 비행 VFX 표시"), Tooltip("금빛 광선과 별 모양 탄두를 켜거나 끕니다. 관통 판정과 피해는 유지됩니다. 변경은 다음 발사부터 적용합니다.")]
            public bool enabled = true;
            [Header("진한 금빛 레이저")]
            // 이전 길이 값은 보존하되 발사 위치부터 이어지는 레이저에는 사용하지 않습니다.
            [HideInInspector] public float length = 3f;
            [InspectorName("광선 중심 굵기"), Tooltip("밝은 중심 선의 두께입니다. 월드 단위이며 실제 충돌 반경은 바꾸지 않습니다.")]
            // 기본 일반탄 굵기 0.06의 1.5배입니다. 저장된 에셋은 해당 일반탄 굵기에 맞춥니다.
            [Min(0.001f)] public float width = 0.09f;
            [InspectorName("주변 광채 굵기"), Tooltip("중심 선 바깥의 은은한 금빛 선 두께입니다. 월드 단위입니다.")]
            // 중심과 주변 광채의 기존 비율을 유지합니다.
            [Min(0f)] public float haloWidth = 0.10899083f;
            [InspectorName("광선 중심 색"), Tooltip("밝은 금빛 중심 선의 색과 불투명도입니다. HDR 강도를 조절할 수 있습니다.")]
            [ColorUsage(true, true)] public Color coreColor = new Color(2f, 1.85f, 0.9f, 1f);
            [InspectorName("주변 광채 색"), Tooltip("선 바깥의 부드러운 금빛과 불투명도입니다. 알파 A를 줄이면 은은해집니다.")]
            [ColorUsage(true, true)] public Color haloColor = new Color(1f, 0.7f, 0.15f, 0.22f);
            [Header("별 모양 탄두")]
            [InspectorName("탄두 별 크기"), Tooltip("탄 앞쪽에 붙는 십자/별 빛의 크기입니다. 월드 단위이며 충돌 판정은 바뀌지 않습니다.")]
            [Min(0f)] public float tipSize = 0.35f;
            [InspectorName("탄두 빛 색"), Tooltip("탄두의 밝은 노란 흰색과 불투명도입니다. HDR 강도로 밝기를 조절합니다.")]
            [ColorUsage(true, true)] public Color tipColor = new Color(2.5f, 2.3f, 1.4f, 1f);
            [InspectorName("탄두 맥동 비율"), Tooltip("기본 별 크기에 대한 크기 변화 폭입니다. 0이면 크기가 고정됩니다.")]
            [Range(0f, 0.5f)] public float tipPulse = 0.12f;
            [InspectorName("탄두 맥동 속도 (라디안/초)"), Tooltip("탄두가 반짝이는 속도입니다. 일시정지 중에는 멈춥니다.")]
            [Min(0f)] public float pulseSpeed = 10f;
            [InspectorName("중심 빛 텍스처 (선택)"), Tooltip("별 중앙의 부드러운 빛 모양입니다. 기본은 기존 피격 VFX의 Flare이며 참조 PNG에는 의존하지 않습니다.")]
            public Texture flare;
            [Header("광선 위 작은 반짝임")]
            [InspectorName("작은 별 개수"), Tooltip("광선을 따라 보이는 작은 반짝임의 수입니다. 0이면 생략합니다.")]
            [Range(0, 16)] public int glintCount = 4;
            [InspectorName("작은 별 크기"), Tooltip("작은 십자 빛의 크기입니다. 월드 단위입니다.")]
            [Min(0f)] public float glintSize = 0.09f;
            [InspectorName("작은 별 이동 속도"), Tooltip("작은 별이 광선을 따라 흐르는 반복 속도입니다. 클수록 빠르며 0은 위치 고정입니다.")]
            [Min(0f)] public float glintSpeed = 1.5f;
        }

        // 2026-11-23: 폭발 적은 폭발탄의 화염 재질을 공유하되 독립적인 노란색 구형 팽창 설정을 사용합니다.
        [Serializable]
        public sealed class BomberExplosionSettings
        {
            [InspectorName("자폭 VFX 표시"), Tooltip("폭발 적의 공격 때 재생합니다. 꺼도 기존 범위 피해는 유지됩니다.")]
            public bool enabled = true;
            [InspectorName("폭발 시간 (초)"), Tooltip("노란 화염구가 팽창하고 사라지는 시간입니다.")]
            [Min(0.05f)] public float duration = 0.7f;
            [InspectorName("폭발 외형 크기 배율"), Tooltip("폭발 적의 폭발 VFX 기준 반경에 곱합니다. 실제 피해 반경과 독립적으로 크기를 조절합니다.")]
            [Min(0.01f)] public float radiusScale = 0.5f;
            [InspectorName("구형 불꽃 개수"), Tooltip("구 표면을 균일하게 덮는 둥근 불꽃 개수입니다. 중심 빛은 별도로 1개 생성합니다.")]
            [Range(1, 64)] public int flameCount = 32;
            [InspectorName("중심 밝은 노란빛"), Tooltip("화염구 중심 색입니다. 기본은 밝은 노란 흰색입니다.")]
            [ColorUsage(true, true)] public Color hotColor = new Color(4f, 3.8f, 1.8f, 1f);
            [InspectorName("주요 노란 화염색"), Tooltip("폭발 대부분을 차지하는 색입니다. 폭발탄보다 노란색 비중을 높였습니다.")]
            [ColorUsage(true, true)] public Color flameColor = new Color(3f, 2.4f, 0.12f, 1f);
            [InspectorName("가장자리 황금색"), Tooltip("식어가는 가장자리의 황금색/옅은 주황색입니다.")]
            [ColorUsage(true, true)] public Color edgeColor = new Color(1.6f, 0.75f, 0.025f, 0.85f);
            [InspectorName("표면 무늬 촘촘함"), Tooltip("폭발탄과 같은 화염 노이즈의 빈도입니다.")]
            [Range(1f, 16f)] public float noiseScale = 5f;
            [InspectorName("구 윤곽 일렁임"), Tooltip("낮을수록 둥근 구 형태가 뚜렷하고 높을수록 거친 불꽃입니다.")]
            [Range(0f, 1f)] public float distortion = 0.28f;
            [InspectorName("불꽃 흐름 속도"), Tooltip("표면 화염 무늬의 흐름 속도입니다. 구 전체는 위로 늘어나지 않습니다.")]
            [Min(0f)] public float flameSpeed = 3f;
            [InspectorName("불씨 개수"), Tooltip("바깥으로 퍼지는 짧은 노란 불씨의 수입니다.")]
            [Range(0, 128)] public int sparkCount = 24;
            [InspectorName("불씨 굵기"), Tooltip("불씨 한 줄의 굵기입니다. 월드 단위입니다.")]
            [Min(0f)] public float sparkWidth = 0.04f;
            [InspectorName("불씨 길이 비율"), Tooltip("폭발 외형 반경에 대한 불씨 길이 비율입니다.")]
            [Min(0f)] public float sparkLength = 0.12f;
        }

        [InspectorName("폭발 적 노란 구형 폭발 (Bomber Explosion)")]
        [Tooltip("폭발 적의 자폭 공격 위치에서 노란색 화염구를 재생합니다. 피해 판정은 Bomber의 기존 폭발 범위입니다.")]
        [SerializeField] private BomberExplosionSettings bomberExplosion = new BomberExplosionSettings();
        public BomberExplosionSettings BomberExplosion => bomberExplosion;

        // 2026-11-23: 실제 체력 수치 기준의 약한 피격 흔들림과 화면 가장자리 피 효과입니다.
        [Serializable]
        public sealed class PlayerHitSettings
        {
            [InspectorName("피격 화면 흔들림 사용"), Tooltip("양수 피해를 받았을 때 기존 CameraShake로 짧게 흔듭니다.")]
            public bool shakeEnabled = true;
            [InspectorName("흔들림 세기"), Tooltip("카메라 위치 흔들림의 월드 단위입니다. 기본 0.025로 약하게 설정했습니다.")]
            [Min(0f)] public float shakeAmplitude = 0.025f;
            // 2026-11-23: 위치 이동만으로 식별하기 어려운 피격을 작은 각도 변화로 보완합니다.
            [InspectorName("흔들림 각도 (도)"), Tooltip("카메라 로컬 각도 흔들림입니다. 기본 0.35도로 약하게 보완합니다. 0이면 위치만 흔듭니다.")]
            [Range(0f, 2f)] public float shakeRotation = 0.35f;
            [InspectorName("흔들림 시간 (초)"), Tooltip("피격 한 번의 흔들림 지속 시간입니다.")]
            [Min(0.01f)] public float shakeDuration = 0.12f;
            [InspectorName("체력 부족 피 테두리 사용"), Tooltip("체력이 기준값 이하일 때 화면 가장자리에 옅은 피 효과를 표시합니다.")]
            public bool bloodEnabled = true;
            [InspectorName("피 테두리 시작 체력"), Tooltip("비율이 아닌 실제 HP입니다. 기본 50이며 정확히 50일 때도 나타납니다.")]
            [Min(0.01f)] public float healthThreshold = 50f;
            [InspectorName("시작 불투명도"), Tooltip("기준 체력에서의 피 테두리 진하기입니다. 0이면 보이지 않고 1이면 불투명합니다.")]
            [Range(0f, 1f)] public float thresholdOpacity = 0.14f;
            [InspectorName("최대 불투명도"), Tooltip("체력이 0에 가까워질 때의 진하기입니다. 중앙 시야는 항상 비워둡니다.")]
            [Range(0f, 1f)] public float criticalOpacity = 0.26f;
            [InspectorName("테두리 폭"), Tooltip("화면 가장자리에서 안쪽으로 번지는 비율입니다. 기본 0.17로 좁게 표시합니다.")]
            [Range(0.02f, 0.25f)] public float edgeWidth = 0.17f;
            [InspectorName("피 색상"), Tooltip("가장자리 붉은 얼룩의 색입니다. 알파는 불투명도 설정에 추가로 곱해집니다.")]
            public Color bloodColor = new Color(0.52f, 0.015f, 0.025f, 1f);
            [InspectorName("나타남 / 사라짐 속도"), Tooltip("초당 불투명도 변화량입니다. 회복해 기준 체력을 넘으면 부드럽게 사라집니다.")]
            [Min(0.01f)] public float fadeSpeed = 0.65f;
        }

        [Serializable]
        public sealed class FireRingSettings
        {
            [InspectorName("화염 고리 표시")] public bool enabled = true;
            [InspectorName("화염 띠 너비"), Min(0.05f)] public float bandWidth = 0.85f;
            [InspectorName("불꽃 꼬리 길이 (각도)"), Tooltip("각 중심점 뒤로 뻗는 꼬리의 각도입니다. 기본 96도이며 3개 불꽃 사이에 24도씩 틈이 생깁니다."), Range(10f, 115f)] public float trailArcDegrees = 96f;
            [InspectorName("바깥 불꽃 높이"), Min(0f)] public float flameHeight = 0.5f;
            [InspectorName("불꽃 흐름 속도"), Min(0f)] public float flowSpeed = 1f;
            [InspectorName("노란 중심 색"), ColorUsage(true, true)] public Color hotColor = new Color(1.5f, 0.95f, 0.08f, 1f);
            [InspectorName("주황 불꽃 색"), ColorUsage(true, true)] public Color flameColor = new Color(1.3f, 0.22f, 0.015f, 1f);
            [InspectorName("빨간 가장자리 색"), ColorUsage(true, true)] public Color edgeColor = new Color(0.9f, 0.025f, 0.005f, 1f);
        }

        [InspectorName("플레이어 주변 화염 고리 (Fire Ring)")]
        [Tooltip("외형만 조절합니다. 공전 반경, 속도, 지속 시간과 피해는 화염의 고리 퍽 설정을 따릅니다.")]
        [SerializeField] private FireRingSettings fireRing = new FireRingSettings();
        public FireRingSettings FireRing => fireRing;

        // 2026-11-23: 플레이어 피격 설정 그룹을 공용 Config에 노출합니다.
        [InspectorName("플레이어 피격과 체력 부족 테두리 (Player Hit)")]
        [Tooltip("피격 시 약한 카메라 흔들림과 HP 50 이하의 옅은 피 테두리 설정입니다.")]
        [SerializeField] private PlayerHitSettings playerHit = new PlayerHitSettings();
        public PlayerHitSettings PlayerHit => playerHit;

        // 2026-11-23: 폭발 없이 탄 자체의 모양만 조절하는 관통탄 전용 설정입니다.
        [InspectorName("관통탄 금빛 광선 (Piercing Ammo)")]
        [Tooltip("꼬리까지 진하기와 굵기를 유지하는 금빛 레이저와 별 모양 탄두입니다. 크기/굵기는 외형만 바꾸며 테스트 씬 관통탄 탭에서 확인합니다.")]
        [SerializeField] private PiercingAmmoSettings piercingAmmo = new PiercingAmmoSettings();
        public PiercingAmmoSettings PiercingAmmo => piercingAmmo;

        // 2026-11-23: 유탄 전용 외형 설정을 기존 VFX와 독립적으로 저장합니다.
        [InspectorName("유탄 비행과 화염 기둥 (Grenade Ammo)")]
        [Tooltip("초록 궤적, 화염 기둥, 검은 연기와 바닥 먼지입니다. 테스트 씬 유탄 탭에서 반복 확인합니다.")]
        [SerializeField] private GrenadeAmmoSettings grenadeAmmo = new GrenadeAmmoSettings();
        public GrenadeAmmoSettings GrenadeAmmo => grenadeAmmo;

        // 2026-11-23: 폭발탄의 색과 화염 연출을 한곳에서 설정합니다. 저장된 다른 VFX 수치는 유지합니다.
        [InspectorName("폭발탄 비행과 화염 폭발 (Explosive Ammo)")]
        [Tooltip("비행 궤적은 일반 총알 길이를 공유하며 굵기 배율과 색을 별도로 지정합니다. 폭발 수치는 다음 착탄부터 적용됩니다.")]
        [SerializeField] private ExplosiveAmmoSettings explosiveAmmo = new ExplosiveAmmoSettings();
        public ExplosiveAmmoSettings ExplosiveAmmo => explosiveAmmo;

        // 2026-11-23: 비행 VFX와 착탄 VFX를 같은 Config 안에서 각각 조절합니다.
        // 2026-11-23: 한국어 표시명/설명만 추가하고 저장 필드명과 기존 수치는 유지합니다.
        [InspectorName("중력탄 비행 외형 (Gravity Projectile)")]
        [Tooltip("날아가는 중력탄의 구체, 회전 띠, 꼬리 설정입니다. 변경 후 다시 발사하세요.")]
        [SerializeField] private GravityProjectileSettings gravityProjectile = new GravityProjectileSettings();
        public GravityProjectileSettings GravityProjectile => gravityProjectile;

        // 2026-11-23: Inspector의 Gravity Well에서 모든 중력장 외형 수치를 조절합니다.
        [InspectorName("중력탄 착탄과 흡입 입자 (Gravity Well)")]
        [Tooltip("착탄 후 입자와 반구, 바닥 원, 중심 빛 설정입니다. 입자 크기는 입자 굵기와 입자 길이에서 조절합니다. 변경 후 다시 착탄시키거나 테스트 씬을 재생하세요. 실제 반경은 Special_Graviton 퍽에서 조절합니다.")]
        [SerializeField] private GravityWellSettings gravityWell = new GravityWellSettings();
        public GravityWellSettings GravityWell => gravityWell;

        [InspectorName("카드 흡수와 총 발광 (Perk Absorb)")]
        [Tooltip("카드가 총으로 이동하는 연출과 흡수 후 총 발광 설정입니다.")]
        [SerializeField] private PerkAbsorbSettings perkAbsorb = new PerkAbsorbSettings();
        public PerkAbsorbSettings PerkAbsorb => perkAbsorb;

        [InspectorName("일반 총알 궤적 (Bullet Trail)")]
        [Tooltip("일반 총알 뒤에 남는 선 궤적 설정입니다.")]
        [SerializeField] private BulletTrailSettings bulletTrail = new BulletTrailSettings();
        public BulletTrailSettings BulletTrail => bulletTrail;

        [InspectorName("피격 등 프리팹 연출 목록 (Entries)")]
        [Tooltip("상황별 연출 프리팹 목록입니다. 같은 종류의 유효한 항목 중 하나를 무작위로 재생합니다.")]
        [SerializeField] private Entry[] entries = { new Entry { id = VfxId.EnemyHit } };

        // 2026-11-09: 같은 ID의 유효한 프리팹 중 하나를 같은 확률로 선택합니다.
        // 빈 항목은 제외하고, 각 후보의 크기/수명/회전 설정도 함께 반환합니다.
        public bool TryGetRandom(VfxId id, out Entry entry)
        {
            entry = null;
            int candidateCount = 0;
            if (entries != null)
            {
                foreach (Entry candidate in entries)
                {
                    if (candidate != null && candidate.id == id && candidate.prefab != null)
                    {
                        candidateCount++;
                    }
                }

                if (candidateCount == 0) return false;

                // 2026-11-09: 매 피격마다 독립 선택하므로 같은 이펙트가 연속으로 나올 수 있습니다.
                int selectedIndex = UnityEngine.Random.Range(0, candidateCount);
                foreach (Entry candidate in entries)
                {
                    if (candidate == null || candidate.id != id || candidate.prefab == null) continue;
                    if (selectedIndex-- != 0) continue;

                    entry = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
