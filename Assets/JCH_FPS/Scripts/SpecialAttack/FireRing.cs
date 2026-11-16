// 11/16 - 화염의 고리: 발동하면 duration 동안 플레이어 주변을 공전하는 화염 구체
// 다시 발동하면 남은 시간이 duration으로 초기화되고 피해량이 갱신됨
// 같은 적은 ringHitInterval마다 한 번씩만 피해
using System.Collections.Generic;
using UnityEngine;

public class FireRing : MonoBehaviour
{
    private Transform player;
    private SpecialAttackSettings settings;
    private float damage;
    private float remaining = 0f;
    private float angle = 0f;

    private readonly List<Transform> orbs = new List<Transform>();
    private readonly Dictionary<IDamageable, float> lastHitTime = new Dictionary<IDamageable, float>();

    public static FireRing Create(Transform player, SpecialAttackSettings settings)
    {
        // 플레이어 회전의 영향을 받지 않도록 자식이 아닌 별도 오브젝트로 만들고 위치만 따라감
        var go = new GameObject("FireRing");
        FireRing ring = go.AddComponent<FireRing>();
        ring.player = player;
        ring.settings = settings;

        int count = Mathf.Max(1, settings.ringOrbCount);
        for (int i = 0; i < count; i++)
        {
            var orb = new GameObject("FireOrb").transform;
            orb.SetParent(go.transform, false);
            SpecialAttackUtil.CreateVisual(orb, settings.projectilePrefab, settings.color, settings.projectileRadius * 2f);
            ring.orbs.Add(orb);
        }

        go.SetActive(false);
        return ring;
    }

    public void Activate(float newDamage)
    {
        damage = newDamage;
        remaining = settings.duration;
        gameObject.SetActive(true);
        UpdateOrbPositions();
    }

    private void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        if (SpecialAttackUtil.IsPaused) return;

        float dt = Time.deltaTime;
        remaining -= dt;
        if (remaining <= 0f)
        {
            lastHitTime.Clear();
            gameObject.SetActive(false);
            return;
        }

        angle += settings.ringOrbitSpeed * dt;
        UpdateOrbPositions();
        DamageEnemies();
    }

    private void UpdateOrbPositions()
    {
        Vector3 center = player.position + Vector3.up * 1f;
        transform.position = center;

        for (int i = 0; i < orbs.Count; i++)
        {
            float a = (angle + 360f * i / orbs.Count) * Mathf.Deg2Rad;
            orbs[i].position = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * settings.radius;
        }
    }

    private void DamageEnemies()
    {
        float now = Time.time;

        foreach (Transform orb in orbs)
        {
            Collider[] colliders = Physics.OverlapSphere(orb.position, settings.projectileRadius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            foreach (Collider col in colliders)
            {
                IDamageable target = SpecialAttackUtil.GetAliveEnemy(col);
                if (target == null) continue;

                if (lastHitTime.TryGetValue(target, out float last) && now - last < settings.ringHitInterval) continue;

                lastHitTime[target] = now;
                PesonShooting.VFX.VfxService.PlayEnemyHit(col, orb.position, col.transform.position - orb.position);
                target.TakeDamage(damage);
            }
        }
    }
}
