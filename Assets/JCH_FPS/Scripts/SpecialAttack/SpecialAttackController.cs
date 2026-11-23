// 11/16 - 특수 공격 퍽 관리
// 플레이어가 특수 공격 퍽을 고르면 PlayerMove.ApplyPerk에서 자동으로 붙는다.
// 방아쇠를 당길 때마다(산탄도 1회) 퍽별로 카운트해서 주기가 되면 특수 공격 발동.
// 같은 퍽을 다시 고르면 레벨이 올라 주기 감소 + 피해 배율 증가 (수치는 PerkData.special에서 조절)
using System.Collections.Generic;
using UnityEngine;

public class SpecialAttackController : MonoBehaviour
{
    private class Entry
    {
        public PerkData perk;
        public int level = 1;
        public int shotCount = 0;
        public FireRing fireRing; // 화염의 고리 전용

        public int Interval
        {
            get
            {
                SpecialAttackSettings s = perk.special;
                int interval = s.shotInterval - s.intervalReducePerLevel * (level - 1);
                return Mathf.Max(Mathf.Max(1, s.minShotInterval), interval);
            }
        }

        public float DamageMultiplier => perk.special.damageMultiplier + perk.special.damageMultiplierPerLevel * (level - 1);
    }

    private readonly List<Entry> entries = new List<Entry>();

    public void AddOrUpgrade(PerkData perk)
    {
        if (perk == null || perk.perkType == PerkType.None || perk.special == null) return;

        foreach (Entry entry in entries)
        {
            if (entry.perk == perk)
            {
                entry.level++;
                return;
            }
        }

        entries.Add(new Entry { perk = perk });
    }

    // PlayerMove.FireGun에서 방아쇠 1회마다 호출
    public void OnShotFired(Vector3 origin, Vector3 direction, float playerDamage)
    {
        foreach (Entry entry in entries)
        {
            entry.shotCount++;

            if (entry.shotCount >= entry.Interval)
            {
                entry.shotCount = 0;
                Fire(entry, origin, direction, playerDamage * entry.DamageMultiplier);
            }
        }
    }

    private void Fire(Entry entry, Vector3 origin, Vector3 direction, float damage)
    {
        if (entry.perk.perkType == PerkType.FireRing)
        {
            if (entry.fireRing == null)
            {
                entry.fireRing = FireRing.Create(transform, entry.perk.special);
            }
            entry.fireRing.Activate(damage);
            return;
        }

        // 2026-11-23: 중력탄 범위와 비행 VFX 크기를 발사 시점 퍽 레벨에 맞추기 위해 레벨을 전달합니다.
        SpecialProjectile.Spawn(entry.perk.perkType, entry.perk.special, origin, direction, damage, gameObject, entry.level);
    }

    private void OnDestroy()
    {
        foreach (Entry entry in entries)
        {
            if (entry.fireRing != null)
            {
                Destroy(entry.fireRing.gameObject);
            }
        }
    }
}

// 11/16 - 특수 공격 공용 기능 (적 판별, 범위 피해, 외형 생성)
public static class SpecialAttackUtil
{
    private static Shader shader;

    public static bool IsPaused => GameManager.Instance != null && GameManager.Instance.isPaused;

    // 살아 있는 적(일반 적/보스)만 대상. 플레이어는 자기 특수 공격에 피해를 입지 않음
    public static bool IsAliveEnemy(IDamageable target)
    {
        return (target is EnemyBase enemy && !enemy.IsDead) || (target is BossEnemy boss && !boss.IsDead);
    }

    public static IDamageable GetAliveEnemy(Collider collider)
    {
        if (collider == null) return null;

        IDamageable target = collider.GetComponentInParent<IDamageable>();
        return target != null && IsAliveEnemy(target) ? target : null;
    }

    // 반경 안의 살아 있는 적 모두에게 한 번씩 피해
    public static void DamageArea(Vector3 center, float radius, float damage)
    {
        Collider[] colliders = Physics.OverlapSphere(center, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        var damaged = new HashSet<IDamageable>();

        foreach (Collider col in colliders)
        {
            IDamageable target = GetAliveEnemy(col);
            if (target != null && damaged.Add(target))
            {
                target.TakeDamage(damage);
            }
        }
    }

    // 프리팹이 있으면 그 모양으로, 없으면 색이 있는 구체(콜라이더 없음)를 생성
    public static Transform CreateVisual(Transform parent, GameObject prefab, Color color, float diameter)
    {
        if (prefab != null)
        {
            GameObject visual = Object.Instantiate(prefab, parent);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // 외형으로만 쓰므로 충돌/물리/기존 총알 동작은 끔
            foreach (Bullet bullet in visual.GetComponentsInChildren<Bullet>()) Object.DestroyImmediate(bullet);
            foreach (Collider col in visual.GetComponentsInChildren<Collider>()) col.enabled = false;
            foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;

            return visual.transform;
        }

        GameObject sphere = CreateSphere(color);
        sphere.transform.SetParent(parent, false);
        sphere.transform.localScale = Vector3.one * diameter;
        return sphere.transform;
    }

    // 콜라이더 없는 구체 + 라이트 영향 없는 반투명 가능 머티리얼 (BossVisuals와 같은 Sprites/Default 사용)
    public static GameObject CreateSphere(Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(sphere.GetComponent<Collider>()); // 같은 프레임의 충돌 검사에 걸리지 않도록 즉시 제거

        if (shader == null) shader = Shader.Find("Sprites/Default");

        Renderer renderer = sphere.GetComponent<Renderer>();
        var material = new Material(shader) { color = color, renderQueue = 3000 };
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        sphere.AddComponent<DestroyMaterialOnDestroy>();

        return sphere;
    }
}
