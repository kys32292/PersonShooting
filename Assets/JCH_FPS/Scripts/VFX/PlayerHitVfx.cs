using UnityEngine;
using UnityEngine.UI;

namespace PesonShooting.VFX
{
    // 2026-11-23: 플레이어 수명에 연결된 화면 효과입니다. 피해/체력 계산과 입력은 변경하지 않습니다.
    public sealed class PlayerHitVfx : MonoBehaviour
    {
        // 2026-11-23: 공용 설정은 참조만 하고 화면 캔버스/머티리얼/페이드 상태는 이 플레이어가 소유합니다.
        private VfxConfig config;
        private Canvas overlay;
        private RawImage image;
        private Material material;
        private float health, opacity;
        // 2026-11-23: 플레이어가 보는 카메라를 직접 보관해 태그 누락/다중 카메라에서도 대상을 명확히 합니다.
        private Camera targetCamera;
        // 2026-11-23: 매 프레임 문자열 조회를 반복하지 않도록 셰이더 속성 ID를 캐시합니다.
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");
        private static readonly int EdgeWidth = Shader.PropertyToID("_EdgeWidth");
        private static readonly int BloodColor = Shader.PropertyToID("_BloodColor");

        // 2026-11-23: Resources 전용 셰이더로 참조 이미지에 의존하지 않고 중앙이 투명한 옅은 피 테두리를 그립니다.
        public void Initialize(float currentHealth, Camera camera = null)
        {
            health = currentHealth;
            targetCamera = camera != null ? camera : GetComponentInChildren<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
            config = Resources.Load<VfxConfig>("VFX/VfxConfig");
            Shader shader = Resources.Load<Shader>("VFX/PlayerBloodEdge");
            if (shader == null || config == null) return;
            material = new Material(shader);
            var root = new GameObject("Subtle player blood screen edge", typeof(Canvas));
            root.transform.SetParent(transform, false);
            overlay = root.GetComponent<Canvas>();
            overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            // 2026-11-23: 일반 HUD보다 아래에 그리며 Raycaster를 만들지 않아 UI 클릭을 가로채지 않습니다.
            overlay.sortingOrder = -10;
            var graphic = new GameObject("Blood edge (input transparent)", typeof(RectTransform), typeof(RawImage));
            graphic.transform.SetParent(root.transform, false);
            image = graphic.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            material.SetFloat(Opacity, 0f);
            image.enabled = false;
        }

        // 2026-11-23: 회복/최대 체력 변경도 같은 갱신 경로를 사용하며 사망 시 테두리를 즉시 숨깁니다.
        public void SetHealth(float currentHealth)
        {
            health = currentHealth;
            if (health <= 0f)
            {
                opacity = 0f;
                if (image != null) image.enabled = false;
            }
        }

        // 2026-11-23: 보스 피격 등 기존 강한 흔들림과 합쳐질 때는 CameraShake의 더 큰 세기 유지 규칙을 사용합니다.
        public void PlayHit()
        {
            if (config == null || SpecialAttackUtil.IsPaused) return;
            VfxConfig.PlayerHitSettings settings = config.PlayerHit;
            if (settings.shakeEnabled && (settings.shakeAmplitude > 0f || settings.shakeRotation > 0f))
                CameraShake.Shake(targetCamera, settings.shakeAmplitude, Mathf.Max(0.01f, settings.shakeDuration), settings.shakeRotation);
        }

        // 2026-11-23: Config의 실시간 편집값을 읽어 회복/저체력 상태를 부드럽게 전환합니다.
        private void Update()
        {
            if (image == null || config == null || SpecialAttackUtil.IsPaused) return;
            VfxConfig.PlayerHitSettings settings = config.PlayerHit;
            float threshold = Mathf.Max(0.01f, settings.healthThreshold);
            // 2026-11-23: HP 50을 포함한 이하에서만 표시하며 50% 같은 체력 비율 조건은 사용하지 않습니다.
            float target = settings.bloodEnabled && health > 0f && health <= threshold
                ? Mathf.Lerp(settings.thresholdOpacity, settings.criticalOpacity, 1f - Mathf.Clamp01(health / threshold)) : 0f;
            opacity = Mathf.MoveTowards(opacity, target, Mathf.Max(0.01f, settings.fadeSpeed) * Time.deltaTime);
            image.enabled = opacity > 0.001f;
            material.SetFloat(Opacity, opacity);
            material.SetFloat(EdgeWidth, Mathf.Clamp(settings.edgeWidth, 0.02f, 0.25f));
            material.SetColor(BloodColor, settings.bloodColor);
        }

        // 2026-11-23: 플레이어 비활성화/씬 종료 시 화면 효과와 생성한 머티리얼을 정리합니다.
        private void OnDisable() { if (overlay != null) overlay.enabled = false; }
        private void OnEnable() { if (overlay != null) overlay.enabled = true; }
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
