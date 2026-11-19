// 11/15 - 보스 패턴 1: 바닥 장판 (11/19 - QA 피드백 반영: 반구 경고 + 즉발 분출)
// 1. 경고(warningTime): 바닥 원이 점점 빠르게 점멸하며 안쪽 원이 차오르고, 위로 반투명 반구가 솟아오르며 점멸
//    FPS 시점에서 발밑 바닥만으로는 잘 안 보여서, 눈높이에서도 보이는 반구를 추가함
// 2. 분출(EruptTime) -> 유지(HoldTime) -> 사라짐(RetractTime): 이 시간 동안 닿으면 데미지 1회 (장판 1개당 1번)
// 3. 종료: 바닥 자국이 사라지고 제거
// 콜라이더를 쓰지 않고 거리로 판정함 (트리거를 두면 플레이어 총알이 장판에 막혀 사라지기 때문)
// 일시정지(GameManager.isPaused) 중에는 시간이 흐르지 않음
// 경고 중인 장판 안에 플레이어가 있으면 BossDangerVignette가 화면 가장자리를 붉게 표시함
using System.Collections.Generic;
using UnityEngine;

public class BossHazardZone : MonoBehaviour
{
    public const float EruptTime = 0.12f;
    public const float HoldTime = 0.5f;
    public const float RetractTime = 0.3f;
    private const float ScarFadeTime = 0.2f;
    private const float DomeHeightRatio = 0.7f; // 반구 높이 = 반지름 x 0.7

    private static readonly Color WarningColor = new Color(1f, 0.25f, 0.15f, 1f);
    private static readonly Color ScarColor = new Color(0.55f, 0.03f, 0.1f, 1f);

    // 경고 중인 장판 목록 (화면 가장자리 경고용)
    private static readonly List<BossHazardZone> warningZones = new List<BossHazardZone>();

    private float radius;
    private float warningTime;
    private float damage;

    private Transform player;
    private IDamageable playerDamageable;

    private float timer = 0f;
    private bool hasHitPlayer = false;

    private Renderer outerRenderer;
    private Renderer innerRenderer;
    private Renderer domeRenderer;
    private Transform inner;
    private Transform dome;

    // 분출 가시 (BossSpikes: 면 수 9, 휘어짐 0.20)
    private Transform spikeRoot;
    private List<BossSpikes.Spike> spikes;

    public static BossHazardZone Spawn(Vector3 groundPosition, float radius, float warningTime, float damage, Transform player)
    {
        var go = new GameObject("Boss Hazard Zone");
        go.transform.position = groundPosition + Vector3.up * 0.04f; // 바닥과 겹쳐 깜빡이지 않도록 살짝 띄움

        var zone = go.AddComponent<BossHazardZone>();
        zone.radius = radius;
        zone.warningTime = warningTime;
        zone.damage = damage;
        zone.player = player;
        zone.playerDamageable = player != null ? player.GetComponent<IDamageable>() : null;
        zone.BuildVisual();

        warningZones.Add(zone);
        BossDangerVignette.Ensure();

        return zone;
    }

    // 플레이어가 경고 중인 장판 안에 있으면 가장 임박한 장판의 진행도(0~1), 없으면 -1
    public static float GetPlayerWarningProgress()
    {
        float best = -1f;
        foreach (BossHazardZone zone in warningZones)
        {
            if (zone == null || zone.timer >= zone.warningTime) continue;
            if (!zone.IsPlayerInside()) continue;

            best = Mathf.Max(best, zone.timer / zone.warningTime);
        }
        return best;
    }

    private void BuildVisual()
    {
        Transform outer = BossVisuals.CreateDisc("Outer", transform, radius, out outerRenderer);
        outer.localPosition = Vector3.zero;

        inner = BossVisuals.CreateDisc("Inner", transform, radius, out innerRenderer);
        inner.localPosition = Vector3.up * 0.01f;
        inner.localScale = Vector3.zero;

        dome = BossVisuals.CreateDome("Dome", transform, radius, radius * DomeHeightRatio, out domeRenderer);
        dome.localScale = new Vector3(1f, 0.15f, 1f);

        spikeRoot = new GameObject("Spikes").transform;
        spikeRoot.SetParent(transform, false);
        spikes = BossSpikes.Build(spikeRoot, radius); // 바닥 아래에 숨긴 상태로 미리 만들어 둠
    }

    private void OnDestroy()
    {
        warningZones.Remove(this);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer < warningTime)
        {
            UpdateWarning(timer / warningTime);
            return;
        }

        float elapsed = timer - warningTime;

        if (elapsed < EruptTime + HoldTime + RetractTime)
        {
            UpdateEruption(elapsed);
        }
        else if (elapsed < EruptTime + HoldTime + RetractTime + ScarFadeTime)
        {
            float fade = 1f - (elapsed - EruptTime - HoldTime - RetractTime) / ScarFadeTime;
            BossVisuals.SetColor(outerRenderer, ScarColor, 0.5f * fade);
            SetSpikeRise(0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void UpdateWarning(float t)
    {
        // 점멸 속도가 4Hz -> 14Hz로 점점 빨라짐
        float frequency = Mathf.Lerp(4f, 14f, t);
        float blink = 0.5f + 0.5f * Mathf.Sin(timer * frequency * Mathf.PI * 2f);

        BossVisuals.SetColor(outerRenderer, WarningColor, Mathf.Lerp(0.12f, 0.4f, blink));
        BossVisuals.SetColor(innerRenderer, WarningColor, 0.35f);
        inner.localScale = Vector3.one * t; // 안쪽 원이 테두리까지 차오르면 발동

        // 반구가 낮게 시작해 발동 시점에 완전히 솟아오름
        dome.localScale = new Vector3(1f, Mathf.Lerp(0.15f, 1f, t), 1f);
        BossVisuals.SetColor(domeRenderer, WarningColor, Mathf.Lerp(0.08f, 0.3f, blink));
    }

    private void UpdateEruption(float elapsed)
    {
        if (dome.gameObject.activeSelf)
        {
            // 분출 시작: 경고 표시 정리 + 약한 흔들림 (여러 개가 동시에 터져도 CameraShake는 센 쪽만 유지)
            dome.gameObject.SetActive(false);
            inner.gameObject.SetActive(false);
            warningZones.Remove(this);
            CameraShake.Shake(0.08f, 0.2f);
        }

        float rise;
        if (elapsed < EruptTime)
        {
            float k = elapsed / EruptTime;
            rise = 1f - Mathf.Pow(1f - k, 3f);
            rise *= 1f + 0.12f * Mathf.Sin(k * Mathf.PI); // 살짝 튀어 오르는 느낌
        }
        else if (elapsed < EruptTime + HoldTime)
        {
            rise = 1f;
        }
        else
        {
            rise = 1f - (elapsed - EruptTime - HoldTime) / RetractTime;
        }

        SetSpikeRise(rise);
        BossVisuals.SetColor(outerRenderer, ScarColor, 0.5f * Mathf.Clamp01(rise * 2f));

        // 분출 ~ 사라질 때까지 닿으면 1번 데미지
        if (!hasHitPlayer && IsPlayerInside())
        {
            hasHitPlayer = true;
            playerDamageable.TakeDamage(damage);
        }
    }

    // 가시 높이 (0 = 바닥 아래, 1 = 완전히 솟음)
    private void SetSpikeRise(float rise)
    {
        BossSpikes.SetRise(spikes, rise);
    }

    private bool IsPlayerInside()
    {
        if (player == null || playerDamageable == null)
        {
            return false;
        }

        Vector3 offset = player.position - transform.position;
        if (Mathf.Abs(offset.y) > 2.5f) // 점프 정도는 피할 수 없게, 다른 층은 맞지 않게
        {
            return false;
        }

        offset.y = 0f;
        return offset.sqrMagnitude <= radius * radius;
    }
}

// 11/15 - 장판/돌진 경로 경고 표시용 바닥 메시와 머티리얼
// URP 프로젝트에서도 항상 빌드에 포함되는 Sprites/Default 셰이더(알파 블렌딩, 라이트 영향 없음)를 사용
public static class BossVisuals
{
    private static Mesh discMesh;
    private static Mesh quadMesh;
    private static Mesh domeMesh;
    private static Shader shader;

    private static Shader GetShader()
    {
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        return shader;
    }

    private static Material CreateMaterial()
    {
        var material = new Material(GetShader());
        material.renderQueue = 3000;
        return material;
    }

    // 반지름 1짜리 원판(XZ 평면)을 radius 배율로 생성
    public static Transform CreateDisc(string name, Transform parent, float radius, out Renderer renderer)
    {
        if (discMesh == null)
        {
            discMesh = BuildDiscMesh(48);
        }

        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = discMesh;

        var holder = new GameObject(name + " Root").transform; // 바깥에서 localScale을 0~1로 조절할 수 있도록 반지름은 안쪽에 적용
        holder.SetParent(parent, false);
        go.transform.SetParent(holder, false);
        go.transform.localScale = new Vector3(radius, 1f, radius);

        renderer = SetupRenderer(go);
        return holder;
    }

    // 11/19 - 투사체/예열용 빛 구체 (지름 1, 콜라이더 없음)
    public static Transform CreateOrb(string name, Transform parent, out Renderer renderer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // 총알/투사체 판정에 걸리지 않도록 즉시 제거
        if (parent != null) go.transform.SetParent(parent, false);

        renderer = SetupRenderer(go);
        return go.transform;
    }

    // 11/19 - 반지름 radius, 높이 height인 반구 (Sprites/Default는 양면 렌더링이라 안쪽에서도 보임)
    // 바깥에서 holder의 localScale.y를 0~1로 조절해 솟아오르게 함
    public static Transform CreateDome(string name, Transform parent, float radius, float height, out Renderer renderer)
    {
        if (domeMesh == null)
        {
            domeMesh = BuildDomeMesh(40, 12);
        }

        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().sharedMesh = domeMesh;

        var holder = new GameObject(name + " Root").transform;
        holder.SetParent(parent, false);
        go.transform.SetParent(holder, false);
        go.transform.localScale = new Vector3(radius, height, radius);

        renderer = SetupRenderer(go);
        return holder;
    }

    // 길이 방향(+Z)으로 length, 폭 width인 사각형. 피벗은 시작점
    public static Transform CreateStrip(string name, Transform parent, float width, float length, out Renderer renderer)
    {
        if (quadMesh == null)
        {
            quadMesh = BuildStripMesh();
        }

        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = quadMesh;
        go.transform.localScale = new Vector3(width, 1f, length);

        renderer = SetupRenderer(go);
        return go.transform;
    }

    private static Renderer SetupRenderer(GameObject go)
    {
        var meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateMaterial();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        go.AddComponent<DestroyMaterialOnDestroy>();
        return meshRenderer;
    }

    public static void SetColor(Renderer renderer, Color color, float alpha)
    {
        if (renderer == null) return;

        color.a = alpha;
        renderer.sharedMaterial.color = color;
    }

    private static Mesh BuildDiscMesh(int segments)
    {
        var vertices = new Vector3[segments + 1];
        var triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i == segments - 1 ? 1 : i + 2;
            triangles[i * 3 + 2] = i + 1;
        }

        var mesh = new Mesh { name = "BossDisc" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // 반지름 1, 높이 1의 반구 (위도 rings, 경도 segments)
    private static Mesh BuildDomeMesh(int segments, int rings)
    {
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int r = 0; r <= rings; r++)
        {
            float phi = (Mathf.PI * 0.5f) * r / rings; // 0 = 바닥 가장자리, PI/2 = 꼭대기
            float y = Mathf.Sin(phi);
            float ringRadius = Mathf.Cos(phi);

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * ringRadius, y, Mathf.Sin(angle) * ringRadius));
            }
        }

        int row = segments + 1;
        for (int r = 0; r < rings; r++)
        {
            for (int i = 0; i < segments; i++)
            {
                int a = r * row + i;
                int b = a + 1;
                int c = a + row;
                int d = c + 1;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        var mesh = new Mesh { name = "BossDome" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildStripMesh()
    {
        var mesh = new Mesh { name = "BossStrip" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
            new Vector3(-0.5f, 0f, 1f), new Vector3(0.5f, 0f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
