using UnityEngine;

namespace PesonShooting.VFX
{
    // 2026-11-23: 중력장 VFX: 흡입 입자, 바닥 원, 반투명 반구와 중심 빛을 생성합니다.
    public sealed class GravityWellVfx : MonoBehaviour
    {
        // 2026-11-23: 선마다 시작 위치와 속도를 저장해 반복 흡입 중에도 입자의 개별 움직임을 유지합니다.
        private struct Strand
        {
            public LineRenderer line;
            public float phase, angle, height, speed, startRadius;
            public bool airborne;
        }

        private Strand[] strands;
        private LineRenderer boundary;
        private readonly LineRenderer[] domeArcs = new LineRenderer[2];
        private Mesh domeMesh;
        private Material domeMaterial;
        private ParticleSystem core;
        private readonly ParticleSystem.Particle[] coreParticle = new ParticleSystem.Particle[1];
        private Material lineMaterial, glowMaterial;
        // 2026-11-23: 전투 범위/수명과 시각 설정을 분리해 피해 판정은 기존 GravityField에 맡깁니다.
        private float elapsed, radius, duration;
        private VfxConfig.GravityWellSettings settings;

        // 2026-11-23: 실제 전투 수명이 아니라 이 시각 효과의 재생 완료 여부만 제공합니다.
        public bool Active => elapsed < duration;

        // 2026-11-23: 선 두께/개수/메시 등 생성 값은 다음 착탄 또는 테스트 재생부터 적용됩니다.
        public void Initialize(float range, float lifetime, VfxConfig.GravityWellSettings visualSettings)
        {
            radius = Mathf.Max(0.001f, range);
            duration = Mathf.Max(0.001f, lifetime);
            settings = visualSettings;
            // 2026-11-23: 지정 머티리얼은 복제하므로 원본 에셋의 색/텍스처를 변경하지 않습니다.
            lineMaterial = settings.material != null ? new Material(settings.material) : new Material(Shader.Find("Sprites/Default"));
            glowMaterial = new Material(lineMaterial);
            if (settings.flare != null) glowMaterial.mainTexture = settings.flare;
            boundary = MakeLine("Effective range", 97, Mathf.Max(0, settings.ringWidth));
            for (int i = 0; i < boundary.positionCount; i++)
            {
                float angle = i / 96f * Mathf.PI * 2;
                boundary.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Max(0, settings.groundOffset), Mathf.Sin(angle) * radius));
            }

            // 2026-11-23: 반구의 반지름은 실제 공격 범위와 일치시킵니다.
            CreateDome();
            for (int axis = 0; axis < domeArcs.Length; axis++)
            {
                LineRenderer arc = MakeLine("Upper range arc " + (axis + 1), 49, Mathf.Max(0, settings.arcWidth));
                domeArcs[axis] = arc;
                for (int i = 0; i < arc.positionCount; i++)
                {
                    float angle = i / 48f * Mathf.PI;
                    float horizontal = Mathf.Cos(angle) * radius;
                    arc.SetPosition(i, new Vector3(axis == 0 ? horizontal : 0,
                        Mathf.Sin(angle) * radius + Mathf.Max(0, settings.groundOffset), axis == 1 ? horizontal : 0));
                }
            }

            // 2026-11-23: 입자는 별도 물리 판정 없이 안쪽으로 이동하는 선이며 개수는 안전 범위로 제한합니다.
            int groundCount = Mathf.Clamp(settings.particleCount, 0, 256);
            int airborneCount = Mathf.Clamp(settings.hemisphereParticleCount, 0, 256);
            strands = new Strand[groundCount + airborneCount];
            for (int i = 0; i < strands.Length; i++)
            {
                bool airborne = i >= groundCount;
                strands[i] = new Strand
                {
                    line = MakeLine(airborne ? "Hemisphere inward streak" : "Inward streak", 2, RandomPositive(settings.particleWidth)),
                    phase = Random.value,
                    angle = Random.value * Mathf.PI * 2,
                    height = Mathf.Min(radius, RandomPositive(settings.particleHeight)),
                    speed = RandomPositive(settings.particleSpeed),
                    startRadius = radius,
                    airborne = airborne
                };
                if (airborne)
                {
                    // 위쪽 반구의 높이를 나누어 배치하고 수평 반경도 줄여 실제 구 범위 안에서 시작합니다.
                    float elevation = Mathf.Lerp(0.2f, 0.95f, (i - groundCount + 0.5f) / airborneCount);
                    float spawnRadius = radius * Random.Range(0.65f, 0.95f);
                    strands[i].height = elevation * spawnRadius;
                    strands[i].startRadius = Mathf.Sqrt(1f - elevation * elevation) * spawnRadius;
                }
            }

            // 2026-11-23: 빛 한 개만 직접 갱신하므로 중심에 불투명 구체를 만들지 않습니다.
            var coreObject = new GameObject("Small energy focus");
            coreObject.transform.SetParent(transform, false);
            core = coreObject.AddComponent<ParticleSystem>();
            core.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = core.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.simulationSpeed = 0;
            main.maxParticles = 1;
            var emission = core.emission;
            emission.enabled = false;
            var shape = core.shape;
            shape.enabled = false;
            core.GetComponent<ParticleSystemRenderer>().sharedMaterial = glowMaterial;
            core.Play();
        }

        // 2026-11-23: 위쪽 반구만 메시로 생성하고, 원본 머티리얼과 분리해 투명도를 조절합니다.
        private void CreateDome()
        {
            const int segments = 48;
            const int rings = 12;
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var triangles = new int[rings * segments * 6];
            for (int ring = 0; ring <= rings; ring++)
            {
                float elevation = ring / (float)rings * Mathf.PI * 0.5f;
                float horizontal = Mathf.Cos(elevation) * radius;
                float height = Mathf.Sin(elevation) * radius + Mathf.Max(0, settings.groundOffset);
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = segment / (float)segments * Mathf.PI * 2;
                    vertices[ring * (segments + 1) + segment] =
                        new Vector3(Mathf.Cos(angle) * horizontal, height, Mathf.Sin(angle) * horizontal);
                    if (ring == rings || segment == segments) continue;
                    int lower = ring * (segments + 1) + segment;
                    int upper = lower + segments + 1;
                    int index = (ring * segments + segment) * 6;
                    triangles[index] = lower;
                    triangles[index + 1] = upper;
                    triangles[index + 2] = lower + 1;
                    triangles[index + 3] = lower + 1;
                    triangles[index + 4] = upper;
                    triangles[index + 5] = upper + 1;
                }
            }

            var dome = new GameObject("Translucent upper hemisphere");
            dome.transform.SetParent(transform, false);
            domeMesh = new Mesh { name = "Gravity range hemisphere", vertices = vertices, triangles = triangles };
            domeMesh.RecalculateNormals();
            domeMesh.RecalculateBounds();
            dome.AddComponent<MeshFilter>().sharedMesh = domeMesh;
            domeMaterial = new Material(lineMaterial);
            var renderer = dome.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = domeMaterial;
            renderer.enabled = settings.showDome;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // 2026-11-23: 바닥 원/반원/입자의 선 생성과 그림자 설정을 공통 처리합니다.
        private LineRenderer MakeLine(string label, int count, float width)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(transform, false);
            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = lineMaterial;
            line.positionCount = count;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        // 2026-11-23: 하나의 경과 시간으로 반구/윤곽/입자/중심 빛의 페이드를 맞춥니다.
        private void Update()
        {
            // 2026-11-23: 게임 일시정지 중에는 VFX 진행도 함께 멈춥니다.
            if (SpecialAttackUtil.IsPaused) return;
            elapsed += Time.deltaTime;
            float fade = (settings.fadeIn <= 0 ? 1 : Mathf.Clamp01(elapsed / settings.fadeIn)) * (settings.fadeOut <= 0 ? 1 : Mathf.Clamp01((duration - elapsed) / settings.fadeOut));
            Color rangeColor = settings.color;
            rangeColor.a = fade * settings.ringOpacity * settings.brightness * settings.color.a;
            boundary.startColor = boundary.endColor = rangeColor;
            Color domeColor = settings.color;
            domeColor.a = fade * settings.domeOpacity * settings.brightness * settings.color.a;
            domeMaterial.color = domeColor;
            Color arcColor = settings.color;
            arcColor.a = fade * settings.arcOpacity * settings.brightness * settings.color.a;
            foreach (LineRenderer arc in domeArcs) arc.startColor = arc.endColor = arcColor;
            foreach (Strand strand in strands)
            {
                // 2026-11-23: 입자의 굵기/길이/속도/생성 높이/휘어짐을 Config 값으로 계산합니다.
                float progress = Mathf.Repeat(elapsed * strand.speed + strand.phase, 1f);
                float endRadius = strand.airborne ? 0f : Mathf.Clamp(settings.particleEndRadius, 0, radius);
                float r = Mathf.Lerp(strand.startRadius, endRadius, progress);
                float angle = strand.angle + progress * settings.particleBend;
                float targetHeight = Mathf.Min(radius, Mathf.Max(0, strand.airborne ? settings.groundOffset : settings.coreHeight));
                Vector3 focus = Vector3.up * targetHeight;
                Vector3 head = new Vector3(Mathf.Cos(angle) * r, Mathf.Lerp(strand.height, targetHeight, progress), Mathf.Sin(angle) * r);
                float length = Mathf.Max(0, Mathf.Lerp(settings.particleLength.x, settings.particleLength.y, progress));
                if (strand.airborne) length *= Mathf.Max(0f, settings.hemisphereLengthScale);
                Vector3 tail = head + (head - focus).normalized * length;
                if (strand.airborne)
                    tail = Vector3.ClampMagnitude(tail, radius);
                strand.line.SetPosition(0, tail);
                strand.line.SetPosition(1, head);
                Color headColor = settings.color;
                if (strand.airborne)
                {
                    headColor.r *= 1.35f;
                    headColor.g *= 1.35f;
                    headColor.b *= 1.35f;
                }
                headColor.a = Mathf.Sin(progress * Mathf.PI) * fade * settings.particleOpacity * settings.brightness * settings.color.a;
                Color tailColor = headColor;
                tailColor.a = 0;
                strand.line.startColor = tailColor;
                strand.line.endColor = headColor;
            }

            // 2026-11-23: 중심 빛의 크기/높이/불투명도/맥동도 Config에서 조절합니다.
            Color coreColor = settings.color;
            coreColor.a = fade * Mathf.Clamp01(settings.coreOpacity + settings.corePulseOpacity * Mathf.Sin(elapsed * settings.corePulseSpeed)) * settings.brightness * settings.color.a;
            coreParticle[0] = new ParticleSystem.Particle
            {
                position = Vector3.up * Mathf.Min(radius, Mathf.Max(0, settings.coreHeight)),
                startSize = settings.coreSize <= 0 ? 0 : Mathf.Max(0, settings.coreSize + settings.corePulseSize * Mathf.Sin(elapsed * settings.corePulseSpeed)),
                startColor = coreColor,
                startLifetime = 1,
                remainingLifetime = 1
            };
            core.SetParticles(coreParticle, 1);
            if (elapsed >= duration) Destroy(gameObject);
        }

        // 2026-11-23: 역순/음수 범위 입력도 안전하게 정렬해 입자별 무작위 수치를 뽑습니다.
        private static float RandomPositive(Vector2 range)
        {
            return Random.Range(Mathf.Max(0, Mathf.Min(range.x, range.y)), Mathf.Max(0, Mathf.Max(range.x, range.y)));
        }

        // 2026-11-23: 반복 발동/씬 종료 시 런타임 생성 머티리얼과 메시를 해제합니다.
        private void OnDestroy()
        {
            if (lineMaterial != null) Destroy(lineMaterial);
            if (glowMaterial != null) Destroy(glowMaterial);
            if (domeMaterial != null) Destroy(domeMaterial);
            if (domeMesh != null) Destroy(domeMesh);
        }
    }
}
