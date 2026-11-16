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
    private Transform visual;

    public static GravityField Create(Vector3 position, SpecialAttackSettings settings, float damage)
    {
        var go = new GameObject("GravityField");
        go.transform.position = position;

        GravityField field = go.AddComponent<GravityField>();
        field.settings = settings;
        field.damage = damage;

        Color color = settings.color;
        color.a = 0.25f;
        field.visual = SpecialAttackUtil.CreateSphere(color).transform;
        field.visual.SetParent(go.transform, false);
        field.visual.localScale = Vector3.one * settings.radius * 2f;

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

        // 크기가 살짝 맥동하는 연출
        float pulse = 1f + Mathf.Sin(elapsed * 20f) * 0.05f;
        visual.localScale = Vector3.one * settings.radius * 2f * pulse;

        Vector3 center = transform.position;
        Collider[] colliders = Physics.OverlapSphere(center, settings.radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

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
