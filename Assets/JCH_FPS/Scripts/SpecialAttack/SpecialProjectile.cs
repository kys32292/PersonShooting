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

    // 2026-11-23: 발사 후 강화되어도 날아가던 탄의 범위/외형은 발사 당시 레벨을 유지합니다.
    private float gravityRadius;

    // 2026-11-23: 기존 호출은 기본 레벨 1을 사용하며 컨트롤러는 실제 퍽 레벨을 전달합니다.
    public static SpecialProjectile Spawn(PerkType type, SpecialAttackSettings settings, Vector3 origin, Vector3 direction, float damage, GameObject owner, int level = 1)
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
        // 2026-11-23: 중력/폭발/유탄/관통탄은 각 전용 VFX를 사용하고 나머지 탄은 기존 프리팹 경로를 유지합니다.
        float gravityRadius = settings.GetGravityRadius(level);
        if (type == PerkType.Graviton)
        {
            // 이전 중력탄 프리팹 외형은 아래 공통 생성 대신 전용 서비스로 대체했습니다.
            // SpecialAttackUtil.CreateVisual(go.transform, settings.projectilePrefab, settings.color, settings.projectileRadius * 2f);
            PesonShooting.VFX.VfxService.AttachGravityProjectile(go.transform, gravityRadius);
        }
        // 2026-11-23: 폭발탄은 일반탄과 같은 선 궤적의 빨간색 버전만 표시합니다.
        else if (type == PerkType.Explosive)
        {
            // 이전 프리팹 외형은 전용 빨간 일반탄 궤적으로 대체하여 주석 보관합니다.
            // SpecialAttackUtil.CreateVisual(go.transform, settings.projectilePrefab, settings.color, settings.projectileRadius * 2f);
            PesonShooting.VFX.VfxService.AttachExplosiveProjectile(go.transform);
        }
        // 2026-11-23: 유탄 외형도 일반탄 궤적을 사용하되 초록색으로 구분합니다.
        else if (type == PerkType.Grenade)
        {
            // 이전 유탄 프리팹 외형은 주석으로 보관합니다.
            // SpecialAttackUtil.CreateVisual(go.transform, settings.projectilePrefab, settings.color, settings.projectileRadius * 2f);
            PesonShooting.VFX.VfxService.AttachGrenadeProjectile(go.transform);
        }
        // 2026-11-23: 관통탄은 참조 이미지의 금빛 광선/별 모양 탄두를 비행 중에만 표시합니다.
        else if (type == PerkType.Piercing)
        {
            // 이전 관통탄 프리팹 외형은 새 전용 VFX로 대체하여 주석으로 보관합니다.
            // SpecialAttackUtil.CreateVisual(go.transform, settings.projectilePrefab, settings.color, settings.projectileRadius * 2f);
            PesonShooting.VFX.VfxService.AttachPiercingProjectile(go.transform);
        }
        else
        {
            SpecialAttackUtil.CreateVisual(go.transform, settings.projectilePrefab, settings.color, settings.projectileRadius * 2f);
        }

        SpecialProjectile projectile = go.AddComponent<SpecialProjectile>();
        projectile.type = type;
        projectile.settings = settings;
        projectile.damage = damage;
        projectile.owner = owner;
        // 2026-11-23: 같은 반경을 착탄 시 실제 중력장에도 전달합니다. 투사체 충돌 판정 크기는 유지합니다.
        projectile.gravityRadius = gravityRadius;
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
                // 2026-11-23: 레벨별 범위를 전투 판정/반구 VFX에 동일하게 전달합니다.
                GravityField.Create(point, settings, damage, gravityRadius);
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
        // 2026-11-23: 폭발탄만 FlameAmmo 참조 기반 화염으로 교체합니다. 피해 판정은 기존 1회 광역 피해입니다.
        if (type == PerkType.Explosive)
        {
            // 이전 폭발탄 구체 연출을 주석 보관합니다.
            // SpecialFlash.Spawn(point, settings.radius, settings.color, 0.25f);
            PesonShooting.VFX.VfxService.PlayExplosiveImpact(point, settings.radius);
        }
        else
        {
            // 2026-11-23: 유탄의 이전 구체 연출을 참조 기반 화염 기둥/연기/먼지로 교체합니다.
            // SpecialFlash.Spawn(point, settings.radius, settings.color, 0.25f);
            PesonShooting.VFX.VfxService.PlayGrenadeImpact(point, settings.radius);
        }
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
