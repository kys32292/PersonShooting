using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour, IDamageable
{
    [Header("체력")]
    public float maxHP = 100f;
    private float currentHP = 0f;

    [Header("이동 속도")]
    [SerializeField]
    private float moveSpeed = 3f;

    private Rigidbody rb;
    private Collider col;

    // Start is called before the first frame update
    void Start()
    {
        yRot = transform.eulerAngles.y;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        mainCamera = Camera.main;
        mouseSensitivity = GameSettings.MouseSensitivity; // 2026-11-01: 타이틀 설정 화면 값 적용

        currentHP = maxHP;
        currentAmmo = magazineSize;

        RefreshHPUI();
        RefreshAmmoUI(currentAmmo.ToString()); // 2026-10-06: Main 탄환 UI와 맞춰 "현재/용량" 분리 표시로 변경
    }

    // Update is called once per frame
    void Update()
    {
        if(GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            return;
        }

        MovePlayer(InputMoveKey());
        RotatePlayer();

        FireGun();
        CountFireRate();
        Reload();
    }

    // 이동 ------------------------------------

    [Header("점프 높이")]
    [SerializeField]
    private float jumpForce = 7f;

    void MovePlayer(Vector3 dir)
    {
        float yVelocity = rb.velocity.y;

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            yVelocity = jumpForce;
        }

        // 11/15 - 보스 돌진 넉백: 이동 속도를 매 프레임 덮어쓰므로 넉백 속도를 따로 더해 주고 서서히 줄임
        knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);

        rb.velocity = new Vector3(dir.x * moveSpeed, yVelocity, dir.z * moveSpeed) + knockbackVelocity;
    }

    // 11/15 - 넉백 ----------------------------------------

    [Header("넉백 감속 (초당)")]
    [SerializeField]
    private float knockbackDecay = 30f;

    private Vector3 knockbackVelocity = Vector3.zero;

    // 수평 방향으로 밀어냄 (BossEnemy 돌진 등에서 호출)
    public void ApplyKnockback(Vector3 velocity)
    {
        velocity.y = 0f;
        knockbackVelocity = velocity;
    }

    Vector3 InputMoveKey()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDir = (transform.forward * vertical + transform.right * horizontal).normalized;

        return inputDir;
    }

    [Header("땅 레이어 설정")]
    [SerializeField]
    private LayerMask groundLayer;

    // 11/09 - 경험치 배율(점프 중 킬)에서 쓰기 위해 public으로 변경
    public bool IsGrounded()
    {
        if (col == null) return false;

        // 1. 콜라이더의 가장 밑면(발바닥) 중심점 구하기
        Vector3 bottomCenter = new Vector3(col.bounds.center.x, col.bounds.min.y + 0.05f, col.bounds.center.z);

        // 11/09 - 점프 수정: Main 씬에서 groundLayer가 Nothing으로 덮어쓰여 항상 공중 판정이었음
        // groundLayer가 비어 있으면 모든 레이어를 바닥으로 보고, 트리거(총알 등)와 플레이어 자신은 제외
        int mask = groundLayer.value != 0 ? groundLayer.value : Physics.DefaultRaycastLayers;

        // 2. 발바닥에서 아래로 0.1m만 Ray를 쏴서 바닥 레이어와 닿았는지 확인
        RaycastHit[] hits = Physics.RaycastAll(bottomCenter, Vector3.down, 0.1f, mask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (!hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    // 11/09 - 경험치 배율: 점프(공중) 상태 여부
    public bool IsAirborne => !IsGrounded();

    // 좌우 회전 ----------------------------------

    [Header("마우스 감도")]
    public float mouseSensitivity = 3f;

    float xRot;
    float yRot;

    [Header("플레이어의 팔")]
    [SerializeField]
    private Transform playerArm;

    [Header("상, 하 제한 각도")]
    public float minAngle = -90f;
    public float maxAngle = 90f;

    void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, minAngle, maxAngle);

        playerArm.localRotation = Quaternion.Euler(xRot, 0, 0);

        yRot += mouseX;
        transform.rotation = Quaternion.Euler(0, yRot, 0);

        TrackSpin(mouseX); // 11/09 - 360도 회전 킬 판정용 회전량 기록
    }

    // 11/09 - 360도 회전 판정 -------------------------------

    [Header("360도 회전 판정")]
    [SerializeField]
    private float spinWindow = 0.5f; // 이 시간 안에
    [SerializeField]
    private float spinAngle = 330f; // 이 각도 이상 한 방향으로 돌면 회전으로 인정
    [SerializeField]
    private float spinGrace = 0.3f; // 회전을 끝낸 뒤 이 시간 안에 적이 죽어도 인정 (총알 비행 시간 고려)

    private readonly Queue<Vector2> spinSamples = new Queue<Vector2>(); // x = 기록 시간, y = 회전량(부호 포함)
    private float spinSum = 0f;
    private float lastSpinTime = -999f;

    private void TrackSpin(float deltaYaw)
    {
        float now = Time.time;

        spinSamples.Enqueue(new Vector2(now, deltaYaw));
        spinSum += deltaYaw;

        // 오래된 기록 제거
        while (spinSamples.Count > 0 && now - spinSamples.Peek().x > spinWindow)
        {
            spinSum -= spinSamples.Dequeue().y;
        }

        // 부호를 포함해 합산하므로 좌우로 흔드는 건 상쇄되고, 한 방향으로 돈 경우만 인정
        if (Mathf.Abs(spinSum) >= spinAngle)
        {
            lastSpinTime = now;
            spinSamples.Clear();
            spinSum = 0f;
        }
    }

    public bool RecentlySpun => Time.time - lastSpinTime <= spinGrace;

    // 총 발사 ----------------------------------------

    private Camera mainCamera;

    [Header("사거리")]
    [SerializeField]
    private float Range = 100f;

    [Header("데미지")]
    [SerializeField]
    private int damage = 10;

    [Header("발사 간격")]
    [SerializeField]
    private float fireRate = 0.2f;
    private float fireRateTime = 0f;

    [Header("탄창 크기")]
    [SerializeField]
    private int magazineSize = 50;
    private int currentAmmo = 0;

    [Header("장전 시간")]
    [SerializeField]
    private float reload = 2f;
    private float reloadTime = 0f;

    [Header("감지 레이어")]
    private LayerMask hitLayer;

    [Header("총알 오브젝트")]
    [SerializeField]
    private GameObject bulletPrefab;

    [Header("총알 발사 위치")]
    [SerializeField]
    Transform MuzzleTrans;

    [Header("총알 발사 수")]
    [SerializeField]
    private int bulletCount = 1;

    [SerializeField]
    private float spreadAngle = 60f;

    private bool canFire = true;

    private bool isReloading = false;

    void FireGun()
    {
        if (Input.GetButton("Fire1") && canFire && currentAmmo > 0)
        {
            currentAmmo--;

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1f));
            Vector3 targetPoint;

            if(Physics.Raycast(ray, out RaycastHit hit, 1000f)) // 1000의 거리에 레이케스트 발사
            {
                targetPoint = hit.point; // 부딫힐 시 그곳을 타깃으로
            }
            else
            {
                targetPoint = ray.GetPoint(Range); // 부딫히지 않을 시 최대 거리를 타깃으로
            }

            Vector3 fireDir = (targetPoint - MuzzleTrans.position).normalized; // 발사 방향

            /*
            Quaternion fireRot = Quaternion.LookRotation(fireDir);

            GameObject bullet = Instantiate(bulletPrefab, MuzzleTrans.position, fireRot);
            */

            for (int i = 0; i < bulletCount; i++) // 산탄
            {
                float angleOffset = 0f; // 기본 0

                if (bulletCount > 1) // 총알이 추가 될 경우 오프셋 값을 추가
                {
                    float step = spreadAngle / (bulletCount - 1);
                    angleOffset = -(spreadAngle / 2f) + (step * i);
                }

                Vector3 finalDir = Quaternion.Euler(0f, angleOffset, 0f) * fireDir;
                Quaternion fireRot = Quaternion.LookRotation(finalDir);

                GameObject bullet = Instantiate(bulletPrefab, MuzzleTrans.position, fireRot);

                // 2026-10-06: 총알에 발사자(owner)를 표시해서 자기 자신과는 충돌하지 않도록 함
                // (bulletCount > 1일 때 같은 지점에서 동시에 나간 총알끼리 서로 부딪히는 문제는 Bullet.cs에서 별도 처리)
                Bullet bulletComp = bullet.GetComponent<Bullet>();
                if (bulletComp != null)
                {
                    bulletComp.owner = gameObject;
                    bulletComp.bulletDamage = damage; // 11/09 - 퍽 수치와 데미지 연결 (DamageUp 퍽이 실제 피해량에 반영됨)
                }
            }

            // 11/16 - 특수 공격 퍽 발사 카운트 (방아쇠 1회 = 1카운트, 산탄도 1회)
            if (specialAttack != null)
            {
                specialAttack.OnShotFired(MuzzleTrans.position, fireDir, damage);
            }

            canFire = false;

            RefreshAmmoUI(currentAmmo.ToString()); // 2026-10-06: Main 탄환 UI와 맞춰 "현재/용량" 분리 표시로 변경
        }
    }

    private void Reload()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading || currentAmmo <= 0)
        {
            canFire = false;
            isReloading = true;
        }

        if (isReloading)
        {
            RefreshAmmoUI("Reloading...");

            if(Timer(ref reloadTime, reload))
            {
                currentAmmo = magazineSize;
                isReloading = false;
                canFire = true;

                RefreshAmmoUI(currentAmmo.ToString()); // 2026-10-06: Main 탄환 UI와 맞춰 "현재/용량" 분리 표시로 변경
            }
        }
    }

    void CountFireRate()
    {
        if (!canFire && !isReloading)
        {
            if(Timer(ref fireRateTime, fireRate))
            {
                canFire = true;
            }
        }
    }

    // 특성 적용 ------------------------------------------------
    // 11/16 - 퍽 구조 변경: 얻은 퍽들의 능력치 증감을 모아 "기본값"에서 다시 계산
    // 최종값 = (기본값 + 고정값 합) x (1 + 퍼센트 합 / 100), 능력치별 최소/최대 제한 적용

    private readonly List<PerkData> acquiredPerks = new List<PerkData>();
    private SpecialAttackController specialAttack;

    private bool baseStatsSaved = false;
    private float baseMoveSpeed;
    private float baseJumpForce;
    private float baseFireRate;
    private float baseReload;
    private float baseMaxHP;
    private int baseDamage;
    private int baseBulletCount;
    private int baseMagazineSize;

    [Header("능력치 제한")]
    [SerializeField] private float maxJumpForce = 15f;
    [SerializeField] private float minFireInterval = 0.05f;
    [SerializeField] private float minReloadTime = 0.1f;
    [SerializeField] private float minMoveSpeed = 0.5f;

    private void SaveBaseStats()
    {
        if (baseStatsSaved) return;
        baseStatsSaved = true;

        baseMoveSpeed = moveSpeed;
        baseJumpForce = jumpForce;
        baseFireRate = fireRate;
        baseReload = reload;
        baseMaxHP = maxHP;
        baseDamage = damage;
        baseBulletCount = bulletCount;
        baseMagazineSize = magazineSize;
    }

    public void ApplyPerk(PerkData perk)
    {
        if (perk == null) return;

        SaveBaseStats();
        acquiredPerks.Add(perk);
        RecalculateStats();

        // 11/16 - 특수 공격 퍽이면 등록 (같은 퍽을 다시 고르면 강화)
        if (perk.perkType != PerkType.None)
        {
            if (specialAttack == null)
            {
                specialAttack = GetComponent<SpecialAttackController>();
                if (specialAttack == null)
                {
                    specialAttack = gameObject.AddComponent<SpecialAttackController>();
                }
            }
            specialAttack.AddOrUpgrade(perk);
        }
    }

    private float GetStat(StatType stat, float baseValue)
    {
        float flat = 0f;
        float percent = 0f;

        foreach (PerkData perk in acquiredPerks)
        {
            if (perk.statModifiers == null) continue;

            foreach (StatModifier mod in perk.statModifiers)
            {
                if (mod.stat != stat) continue;

                if (mod.mode == ModifierMode.Flat) flat += mod.value;
                else percent += mod.value;
            }
        }

        return (baseValue + flat) * (1f + percent / 100f);
    }

    private void RecalculateStats()
    {
        moveSpeed = Mathf.Max(minMoveSpeed, GetStat(StatType.MoveSpeed, baseMoveSpeed));
        jumpForce = Mathf.Clamp(GetStat(StatType.JumpForce, baseJumpForce), 0f, maxJumpForce);
        fireRate = Mathf.Max(minFireInterval, GetStat(StatType.FireInterval, baseFireRate));
        reload = Mathf.Max(minReloadTime, GetStat(StatType.ReloadTime, baseReload));
        damage = Mathf.Max(1, Mathf.RoundToInt(GetStat(StatType.Damage, baseDamage)));
        bulletCount = Mathf.Max(1, Mathf.RoundToInt(GetStat(StatType.BulletCount, baseBulletCount)));

        // 탄창: 늘어난 만큼 현재 탄도 채워주고, 줄어들면 넘치는 탄은 버림
        int newMagazine = Mathf.Max(1, Mathf.RoundToInt(GetStat(StatType.MagazineSize, baseMagazineSize)));
        if (newMagazine != magazineSize)
        {
            int diff = newMagazine - magazineSize;
            magazineSize = newMagazine;
            currentAmmo = Mathf.Clamp(currentAmmo + Mathf.Max(diff, 0), 0, magazineSize);
            RefreshAmmoUI(isReloading ? "Reloading..." : currentAmmo.ToString());
        }

        // 최대 체력: 늘어난 만큼 현재 체력도 회복, 줄어들면 최대 체력에 맞춰 자름
        float newMaxHP = Mathf.Max(1f, GetStat(StatType.MaxHP, baseMaxHP));
        if (!Mathf.Approximately(newMaxHP, maxHP))
        {
            float diff = newMaxHP - maxHP;
            maxHP = newMaxHP;
            currentHP = Mathf.Clamp(currentHP + Mathf.Max(diff, 0f), 0f, maxHP);
            RefreshHPUI();
        }
    }

    // UI -------------------------------------------------------

    [Header("HP 바")]
    [SerializeField]
    private Image hpBar;

    [SerializeField]
    private TMP_Text hpText; // 2026-10-06: 체력 숫자 표시용 텍스트 추가 (HealthValue UI와 연결)

    [Header("총알 Text")]
    [SerializeField]
    private TMP_Text ammoText;

    [SerializeField]
    private TMP_Text ammoMaxText; // 2026-10-06: Main 씬 기존 탄환 UI(AmmoReserve 재사용)의 탄창 용량 표시용, 없으면 무시됨

    private void RefreshHPUI()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHP / maxHP;
        }

        if (hpText != null) // 2026-10-06
        {
            hpText.text = Mathf.CeilToInt(currentHP).ToString();
        }
    }

    private void RefreshAmmoUI(string printText)
    {
        if(ammoText != null)
        {
            ammoText.text = printText;
        }

        // 2026-10-06: Main 씬의 기존 탄환 UI(현재/탄창용량 분리 표시)와 호환
        if (ammoMaxText != null)
        {
            ammoMaxText.text = magazineSize.ToString();
        }
    }

    // 데미지, 게임 오버 처리 ----------------------------------------

    public bool IsDead => currentHP <= 0;

    public void TakeDamage(float _damage)
    {
        if(IsDead) // 이미 죽었을 경우 중복 작동을 방지하기 위해
        {
            return;
        }

        currentHP -= _damage;
        currentHP = Mathf.Max(currentHP, 0); // currentHP 값이 음수로 내려가지 않게 하기 위함

        RefreshHPUI();

        if (IsDead) // 데미지를 받은 뒤 죽었을 경우 Die를 호출
        {
            Die();
        }
    }

    private void Die()
    {
        GameManager.Instance.GameOver();
    }


    // 시간 계산 -----------------------------------------

    private bool Timer(ref float timer, float time)
    {
        timer += Time.deltaTime;

        if (timer >= time)
        {
            timer = 0;

            return true;
        }

        return false;
    }
}
