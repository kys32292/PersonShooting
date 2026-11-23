using UnityEngine;
using UnityEngine.Rendering;

namespace PesonShooting.VFX
{
    // 2026-11-23: FlameAmmo 참조의 밝은 화염구를 텍스처 없이 겹친 불꽃과 불씨로 구성합니다.
    // 전투 판정/지속 피해/물리 콜라이더를 만들지 않으며 폭발 위치에서 한 번 재생합니다.
    public sealed class ExplosiveImpactVfx : MonoBehaviour
    {
        private VfxConfig.ExplosiveAmmoSettings settings;
        private ParticleSystem flames;
        private ParticleSystem.Particle[] particles;
        // 2026-11-23: 팽창 방향/크기/지연을 발동 때 저장해 매 프레임 불꽃 모양이 무작위로 튀지 않게 합니다.
        private Vector3[] directions;
        private float[] sizes, delays;
        private LineRenderer[] sparks;
        private Vector3[] sparkDirections;
        private float[] sparkSpeeds;
        private Material flameMaterial;
        private float radius, duration, elapsed;
        // 2026-11-23: 기본 1은 기존 폭발탄 형태를 유지하고 유탄만 위쪽 기둥 형태로 늘립니다.
        private float verticalStretch = 1f;
        // 2026-11-23: 폭발 적만 균일한 구형 분포를 사용하며 폭발탄/유탄의 기존 분포는 유지합니다.
        private bool spherical;

        // 2026-11-23: 실제 폭발 반경에 외형 배율만 곱하며 파티클 배열은 초기화 때 한 번 생성합니다.
        public void Initialize(float blastRadius, VfxConfig.ExplosiveAmmoSettings config, float verticalStretch = 1f, bool spherical = false)
        {
            this.spherical = spherical;
            // 2026-11-23: 유탄이 공유 화염 렌더러의 위쪽 분포만 조절하도록 전달받습니다.
            this.verticalStretch = Mathf.Max(1f, verticalStretch);
            settings = config;
            radius = Mathf.Max(0.001f, blastRadius * Mathf.Max(0.01f, config.radiusScale));
            duration = Mathf.Max(0.05f, config.duration);
            Shader shader = Resources.Load<Shader>("VFX/FlameBurst");
            if (shader == null || !shader.isSupported)
            {
                // 2026-11-23: 렌더링할 수 없을 때는 시각 오브젝트만 종료하며 이미 적용된 피해에는 관여하지 않습니다.
                Debug.LogWarning("[VFX] FlameBurst 셰이더를 사용할 수 없습니다.", this);
                Destroy(gameObject);
                return;
            }
            flameMaterial = new Material(shader);
            flameMaterial.SetColor("_HotColor", config.hotColor);
            flameMaterial.SetColor("_FlameColor", config.flameColor);
            flameMaterial.SetColor("_EdgeColor", config.edgeColor);
            flameMaterial.SetFloat("_NoiseScale", config.noiseScale);
            flameMaterial.SetFloat("_Distortion", config.distortion);

            // 2026-11-23: 수동 파티클 갱신으로 게임 일시정지 시 모양/팽창/소멸을 함께 정지시킵니다.
            var cloud = new GameObject("Layered flame billboards");
            cloud.transform.SetParent(transform, false);
            flames = cloud.AddComponent<ParticleSystem>();
            flames.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = flames.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.simulationSpeed = 0f;
            main.maxParticles = Mathf.Clamp(config.flameCount, 1, 64) + 1;
            var emission = flames.emission;
            emission.enabled = false;
            var shape = flames.shape;
            shape.enabled = false;
            var renderer = cloud.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = flameMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.maxParticleSize = 1f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // 2026-11-23: 자동 방출은 끈 상태로 렌더링만 시작하고 위치/수명은 아래 수동 갱신을 사용합니다.
            flames.Play(false);
            particles = new ParticleSystem.Particle[main.maxParticles];
            directions = new Vector3[particles.Length];
            sizes = new float[particles.Length];
            delays = new float[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                directions[i] = i == 0 ? Vector3.zero : Random.onUnitSphere;
                sizes[i] = i == 0 ? 1.3f : Random.Range(0.45f, 0.85f);
                delays[i] = i == 0 ? 0f : Random.Range(0f, 0.12f);
                // 2026-11-23: 황금각 분포로 모든 방향을 고르게 덮고 크기/시작을 맞춰 구형 윤곽을 유지합니다.
                if (spherical)
                {
                    float y = 1f - 2f * (i - 0.5f) / (particles.Length - 1);
                    float ring = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                    float angle = i * 2.399963f;
                    directions[i] = i == 0 ? Vector3.zero : new Vector3(Mathf.Cos(angle) * ring, y, Mathf.Sin(angle) * ring);
                    sizes[i] = i == 0 ? 1.7f : 0.75f;
                    delays[i] = 0f;
                }
            }

            // 2026-11-23: 기존 레이저 머티리얼을 공유해 짧은 불씨 선을 바깥으로 퍼뜨립니다.
            Material sparkMaterial = Resources.Load<Material>("VFX/BulletTrail");
            int sparkCount = sparkMaterial == null ? 0 : Mathf.Clamp(config.sparkCount, 0, 128);
            sparks = new LineRenderer[sparkCount];
            sparkDirections = new Vector3[sparkCount];
            sparkSpeeds = new float[sparkCount];
            for (int i = 0; i < sparkCount; i++)
            {
                var child = new GameObject("Expanding ember " + i);
                child.transform.SetParent(transform, false);
                LineRenderer line = child.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.sharedMaterial = sparkMaterial;
                line.widthMultiplier = Mathf.Max(0f, config.sparkWidth);
                line.widthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                line.numCapVertices = 2;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                sparks[i] = line;
                sparkDirections[i] = Random.onUnitSphere;
                sparkSpeeds[i] = Random.Range(0.65f, 1.25f);
            }
            Draw();
        }

        // 2026-11-23: 기존 특수 공격과 같은 게임 시간을 사용하고 연출 종료 시 자신을 제거합니다.
        private void Update()
        {
            if (flames == null || SpecialAttackUtil.IsPaused) return;
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Destroy(gameObject); return; }
            Draw();
        }

        // 2026-11-23: 초반에 빠르게 팽창하고 후반에는 위로 흐르며 붉게 식어 사라집니다.
        private void Draw()
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            flameMaterial.SetFloat("_Age", elapsed * settings.flameSpeed);
            flameMaterial.SetFloat("_Cooling", progress);
            for (int i = 0; i < particles.Length; i++)
            {
                float p = Mathf.Clamp01((progress - delays[i]) / (1f - delays[i]));
                float expansion = 1f - Mathf.Pow(1f - Mathf.Clamp01(p / 0.45f), 3f);
                float opacity = Mathf.SmoothStep(0f, 1f, p / 0.07f) * (1f - Mathf.SmoothStep(0.25f, 1f, p));
                Vector3 position = directions[i] * radius * 0.6f * expansion
                    + Vector3.up * (radius * settings.rise * p * p);
                // 2026-11-23: 폭발 적의 화염구는 상승/기둥 변형 없이 자폭 중심에서 모든 방향으로 팽창합니다.
                if (spherical) position = directions[i] * radius * 0.6f * expansion;
                // 2026-11-23: 유탄은 불꽃을 지면 위로 모아 세로로 솟게 하며 기존 폭발탄은 변경하지 않습니다.
                if (verticalStretch > 1f)
                    position.y = Mathf.Abs(directions[i].y) * radius * 0.6f * expansion * verticalStretch
                        + radius * settings.rise * p * p;
                particles[i] = new ParticleSystem.Particle
                {
                    position = position,
                    startSize = radius * sizes[i] * Mathf.Lerp(0.1f, 1f, expansion),
                    startColor = new Color(1f, 1f, 1f, opacity),
                    rotation = i * 137.5f,
                    startLifetime = 1f,
                    remainingLifetime = 1f,
                    randomSeed = (uint)(i + 1)
                };
            }
            flames.SetParticles(particles, particles.Length);
            // 2026-11-23: 불씨는 초반에 빠르게 퍼진 뒤 아래로 떨어지며 길이와 불투명도를 함께 줄입니다.
            for (int i = 0; i < sparks.Length; i++)
            {
                float distance = radius * sparkSpeeds[i] * (1f - Mathf.Pow(1f - progress, 2f));
                Vector3 head = sparkDirections[i] * distance + Vector3.down * radius * 0.2f * progress * progress;
                Vector3 tail = head - sparkDirections[i] * radius * settings.sparkLength * (1f - progress);
                sparks[i].SetPosition(0, tail);
                sparks[i].SetPosition(1, head);
                Color color = Color.Lerp(settings.hotColor, settings.flameColor, progress);
                color.a *= 1f - progress;
                sparks[i].endColor = color;
                color.a = 0f;
                sparks[i].startColor = color;
            }
        }

        // 2026-11-23: 폭발 전용 인스턴스 머티리얼만 해제하고 공유 불씨 머티리얼은 유지합니다.
        private void OnDestroy()
        {
            if (flameMaterial != null) Destroy(flameMaterial);
        }
    }
}
