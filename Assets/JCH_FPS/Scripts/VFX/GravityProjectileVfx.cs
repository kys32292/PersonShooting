using UnityEngine;
using UnityEngine.Rendering;

namespace PesonShooting.VFX
{
    // 2026-11-23: 로컬 참조 이미지를 런타임에 읽지 않고 구체/곡선으로 비행 중력탄을 구성합니다.
    // 부모 투사체의 이동과 수명을 따르며 모든 자식은 시각 효과 전용입니다.
    public sealed class GravityProjectileVfx : MonoBehaviour
    {
        private VfxConfig.GravityProjectileSettings settings;
        private Material coreMaterial, rimMaterial, lineMaterial;
        // 2026-11-23: 로컬 곡선을 보관해 새 오브젝트를 만들지 않고 궤도 띠와 꼬리만 갱신합니다.
        private LineRenderer[] ribbons;
        private LineRenderer tail;
        private float elapsed;
        private const int Segments = 40;

        // 2026-11-23: 레벨별 실제 반경 / 기준 반경 비율로 구체와 띠, 꼬리를 함께 확대합니다.
        public void Initialize(float radius, VfxConfig.GravityProjectileSettings config)
        {
            settings = config;
            transform.localScale = Vector3.one * Mathf.Max(0.001f, config.diameter)
                * Mathf.Max(0.001f, radius) / Mathf.Max(0.001f, config.referenceRadius);
            coreMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            coreMaterial.SetColor("_BaseColor", config.coreColor);
            rimMaterial = new Material(Shader.Find("PersonShooting/VFX/UpgradeFlash"));
            rimMaterial.SetColor("_FlashColor", config.rimColor);
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            CreateSphere("Dark indigo core", config.coreRatio, coreMaterial);
            CreateSphere("Cyan rim", config.coreRatio * config.rimRatio, rimMaterial);

            // 2026-11-23: 구체/띠 개수는 발사 때 확정하며 레벨 반경에 따른 부모 배율을 함께 적용합니다.
            ribbons = new LineRenderer[Mathf.Clamp(config.ribbonCount, 0, 8)];
            for (int i = 0; i < ribbons.Length; i++)
                ribbons[i] = CreateLine("Orbit ribbon " + i, config.ribbonWidth);
            tail = CreateLine("Trailing energy", config.tailWidth);
            DrawCurves();
        }

        // 2026-11-23: Primitive의 기본 콜라이더는 즉시 끄고 제거하여 자기 탄에 피격되지 않게 합니다.
        private void CreateSphere(string label, float diameter, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = label;
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * Mathf.Max(0.001f, diameter);
            Collider collider = sphere.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            Renderer renderer = sphere.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // 2026-11-23: 곡선을 로컬 좌표로 그려 탄과 함께 움직이고, 폭도 반경 비율로 확대합니다.
        private LineRenderer CreateLine(string label, float width)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = false;
            line.positionCount = Segments + 1;
            line.widthMultiplier = Mathf.Max(0f, width) * transform.localScale.x;
            line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            line.startColor = settings.ribbonHead;
            line.endColor = settings.ribbonTail;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        // 2026-11-23: 게임 일시정지 중에는 회전도 멈춥니다. 구체 자체는 확대/축소하지 않습니다.
        private void Update()
        {
            if (settings == null || SpecialAttackUtil.IsPaused) return;
            elapsed += Time.deltaTime;
            DrawCurves();
        }

        // 2026-11-23: 서로 기울어진 부분 원 궤도로 감기는 느낌을 주고, 꼬리는 진행 방향 뒤(-Z)에 둡니다.
        private void DrawCurves()
        {
            for (int i = 0; i < ribbons.Length; i++)
            {
                Quaternion tilt = Quaternion.Euler(35f + i * 41f, i * 67f, 0f);
                float phase = (elapsed * settings.spinSpeed * (i % 2 == 0 ? 1f : -1f) + i * 120f) * Mathf.Deg2Rad;
                for (int j = 0; j <= Segments; j++)
                {
                    float angle = phase + j / (float)Segments * Mathf.PI * 2f * settings.ribbonArc;
                    Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.5f;
                    ribbons[i].SetPosition(j, tilt * point);
                }
            }
            for (int j = 0; j <= Segments; j++)
            {
                float t = j / (float)Segments;
                float angle = elapsed * settings.spinSpeed * Mathf.Deg2Rad - t * Mathf.PI * 2f;
                float spread = Mathf.Sin(t * Mathf.PI) * 0.18f;
                tail.SetPosition(j, new Vector3(Mathf.Cos(angle) * spread, Mathf.Sin(angle) * spread,
                    -0.25f - t * Mathf.Max(0f, settings.tailLength)));
            }
            tail.enabled = settings.tailLength > 0f && settings.tailWidth > 0f;
        }

        // 2026-11-23: 발사마다 만든 머티리얼만 정리하며 공유 에셋은 건드리지 않습니다.
        private void OnDestroy()
        {
            if (coreMaterial != null) Destroy(coreMaterial);
            if (rimMaterial != null) Destroy(rimMaterial);
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
