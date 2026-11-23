// 11/09 - 파일 인코딩 CP949 -> UTF-8 변환 (한글 주석 깨짐 해결)
// 11/16 - 퍽 구조 변경: 하나의 퍽에서 여러 능력치 증감 + 일정 발사 수마다 나가는 특수 공격(perkType)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 11/16 - PerkType은 이제 "특수 공격 종류" (능력치만 바꾸는 퍽은 None)
public enum PerkType
{
    None,          // 특수 공격 없음 (능력치만 변경)
    Graviton,      // 중력자탄: 부딪힌 자리로 주변 적을 끌어당김
    Grenade,       // 유탄: 중력의 영향을 받아 포물선으로 날아가 폭발
    FireRing,      // 화염의 고리: 플레이어 주변을 공전하며 피해
    Explosive,     // 폭발탄: 부딪힌 자리에서 폭발
    Piercing,      // 관통탄: 적을 뚫고 지나가며 경로의 적 모두에게 피해
    Homing         // 유도탄: 가까운 적을 따라감
}

// 11/16 - 퍽으로 바꿀 수 있는 능력치
public enum StatType
{
    MoveSpeed,     // 이동 속도
    JumpForce,     // 점프력
    FireInterval,  // 발사 간격(초) - 음수면 빨라짐
    Damage,        // 데미지
    BulletCount,   // 한 번에 나가는 총알 수
    MagazineSize,  // 탄창 크기
    MaxHP,         // 최대 체력
    ReloadTime     // 재장전 시간(초) - 음수면 빨라짐
}

// 11/16 - 고정값(+10) / 퍼센트(+20% = 20) 중 선택
public enum ModifierMode
{
    Flat,
    Percent
}

// 11/16 - 능력치 하나의 증감 (값이 음수면 감소)
[System.Serializable]
public struct StatModifier
{
    public StatType stat;
    public ModifierMode mode;
    [Tooltip("Flat: 그대로 더함 / Percent: 20 = +20% (기본값 기준). 음수면 감소")]
    public float value;
}

// 11/16 - 특수 공격 수치 (특수 공격 종류에 따라 쓰는 항목만 사용됨)
[System.Serializable]
public class SpecialAttackSettings
{
    [Header("발동 주기")]
    [Tooltip("이 횟수만큼 발사할 때마다 특수 공격 1회")]
    public int shotInterval = 6;
    [Tooltip("같은 퍽을 다시 고를 때마다 줄어드는 주기")]
    public int intervalReducePerLevel = 1;
    [Tooltip("주기가 이 값보다 작아지지 않음")]
    public int minShotInterval = 2;

    [Header("피해 (플레이어 데미지 x 배율)")]
    public float damageMultiplier = 2f;
    [Tooltip("같은 퍽을 다시 고를 때마다 더해지는 배율")]
    public float damageMultiplierPerLevel = 0.5f;

    [Header("투사체")]
    public float projectileSpeed = 30f;
    public float projectileRadius = 0.3f;
    public float projectileLifetime = 4f;
    [Tooltip("비워두면 색이 있는 구체를 코드로 생성")]
    public GameObject projectilePrefab;
    public Color color = Color.white;

    [Header("범위 / 지속 (중력자탄, 유탄, 폭발탄, 화염의 고리)")]
    public float radius = 5f;
    [Tooltip("중력자탄: 끌어당기는 시간 / 화염의 고리: 유지 시간")]
    public float duration = 2f;

    [Header("중력자탄")]
    // 2026-11-23: 퍽 레벨에 따른 중력장 반경 증가량. 다른 특수탄의 범위는 변경하지 않습니다.
    [Tooltip("중력탄 레벨이 1 오를 때 추가되는 반경(월드 단위). 0이면 기존 고정 범위")]
    [Min(0f)] public float gravityRadiusPerLevel = 0.5f; // 2026-11-23: 기본 레벨당 증가량을 0.5로 조정합니다.

    // 2026-11-23: 공유 ScriptableObject를 수정하지 않고 발사 시점 레벨의 범위를 계산합니다.
    public float GetGravityRadius(int level)
    {
        return Mathf.Max(0.001f, radius + Mathf.Max(0f, gravityRadiusPerLevel) * Mathf.Max(0, level - 1));
    }

    public float pullSpeed = 8f;
    [Tooltip("끌어당기는 동안 초당 피해 = 피해 x 이 값")]
    public float pullDamagePerSecond = 0.5f;

    [Header("유탄")]
    [Tooltip("위쪽으로 더해지는 발사 각도(도)")]
    public float launchAngle = 10f;
    public float gravityScale = 1f;

    [Header("관통탄")]
    [Tooltip("0이면 무제한 관통")]
    public int maxPierceCount = 0;

    [Header("유도탄")]
    public float homingRange = 25f;
    [Tooltip("초당 회전 각도")]
    public float homingTurnSpeed = 360f;

    [Header("화염의 고리")]
    public int ringOrbCount = 3;
    public float ringOrbitSpeed = 360f; // 초당 회전 각도
    public float ringHitInterval = 0.5f; // 같은 적을 다시 때리기까지 시간
}

[CreateAssetMenu(fileName = "NewPerData", menuName = "Perk/PerkData")]
public class PerkData : ScriptableObject
{
    [Header("특수 공격 종류 (None이면 능력치만 변경)")]
    public PerkType perkType = PerkType.None;

    public string perkName; // 이름
    public string description; // 설명
    public Sprite icon;

    // 11/16 - 하나의 퍽에서 여러 능력치를 증가/감소 (기존 value 하나 -> 리스트)
    [Header("능력치 증감 (여러 개 가능, 음수 = 감소)")]
    public List<StatModifier> statModifiers = new List<StatModifier>();

    // 11/16 - perkType이 None이 아닐 때 사용
    [Header("특수 공격 설정")]
    public SpecialAttackSettings special = new SpecialAttackSettings();
}
