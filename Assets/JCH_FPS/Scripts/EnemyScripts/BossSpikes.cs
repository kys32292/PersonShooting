// 11/19 - 보스 장판 분출 가시 (승인된 미리보기와 같은 생성 로직: 면 수 9, 휘어짐 0.20)
// 메시: 바닥에서 꼭대기로 가늘어지는 각진 원뿔. 위로 갈수록 한쪽으로 휘고(bend), 살짝 비틀리며(twist), 단면에 울퉁불퉁함(noise)
//       모양이 다른 3종을 미리 만들어 돌려 씀 (모든 장판이 공유, 한 번만 생성)
// 배치: 가운데 1개(가장 큼) + 안쪽 고리 5개 + 바깥 고리 (반지름 x 2.6)개, 바깥으로 갈수록 낮고 바깥쪽으로 기울어짐. 장판마다 무작위
// 재질: 몸체는 보스와 같은 셀 셰이더(각진 면 그대로), 외곽선은 부드러운 노멀 메시 + Resources/BossSpikeOutline 셰이더
using System.Collections.Generic;
using UnityEngine;

public static class BossSpikes
{
    // 미리보기에서 정한 값
    private const int Sides = 9;
    private const float Bend = 0.20f;
    private const int Segments = 4;
    private const float BaseRadius = 0.16f;  // 높이 1 기준 밑면 반지름 (실제는 가로로 WidthScale배)
    private const float Noise = 0.25f;
    private const float WidthScale = 1.9f;
    private const float OutlineWidth = 0.025f;

    private static readonly Color BodyColor = new Color(0.478f, 0.086f, 0.133f, 1f);
    private static readonly Color BodyEmission = new Color(0.16f, 0.01f, 0.02f, 1f);

    private static Mesh[] bodyMeshes;
    private static Mesh[] hullMeshes;
    private static Material bodyMaterial;
    private static Material outlineMaterial;
    private static bool materialsLoaded;

    public struct Spike
    {
        public Transform transform; // 높이/위치를 조절할 대상 (기울어진 holder의 자식)
        public float height;
        public Renderer[] renderers;
    }

    // radius 크기 장판에 가시 묶음 생성 (처음에는 바닥 아래에 숨긴 상태)
    public static List<Spike> Build(Transform root, float radius)
    {
        EnsureAssets();

        var spikes = new List<Spike>();
        float maxHeight = radius * 0.75f;

        // (중심 거리 비율, 각도, 높이 비율)
        var points = new List<Vector3> { new Vector3(0f, 0f, 1f) };
        int innerCount = 5;
        int outerCount = Mathf.Max(3, Mathf.RoundToInt(radius * 2.6f));
        for (int i = 0; i < innerCount; i++)
        {
            points.Add(new Vector3(0.42f, (i + Random.value * 0.4f) / innerCount * Mathf.PI * 2f, 0.85f));
        }
        for (int i = 0; i < outerCount; i++)
        {
            points.Add(new Vector3(0.78f, (i + Random.value * 0.4f) / outerCount * Mathf.PI * 2f, 0.65f));
        }

        for (int index = 0; index < points.Count; index++)
        {
            Vector3 point = points[index];
            float distance = point.x * radius + (index > 0 ? (Random.value - 0.5f) * 0.16f * radius : 0f);
            float angle = point.y;
            float height = maxHeight * point.z * (0.85f + Random.value * 0.3f);

            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            // 바깥쪽으로 기울어짐 (가운데 0도 ~ 가장자리 24도)
            var holder = new GameObject("Spike").transform;
            holder.SetParent(root, false);
            holder.localPosition = outward * distance;
            if (distance > 0.01f)
            {
                float tilt = 6f + 18f * (distance / radius);
                Vector3 tiltedUp = Vector3.Slerp(Vector3.up, outward, tilt / 90f);
                holder.localRotation = Quaternion.FromToRotation(Vector3.up, tiltedUp);
            }

            var spike = new GameObject("Mesh").transform;
            spike.SetParent(holder, false);
            spike.localRotation = Quaternion.Euler(0f, Random.value * 360f, 0f); // 휘는 방향 무작위
            spike.localScale = new Vector3(height * WidthScale, height, height * WidthScale);
            spike.localPosition = new Vector3(0f, -height, 0f);

            int variant = index % bodyMeshes.Length;
            var renderers = new List<Renderer>
            {
                CreatePart("Body", spike, bodyMeshes[variant], bodyMaterial),
            };
            if (outlineMaterial != null)
            {
                renderers.Add(CreatePart("Outline", spike, hullMeshes[variant], outlineMaterial));
            }

            foreach (Renderer part in renderers) part.enabled = false;

            spikes.Add(new Spike { transform = spike, height = height, renderers = renderers.ToArray() });
        }

        return spikes;
    }

    // rise: 0 = 바닥 아래(숨김), 1 = 완전히 솟음 (1보다 크면 살짝 튀어 오름)
    public static void SetRise(List<Spike> spikes, float rise)
    {
        if (spikes == null) return;

        bool visible = rise > 0.001f;
        foreach (Spike spike in spikes)
        {
            if (spike.transform == null) continue;

            spike.transform.localPosition = new Vector3(0f, -spike.height * (1f - rise), 0f);
            foreach (Renderer part in spike.renderers)
            {
                if (part.enabled != visible) part.enabled = visible;
            }
        }
    }

    private static Renderer CreatePart(string name, Transform parent, Mesh mesh, Material material)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;

        var meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        return meshRenderer;
    }

    // 메시/머티리얼 (모든 장판이 공유) --------------------------------------

    private static void EnsureAssets()
    {
        if (bodyMeshes == null)
        {
            bodyMeshes = new Mesh[3];
            hullMeshes = new Mesh[3];
            for (int v = 0; v < 3; v++)
            {
                float bend = Bend * (0.7f + v * 0.3f);
                float twist = 0.6f * (v - 1);
                uint seed = (uint)(11 + v * 7);
                bodyMeshes[v] = BuildMesh(bend, twist, seed, true);
                hullMeshes[v] = BuildMesh(bend, twist, seed, false);
            }
        }

        if (!materialsLoaded)
        {
            materialsLoaded = true;

            // 몸체: 보스 몬스터와 같은 셀 셰이더 (몬스터 머티리얼이 쓰고 있어 빌드에 포함됨)
            Shader cel = Shader.Find("PVFX/PVFX_URP_Low_CelShader1.0");
            if (cel == null) cel = Shader.Find("Universal Render Pipeline/Lit");
            if (cel == null) cel = Shader.Find("Sprites/Default");
            bodyMaterial = new Material(cel) { name = "BossSpikeBody" };
            if (bodyMaterial.HasProperty("_Color")) bodyMaterial.SetColor("_Color", BodyColor);
            if (bodyMaterial.HasProperty("_BaseColor")) bodyMaterial.SetColor("_BaseColor", BodyColor);
            if (bodyMaterial.HasProperty("_EmissionColor")) bodyMaterial.SetColor("_EmissionColor", BodyEmission);
            if (bodyMaterial.HasProperty("_OutlineWidth")) bodyMaterial.SetFloat("_OutlineWidth", 0f); // 외곽선은 따로 그림
            if (bodyMaterial.HasProperty("_RimColor")) bodyMaterial.SetColor("_RimColor", new Color(1f, 0.45f, 0.4f, 1f));
            if (bodyMaterial.HasProperty("_RimIntensity")) bodyMaterial.SetFloat("_RimIntensity", 0.6f);

            // 외곽선: Resources 폴더의 셰이더 (Resources에 있어야 빌드에 포함됨)
            Shader outline = Resources.Load<Shader>("BossSpikeOutline");
            if (outline != null)
            {
                outlineMaterial = new Material(outline) { name = "BossSpikeOutline" };
                outlineMaterial.SetColor("_OutlineColor", Color.black);
                outlineMaterial.SetFloat("_OutlineWidth", OutlineWidth);
            }
            else
            {
                Debug.LogWarning("[Boss] Resources/BossSpikeOutline 셰이더를 찾지 못해 가시 외곽선 없이 표시합니다.");
            }
        }
    }

    // 높이 1짜리 가시. flat = true면 면마다 정점을 따로 둬서 각진 음영(몸체), false면 정점 공유로 부드러운 노멀(외곽선)
    private static Mesh BuildMesh(float bend, float twist, uint seed, bool flat)
    {
        var random = new SeededRandom(seed);

        var rings = new Vector3[Segments, Sides];
        for (int k = 0; k < Segments; k++)
        {
            float y = (float)k / Segments;
            float r = BaseRadius * Mathf.Pow(1f - y, 0.85f);
            float cx = bend * y * y;
            for (int i = 0; i < Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides + twist * y;
                float rr = r * (1f + Noise * (random.Next() - 0.5f));
                rings[k, i] = new Vector3(cx + Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
            }
        }
        Vector3 tip = new Vector3(bend, 1f, 0f);

        // 정점 공유 인덱스: ring k의 i번 = k * Sides + i, 꼭대기 = Segments * Sides
        var sharedVertices = new List<Vector3>();
        for (int k = 0; k < Segments; k++)
            for (int i = 0; i < Sides; i++)
                sharedVertices.Add(rings[k, i]);
        sharedVertices.Add(tip);
        int tipIndex = Segments * Sides;

        var triangles = new List<int>();
        for (int k = 0; k < Segments - 1; k++)
        {
            for (int i = 0; i < Sides; i++)
            {
                int j = (i + 1) % Sides;
                int a = k * Sides + i, b = k * Sides + j, c = (k + 1) * Sides + i, d = (k + 1) * Sides + j;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }
        int top = (Segments - 1) * Sides;
        for (int i = 0; i < Sides; i++)
        {
            triangles.Add(top + i); triangles.Add(tipIndex); triangles.Add(top + (i + 1) % Sides);
        }

        var mesh = new Mesh { name = flat ? "BossSpike" : "BossSpikeHull" };
        if (flat)
        {
            var vertices = new Vector3[triangles.Count];
            var indices = new int[triangles.Count];
            for (int n = 0; n < triangles.Count; n++)
            {
                vertices[n] = sharedVertices[triangles[n]];
                indices[n] = n;
            }
            mesh.vertices = vertices;
            mesh.triangles = indices;
        }
        else
        {
            mesh.SetVertices(sharedVertices);
            mesh.SetTriangles(triangles, 0);
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // 미리보기(JS mulberry32)와 같은 결과를 내는 시드 난수 -> 메시 모양이 미리보기와 동일
    private struct SeededRandom
    {
        private uint state;

        public SeededRandom(uint seed) { state = seed; }

        public float Next()
        {
            unchecked
            {
                state += 0x6D2B79F5u;
                uint t = (state ^ (state >> 15)) * (1u | state);
                t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                return (t ^ (t >> 14)) / 4294967296f;
            }
        }
    }
}
