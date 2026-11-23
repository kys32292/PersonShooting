using UnityEngine;
using UnityEngine.Rendering;

namespace PesonShooting.VFX
{
    // 2026-11-23: 참조 이미지의 금빛 광선, 별 모양 탄두와 작은 십자 반짝임을 구성합니다.
    // 콜라이더나 폭발은 만들지 않고 부모 투사체의 이동/관통/소멸을 그대로 따릅니다.
    public sealed class PiercingProjectileVfx : MonoBehaviour
    {
        private VfxConfig.PiercingAmmoSettings settings;
        // 2026-11-23: 공유 선 머티리얼과 직접 만든 대체/중심 빛 머티리얼을 구분해 소유 자원만 해제합니다.
        private Material lineMaterial, ownedLineMaterial, glowMaterial;
        private Camera view;
        private Vector3 launchPosition;
        private readonly LineRenderer[] beams = new LineRenderer[2];
        private LineRenderer[] tip;
        private LineRenderer[][] glints;
        private ParticleSystem glow;
        private readonly ParticleSystem.Particle[] glowParticle = new ParticleSystem.Particle[1];
        private float elapsed;

        // 2026-11-23: 길이/굵기/개수는 발사 시 결정하고 기존 공유 머티리얼과 원본 텍스처를 수정하지 않습니다.
        public void Initialize(VfxConfig.PiercingAmmoSettings config)
        {
            settings = config;
            launchPosition = transform.position;
            lineMaterial = Resources.Load<Material>("VFX/BulletTrail");
            if (lineMaterial == null)
            {
                // 2026-11-23: 기존 궤적 머티리얼이 없더라도 기본 스프라이트 셰이더로 광선을 표시합니다.
                ownedLineMaterial = new Material(Shader.Find("Sprites/Default"));
                lineMaterial = ownedLineMaterial;
            }
            view = Camera.main;
            if (view == null) view = FindFirstObjectByType<Camera>();

            // 2026-11-23: 탄두가 로컬 원점이며 진한 레이저가 뒤쪽(-Z)까지 같은 굵기/색으로 이어집니다.
            beams[0] = CreateBeam("Soft golden beam halo", config.haloWidth, config.haloColor);
            beams[1] = CreateBeam("Solid bright golden laser", config.width, config.coreColor);
            tip = CreateStar("Four pointed projectile tip", config.tipSize * 0.16f);
            glints = new LineRenderer[Mathf.Clamp(config.glintCount, 0, 32)][];
            // 2026-11-23: 작은 별은 위상을 나눠 광선을 따라 흐르며 양 끝에서는 투명해집니다.
            for (int i = 0; i < glints.Length; i++)
                glints[i] = CreateStar("Traveling beam glint " + i, config.glintSize * 0.18f);

            // 2026-11-23: 선택한 Flare가 있을 때만 작은 중심 빛을 추가합니다. 별 윤곽은 텍스처 없이도 보입니다.
            if (config.flare != null)
            {
                glowMaterial = new Material(Shader.Find("Sprites/Default"));
                glowMaterial.mainTexture = config.flare;
                var child = new GameObject("Soft star tip focus");
                child.transform.SetParent(transform, false);
                glow = child.AddComponent<ParticleSystem>();
                glow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = glow.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.simulationSpeed = 0f;
                main.maxParticles = 1;
                var emission = glow.emission;
                emission.enabled = false;
                var shape = glow.shape;
                shape.enabled = false;
                var renderer = child.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = glowMaterial;
                renderer.maxParticleSize = 1f;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                glow.Play(false);
            }
            DrawStars();
        }

        // 2026-11-23: 비행 내내 양 끝의 굵기와 알파를 같게 유지해 꼬리가 흐려지지 않는 레이저로 표시합니다.
        private LineRenderer CreateBeam(string label, float width, Color color)
        {
            LineRenderer line = CreateLine(label, 2, width);
            // 2026-11-23: 기존 꼬리 축소/투명화는 아래 주석으로 보관하고 균일한 광선으로 대체합니다.
            // line.widthCurve = AnimationCurve.Linear(0f, 0.02f, 1f, 1f);
            // line.endColor = color;
            // color.a = 0f;
            // line.startColor = color;
            line.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            line.SetPosition(0, transform.InverseTransformPoint(launchPosition));
            line.SetPosition(1, Vector3.zero);
            line.startColor = line.endColor = color;
            return line;
        }

        // 2026-11-23: 화면을 향한 두 개의 뾰족한 선을 교차해 네 갈래 별 모양을 만듭니다.
        private LineRenderer[] CreateStar(string label, float width)
        {
            var star = new LineRenderer[2];
            for (int i = 0; i < star.Length; i++)
            {
                star[i] = CreateLine(label + " axis " + i, 3, width);
                star[i].widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
            }
            return star;
        }

        // 2026-11-23: 모든 선은 로컬 좌표이며 물리 컴포넌트 없이 부모 탄과 함께 움직입니다.
        private LineRenderer CreateLine(string label, int count, float width)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = false;
            line.positionCount = count;
            line.widthMultiplier = Mathf.Max(0f, width);
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        // 2026-11-23: 부모 투사체가 이동한 뒤 별과 반짝임을 갱신하며 일시정지 중에는 진행을 멈춥니다.
        private void LateUpdate()
        {
            if (settings == null || SpecialAttackUtil.IsPaused) return;
            elapsed += Time.deltaTime;
            // 발사 당시 총구 위치를 고정해 탄두가 멀어져도 꼬리가 잘리지 않게 합니다.
            Vector3 tail = transform.InverseTransformPoint(launchPosition);
            foreach (LineRenderer beam in beams)
                beam.SetPosition(0, tail);
            DrawStars();
        }

        // 2026-11-23: 카메라 축을 탄의 로컬 좌표로 바꿔 어느 방향에서 보더라도 십자 모양을 유지합니다.
        private void DrawStars()
        {
            Vector3 right = view != null ? transform.InverseTransformDirection(view.transform.right) : Vector3.right;
            Vector3 up = view != null ? transform.InverseTransformDirection(view.transform.up) : Vector3.up;
            float pulse = Mathf.Max(0f, 1f + Mathf.Sin(elapsed * settings.pulseSpeed) * settings.tipPulse);
            SetStar(tip, Vector3.zero, right, up, Mathf.Max(0f, settings.tipSize) * pulse, settings.tipColor);
            for (int i = 0; i < glints.Length; i++)
            {
                float t = Mathf.Repeat(i / (float)glints.Length + elapsed * settings.glintSpeed, 1f);
                float visibility = Mathf.Sin(t * Mathf.PI);
                Color color = settings.tipColor;
                color.a *= visibility;
                SetStar(glints[i], transform.InverseTransformPoint(launchPosition) * t, right, up,
                    Mathf.Max(0f, settings.glintSize) * visibility, color);
            }
            if (glow != null)
            {
                glowParticle[0] = new ParticleSystem.Particle
                {
                    position = Vector3.zero,
                    startSize = Mathf.Max(0f, settings.tipSize) * pulse * 0.8f,
                    startColor = settings.tipColor,
                    startLifetime = 1f,
                    remainingLifetime = 1f
                };
                glow.SetParticles(glowParticle, 1);
            }
        }

        // 2026-11-23: 한 별의 가로/세로 길이만 달리해 참조처럼 세로가 긴 날카로운 탄두를 표현합니다.
        private static void SetStar(LineRenderer[] lines, Vector3 center, Vector3 right, Vector3 up, float size, Color color)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                Vector3 axis = (i == 0 ? right * 0.6f : up) * size * 0.5f;
                lines[i].SetPosition(0, center - axis);
                lines[i].SetPosition(1, center);
                lines[i].SetPosition(2, center + axis);
                lines[i].startColor = lines[i].endColor = color;
            }
        }

        // 2026-11-23: 발사별로 생성한 머티리얼만 해제하고 공유 궤적 머티리얼은 유지합니다.
        private void OnDestroy()
        {
            if (ownedLineMaterial != null) Destroy(ownedLineMaterial);
            if (glowMaterial != null) Destroy(glowMaterial);
        }
    }
}
