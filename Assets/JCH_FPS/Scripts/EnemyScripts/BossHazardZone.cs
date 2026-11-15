// 11/15 - 보스 패턴 1: 바닥 장판
// 1. 경고(warningTime): 반투명 원이 점점 빠르게 점멸하고, 안쪽 원이 테두리까지 차오르며 발동 시점을 알려줌
// 2. 활성(activeTime): 진한 색으로 바뀌고, 안에 있는 플레이어에게 tickInterval마다 tickDamage
// 3. 종료: 서서히 사라짐
// 콜라이더를 쓰지 않고 거리로 판정함 (트리거를 두면 플레이어 총알이 장판에 막혀 사라지기 때문)
// 일시정지(GameManager.isPaused) 중에는 시간이 흐르지 않음
using UnityEngine;

public class BossHazardZone : MonoBehaviour
{
    private float radius;
    private float warningTime;
    private float activeTime;
    private float tickDamage;
    private float tickInterval;

    private Transform player;
    private IDamageable playerDamageable;

    private float timer = 0f;
    private float tickTimer = 0f;

    private Renderer outerRenderer;
    private Renderer innerRenderer;
    private Transform inner;

    private static readonly Color WarningColor = new Color(1f, 0.25f, 0.15f, 1f);
    private static readonly Color ActiveColor = new Color(0.85f, 0.05f, 0.25f, 1f);
    private const float FadeOutTime = 0.3f;

    public static BossHazardZone Spawn(Vector3 groundPosition, float radius, float warningTime, float activeTime,
        float tickDamage, float tickInterval, Transform player)
    {
        var go = new GameObject("Boss Hazard Zone");
        go.transform.position = groundPosition + Vector3.up * 0.04f; // 바닥과 겹쳐 깜빡이지 않도록 살짝 띄움

        var zone = go.AddComponent<BossHazardZone>();
        zone.radius = radius;
        zone.warningTime = warningTime;
        zone.activeTime = activeTime;
        zone.tickDamage = tickDamage;
        zone.tickInterval = tickInterval;
        zone.player = player;
        zone.playerDamageable = player != null ? player.GetComponent<IDamageable>() : null;
        zone.BuildVisual();

        return zone;
    }

    private void BuildVisual()
    {
        Transform outer = BossVisuals.CreateDisc("Outer", transform, radius, out outerRenderer);
        outer.localPosition = Vector3.zero;

        inner = BossVisuals.CreateDisc("Inner", transform, radius, out innerRenderer);
        inner.localPosition = Vector3.up * 0.01f;
        inner.localScale = Vector3.zero;
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
        }
        else if (timer < warningTime + activeTime)
        {
            UpdateActive(timer - warningTime);
        }
        else if (timer < warningTime + activeTime + FadeOutTime)
        {
            float fade = 1f - (timer - warningTime - activeTime) / FadeOutTime;
            BossVisuals.SetColor(outerRenderer, ActiveColor, 0.5f * fade);
            BossVisuals.SetColor(innerRenderer, ActiveColor, 0f);
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
    }

    private void UpdateActive(float activeElapsed)
    {
        inner.localScale = Vector3.one;

        float pulse = 0.5f + 0.5f * Mathf.Sin(activeElapsed * 6f);
        BossVisuals.SetColor(outerRenderer, ActiveColor, 0.55f + 0.15f * pulse);
        BossVisuals.SetColor(innerRenderer, ActiveColor, 0.25f);

        // 활성화되는 순간 바로 한 번 판정하고, 이후 tickInterval마다 판정
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;

            if (IsPlayerInside())
            {
                playerDamageable.TakeDamage(tickDamage);
            }
        }
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
