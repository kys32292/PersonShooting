using UnityEngine;
using UnityEngine.Rendering;

namespace PesonShooting.VFX
{
    // 2026-11-23: GrenadeAmmo 참조의 화염 기둥, 위로 올라가는 검은 연기, 바닥 먼지를 조합합니다.
    // 실제 범위 피해와 물리 판정은 기존 유탄 코드가 담당하며 이 컴포넌트는 외형만 생성합니다.
    public sealed class GrenadeImpactVfx : MonoBehaviour
    {
        private VfxConfig.GrenadeAmmoSettings settings;
        private ParticleSystem cloud;
        private ParticleSystem.Particle[] particles;
        private Vector3[] directions;
        private float[] sizes, delays, heights;
        private Material smokeMaterial;
        // 2026-11-23: 한 파티클 배열의 앞부분은 상승 연기, 뒷부분은 지면 먼지로 구분합니다.
        private int smokeCount;
        private float radius, duration, elapsed;

        // 2026-11-23: 화염은 공통 구현을 세로로 늘려 재사용하고 유탄 전용 연기/먼지는 독립적으로 구성합니다.
        public void Initialize(float blastRadius, VfxConfig.GrenadeAmmoSettings config)
        {
            settings = config;
            radius = Mathf.Max(0.001f, blastRadius * Mathf.Max(0.01f, config.radiusScale));
            duration = Mathf.Max(0.05f, Mathf.Max(config.duration, config.fireDuration));
            var fireObject = new GameObject("Rising grenade flame column");
            fireObject.transform.SetParent(transform, false);
            fireObject.AddComponent<ExplosiveImpactVfx>().Initialize(blastRadius,
                new VfxConfig.ExplosiveAmmoSettings
                {
                    duration = config.fireDuration,
                    radiusScale = config.radiusScale,
                    flameCount = config.flameCount,
                    hotColor = config.hotColor,
                    flameColor = config.flameColor,
                    edgeColor = config.edgeColor,
                    noiseScale = config.noiseScale,
                    distortion = config.distortion,
                    flameSpeed = config.fireSpeed,
                    rise = 0.35f,
                    sparkCount = config.sparkCount,
                    sparkWidth = config.sparkWidth,
                    sparkLength = config.sparkLength
                }, config.columnHeight);

            // 2026-11-23: 연기와 먼지 모두 절차적 노이즈 셰이더를 사용하여 로컬 참조 이미지에 의존하지 않습니다.
            Shader shader = Resources.Load<Shader>("VFX/BlastSmoke");
            if (shader == null || !shader.isSupported)
            {
                // 2026-11-23: 연기 셰이더가 없어도 앞서 생성한 화염과 전체 수명 갱신은 유지합니다.
                Debug.LogWarning("[VFX] BlastSmoke 셰이더를 사용할 수 없어 유탄 연기/먼지를 생략합니다.", this);
                return;
            }
            smokeCount = Mathf.Clamp(config.smokeCount, 0, 128);
            int count = smokeCount + Mathf.Clamp(config.dustCount, 0, 128);
            // 2026-11-23: 연기/먼지를 모두 0으로 설정하면 해당 렌더러 생성을 생략하고 화염만 재생합니다.
            if (count == 0) return;
            smokeMaterial = new Material(shader);
            smokeMaterial.SetFloat("_NoiseScale", Mathf.Max(1f, config.noiseScale));
            smokeMaterial.SetFloat("_Distortion", Mathf.Clamp01(config.distortion));
            var cloudObject = new GameObject("Grenade smoke and ground dust");
            cloudObject.transform.SetParent(transform, false);
            cloud = cloudObject.AddComponent<ParticleSystem>();
            cloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = cloud.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.simulationSpeed = 0f;
            main.maxParticles = count;
            var emission = cloud.emission;
            emission.enabled = false;
            var shape = cloud.shape;
            shape.enabled = false;
            var renderer = cloudObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = smokeMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.maxParticleSize = 1f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // 2026-11-23: 난수는 생성 시 한 번만 저장하여 일시정지나 매 프레임 갱신 중 모양이 튀지 않게 합니다.
            particles = new ParticleSystem.Particle[count];
            directions = new Vector3[count];
            sizes = new float[count];
            delays = new float[count];
            heights = new float[count];
            for (int i = 0; i < count; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                directions[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                sizes[i] = Random.Range(0.75f, 1.35f);
                delays[i] = i < smokeCount ? Random.Range(0.04f, 0.16f) : Random.Range(0f, 0.06f);
                heights[i] = Random.Range(0.25f, 0.8f);
            }
            cloud.Play(false);
            DrawCloud();
        }

        // 2026-11-23: 부모 수명과 무관하게 끝까지 재생하며 게임 일시정지 시 화염과 연기를 함께 멈춥니다.
        private void Update()
        {
            if (settings == null || SpecialAttackUtil.IsPaused) return;
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Destroy(gameObject); return; }
            if (cloud != null) DrawCloud();
        }

        // 2026-11-23: 검은 연기는 기둥 위로 부풀어 오르고 갈색 먼지는 바닥을 따라 방사형으로 퍼집니다.
        private void DrawCloud()
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            smokeMaterial.SetFloat("_Age", elapsed * 0.45f);
            for (int i = 0; i < particles.Length; i++)
            {
                bool smoke = i < smokeCount;
                float p = Mathf.Clamp01((progress - delays[i]) / (1f - delays[i]));
                float expansion = 1f - Mathf.Pow(1f - p, 3f);
                Vector3 position;
                Color color = smoke ? settings.smokeColor : settings.dustColor;
                float size = radius * Mathf.Max(0f, settings.smokeSize) * sizes[i];
                if (smoke)
                {
                    // 2026-11-23: 위쪽 검은 연기의 초기 높이는 화염 기둥 비율을 따라가며 시간에 따라 추가 상승합니다.
                    position = directions[i] * radius * 0.35f * expansion
                        + Vector3.up * radius * (heights[i] * Mathf.Max(0f, settings.columnHeight) * 0.45f
                            + Mathf.Max(0f, settings.smokeRise) * p);
                    size *= Mathf.Lerp(0.3f, 1.6f, expansion);
                    Color cooled = Color.Lerp(color, new Color(0.3f, 0.3f, 0.3f, color.a), p * 0.45f);
                    color = cooled;
                }
                else
                {
                    // 2026-11-23: 먼지는 낮은 높이를 유지하며 dustSpread 배율만큼 바깥으로 퍼집니다.
                    position = directions[i] * radius * Mathf.Max(0f, settings.dustSpread) * expansion
                        + Vector3.up * radius * (0.04f + heights[i] * 0.12f * expansion);
                    size *= Mathf.Lerp(0.2f, 1.3f, expansion);
                }
                float fadeIn = Mathf.SmoothStep(0f, 1f, p / (smoke ? 0.18f : 0.08f));
                float fadeOut = 1f - Mathf.SmoothStep(smoke ? 0.45f : 0.15f, 1f, p);
                color.a *= fadeIn * fadeOut;
                particles[i] = new ParticleSystem.Particle
                {
                    position = position,
                    startSize = size,
                    startColor = color,
                    rotation = i * 137.5f + p * (smoke ? 12f : -8f),
                    startLifetime = 1f,
                    remainingLifetime = 1f,
                    randomSeed = (uint)(i + 1)
                };
            }
            cloud.SetParticles(particles, particles.Length);
        }

        // 2026-11-23: 인스턴스 연기 머티리얼만 해제하며 공통 화염은 자식 컴포넌트가 따로 정리합니다.
        private void OnDestroy()
        {
            if (smokeMaterial != null) Destroy(smokeMaterial);
        }
    }
}
