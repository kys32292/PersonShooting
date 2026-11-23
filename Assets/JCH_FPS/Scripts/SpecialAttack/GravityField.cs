// 11/16 - 중력자탄이 부딪힌 자리에 생기는 중력장
// duration 동안 반경 안의 일반 적을 중심으로 끌어당기고(보스는 끌려오지 않음), 범위 안 적 모두에게 지속 피해
using UnityEngine;
using UnityEngine.AI;

public class GravityField : MonoBehaviour
{
    private const float TickInterval = 0.25f;

    private SpecialAttackSettings settings;
    private float damage;
    private float elapsed = 0f;
    private float tickTimer = 0f;
    // 2026-11-23: 공유 퍽 데이터와 별도로 발사 시 확정된 레벨별 실제 반경을 보관합니다.
    private float radius;
    // 2026-11-23: 기존 구체 연출용 필드는 보관만 하고 새 VFX로 대체합니다.
    // private Transform visual;

    // 2026-11-23: 반경을 생략한 기존 호출은 퍽의 기본 반경을 그대로 사용합니다.
    public static GravityField Create(Vector3 position, SpecialAttackSettings settings, float damage, float effectiveRadius = -1f)
    {
        var go = new GameObject("GravityField");
        go.transform.position = position;

        GravityField field = go.AddComponent<GravityField>();
        field.settings = settings;
        field.damage = damage;
        // 2026-11-23: 판정과 외형의 반경이 같도록 한 번만 확정합니다.
        field.radius = Mathf.Max(0.001f, effectiveRadius >= 0f ? effectiveRadius : settings.radius);

        // 2026-11-23: 이전의 반투명 구체 생성 코드를 주석으로 보관합니다.
        // Color color = settings.color;
        // color.a = 0.25f;
        // field.visual = SpecialAttackUtil.CreateSphere(color).transform;
        // field.visual.SetParent(go.transform, false);
        // field.visual.localScale = Vector3.one * settings.radius * 2f;

        // 2026-11-23: 중력탄의 실제 착탄 위치에 Config 기반 흡입 입자/반구 연출을 생성합니다.
        PesonShooting.VFX.VfxService.AttachGravityWell(go.transform, field.radius, settings.duration);

        return field;
    }

    private void Update()
    {
        if (SpecialAttackUtil.IsPaused) return;

        float dt = Time.deltaTime;
        elapsed += dt;

        if (elapsed >= settings.duration)
        {
            Destroy(gameObject);
            return;
        }

        // 2026-11-23: 기존 구체 맥동도 비활성화합니다. 새 VFX의 맥동은 GravityWellVfx가 담당합니다.
        // float pulse = 1f + Mathf.Sin(elapsed * 20f) * 0.05f;
        // visual.localScale = Vector3.one * settings.radius * 2f * pulse;

        Vector3 center = transform.position;
        // 2026-11-23: 강화된 반경으로 끌어당김/지속 피해 대상을 함께 검사합니다.
        Collider[] colliders = Physics.OverlapSphere(center, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        tickTimer += dt;
        bool doDamage = tickTimer >= TickInterval;
        if (doDamage) tickTimer -= TickInterval;

        var handled = new System.Collections.Generic.HashSet<IDamageable>();
        foreach (Collider col in colliders)
        {
            IDamageable target = SpecialAttackUtil.GetAliveEnemy(col);
            if (target == null || !handled.Add(target)) continue;

            // 끌어당기기: 일반 적만
            if (target is EnemyBase enemy)
            {
                Pull(enemy.transform, center, dt);
            }

            if (doDamage)
            {
                target.TakeDamage(damage * settings.pullDamagePerSecond * TickInterval);
            }
        }
    }

    private void Pull(Transform enemy, Vector3 center, float dt)
    {
        Vector3 toCenter = center - enemy.position;
        toCenter.y = 0f;
        float distance = toCenter.magnitude;
        if (distance < 0.5f) return;

        Vector3 step = toCenter / distance * Mathf.Min(settings.pullSpeed * dt, distance);

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Move(step); // NavMesh 위에서만 움직이므로 벽을 뚫지 않음
        }
        else
        {
            enemy.position += step;
        }
    }
}
