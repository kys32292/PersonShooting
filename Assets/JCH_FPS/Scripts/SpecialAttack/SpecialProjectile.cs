// 11/16 - 특수 공격 투사체 (중력자탄 / 유탄 / 폭발탄 / 관통탄 / 유도탄)
// Rigidbody 없이 직접 이동하고, 매 프레임 이동 경로를 SphereCast로 검사해서 빠른 속도에서도 뚫고 지나가지 않게 함
using System.Collections.Generic;
using UnityEngine;

public class SpecialProjectile : MonoBehaviour
{
    private PerkType type;
    private SpecialAttackSettings settings;
    private float damage;
    private GameObject owner;

    private Vector3 velocity;
    private float lifeTime = 0f;

    private readonly HashSet<IDamageable> piercedTargets = new HashSet<IDamageable>(); // 관통탄: 같은 적 중복 피해 방지
    private Transform homingTarget;

    public static SpecialProjectile Spawn(PerkType type, SpecialAttackSettings settings, Vector3 origin, Vector3 direction, float damage, GameObject owner)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = owner != null ? owner.transform.forward : Vector3.forward;
        direction.Normalize();

        // 유탄: 위쪽으로 살짝 들어서 포물선으로 날아가게 함
        if (type == PerkType.Grenade)
        {
            direction = Vector3.RotateTowards(direction, Vector3.up, settings.launchAngle * Mathf.Deg2Rad, 0f);
        }

        var go = new GameObject("Special_" + type);
        go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
        SpecialAttackUtil.CreateVisual(go.transform, settings.projectilePrefab, settings.color, settings.projectileRadius * 2f);

        SpecialProjectile projectile = go.AddComponent<SpecialProjectile>();
        projectile.type = type;
        projectile.settings = settings;
        projectile.damage = damage;
        projectile.owner = owner;
        projectile.velocity = direction * settings.projectileSpeed;
        return projectile;
    }

    private void Update()
    {
        if (SpecialAttackUtil.IsPaused) return;

        float dt = Time.deltaTime;
        lifeTime += dt;

        if (lifeTime >= settings.projectileLifetime)
        {
            // 유탄은 시간이 다 되면 그 자리에서 폭발, 나머지는 소멸
            if (type == PerkType.Grenade) Explode(transform.position);
            Destroy(gameObject);
            return;
        }

        if (type == PerkType.Grenade)
        {
            velocity += Physics.gravity * settings.gravityScale * dt;
        }
        else if (type == PerkType.Homing)
        {
            UpdateHoming(dt);
        }

        Vector3 move = velocity * dt;
        float distance = move.magnitude;
        if (distance > 0f && CheckHits(move / distance, distance))
        {
            return; // 충돌로 소멸함
        }

        transform.position += move;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    // 이동 경로에 걸린 것들을 가까운 순서대로 처리. 소멸했으면 true
    private bool CheckHits(Vector3 direction, float distance)
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, settings.projectileRadius, direction, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (owner != null && hit.collider.transform.IsChildOf(owner.transform)) continue;

            IDamageable enemy = SpecialAttackUtil.GetAliveEnemy(hit.collider);

            // 이미 겹친 상태에서 시작하면 hit.point가 (0,0,0)이므로 현재 위치를 사용
            Vector3 point = hit.distance <= 0f ? transform.position : hit.point;

            if (type == PerkType.Piercing)
            {
                if (enemy != null)
                {
                    if (piercedTargets.Add(enemy))
                    {
                        PesonShooting.VFX.VfxService.PlayEnemyHit(hit.collider, point, direction);
                        enemy.TakeDamage(damage);

                        if (settings.maxPierceCount > 0 && piercedTargets.Count >= settings.maxPierceCount)
                        {
                            Destroy(gameObject);
                            return true;
                        }
                    }
                    continue; // 적은 뚫고 지나감
                }

                // 이미 죽은 적의 콜라이더도 그냥 통과
                if (hit.collider.GetComponentInParent<IDamageable>() != null) continue;

                Destroy(gameObject); // 벽 등에 닿으면 소멸
                return true;
            }

            // 이미 죽은 적은 무시하고 지나감
            if (enemy == null && hit.collider.GetComponentInParent<IDamageable>() != null) continue;

            OnImpact(point, enemy, hit.collider, direction);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    private void OnImpact(Vector3 point, IDamageable enemy, Collider hitCollider, Vector3 direction)
    {
        switch (type)
        {
            case PerkType.Explosive:
            case PerkType.Grenade:
                Explode(point);
                break;

            case PerkType.Graviton:
                if (enemy != null)
                {
                    PesonShooting.VFX.VfxService.PlayEnemyHit(hitCollider, point, direction);
                    enemy.TakeDamage(damage);
                }
                GravityField.Create(point, settings, damage);
                break;

            case PerkType.Homing:
            default:
                if (enemy != null)
                {
                    PesonShooting.VFX.VfxService.PlayEnemyHit(hitCollider, point, direction);
                    enemy.TakeDamage(damage);
                }
                break;
        }
    }

    private void Explode(Vector3 point)
    {
        SpecialAttackUtil.DamageArea(point, settings.radius, damage);
        SpecialFlash.Spawn(point, settings.radius, settings.color, 0.25f);
    }

    // 유도탄: 범위 안의 가장 가까운 살아 있는 적 쪽으로 진행 방향을 회전
    private void UpdateHoming(float dt)
    {
        if (homingTarget == null || !IsAlive(homingTarget))
        {
            homingTarget = FindNearestEnemy();
        }

        if (homingTarget == null) return;

        Vector3 targetPos = homingTarget.position + Vector3.up * 1f;
        Vector3 desired = (targetPos - transform.position).normalized;
        float speed = velocity.magnitude;
        Vector3 newDir = Vector3.RotateTowards(velocity.normalized, desired, settings.homingTurnSpeed * Mathf.Deg2Rad * dt, 0f);
        velocity = newDir * speed;
    }

    private Transform FindNearestEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, settings.homingRange,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        Transform nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider col in colliders)
        {
            IDamageable enemy = SpecialAttackUtil.GetAliveEnemy(col);
            if (enemy == null) continue;

            Transform t = ((Component)enemy).transform;
            float dist = (t.position - transform.position).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = t;
            }
        }

        return nearest;
    }

    private static bool IsAlive(Transform t)
    {
        IDamageable enemy = t.GetComponent<IDamageable>();
        return enemy != null && SpecialAttackUtil.IsAliveEnemy(enemy);
    }
}
