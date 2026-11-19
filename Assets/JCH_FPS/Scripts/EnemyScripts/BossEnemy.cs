// 11/15 - 보스 몬스터
// 등장: GameManager가 처치 수/경과 시간 조건을 확인해 EnemyManager.SpawnBoss()로 한 번 소환
// 상태: 등장(Intro) -> 추적(Chase) -> 패턴 선택 -> [장판(Casting) | 돌진(DashWindup -> Dashing) | 투사체(ProjectileWindup)] -> 회복/경직(Recover) -> 추적
// 패턴 1 장판: 플레이어 위치 + 이동 예측 위치 + 주변에 BossHazardZone 3개 (2페이즈: 5개씩 2번 연속, 경고 시간 단축)
//             반구 경고 후 분출하는 순간부터 사라질 때까지 닿으면 즉발 데미지
// 패턴 2 돌진: 예열 동안 바닥에 경로 경고 -> 고정된 방향으로 돌진 (2페이즈: 3연속)
//             벽에 박히면 긴 경직(반격 타이밍), 아니면 짧은 경직. 2페이즈는 마지막 돌진 직후 투사체 연계
// 패턴 3 투사체: 예열 동안 빛 구체 + 바닥 사선 경고 -> 부채꼴 직선 투사체 (비유도, 2페이즈: 5발 x 2번)
// 11/19 - QA 피드백: 이동 속도/돌진 속도·거리 상향, 패턴 후딜레이 대폭 감소, 원거리 패턴 추가
// 2페이즈(HP 50% 이하): 몸이 붉게 변하고 이동/패턴 간격이 빨라짐
// 처치: 경험치(처치 배율 적용) + 특성 선택 1회 확정 + 엔딩 기록(처치 수). 게임은 계속 진행되고 조건을 다시 채우면 다음 보스 등장
// 프리팹/애니메이터/UI는 Tools/Boss/Build Boss (Main) 메뉴로 생성
// EnemyBase는 Update에서 상태/공격을 직접 처리하고 필드가 private이라 상속하지 않고 따로 구현함
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BossEnemy : MonoBehaviour, IDamageable
{
    private enum BossState
    {
        Intro,
        Chase,
        Casting,
        DashWindup,
        Dashing,
        ProjectileWindup,
        Recover,
        Dead
    }

    [Header("=== 기본 정보 ===")]
    [SerializeField] private string bossName = "BEHEMOTH";
    public float maxHP = 1500f;
    [SerializeField] private float currentHP = 0f;
    [SerializeField] private float exp = 1000f;
    [SerializeField] private float moveSpeed = 6.5f;        // 11/19 - 3.5 -> 6.5 (플레이어 이동 속도 10보다는 느리게)
    [SerializeField] private float introTime = 1.2f;        // 등장 후 첫 행동까지 대기 (11/19 - 2 -> 1.2)

    [Header("=== 패턴 공통 ===")]
    [SerializeField] private float patternCooldown = 1.2f;  // 패턴 사이 추적 시간 (11/19 - 2.5 -> 1.2)
    [SerializeField] private float recoverTime = 0.15f;     // 장판/투사체 후 쉬는 시간 (11/19 - 0.7 -> 0.15)
    [SerializeField] private float nearDistance = 10f;      // 이 거리 이상이면 '먼 거리' 확률 사용

    // 11/19 - 패턴 선택 가중치 (합이 1이 아니어도 비율로 계산)
    [Header("=== 패턴 확률: 먼 거리 ===")]
    [SerializeField] private float farProjectileWeight = 0.45f;
    [SerializeField] private float farDashWeight = 0.40f;
    [SerializeField] private float farZoneWeight = 0.15f;
    [Header("=== 패턴 확률: 가까운 거리 ===")]
    [SerializeField] private float nearProjectileWeight = 0.15f;
    [SerializeField] private float nearDashWeight = 0.25f;
    [SerializeField] private float nearZoneWeight = 0.60f;

    [Header("=== 패턴 1: 장판 ===")]
    [SerializeField] private float castTime = 0.6f;         // 11/19 - 1.0 -> 0.6
    [SerializeField] private float zoneRadius = 3f;
    [SerializeField] private float zoneWarningTime = 1f;    // 11/15 - 1.5 -> 1.0
    [SerializeField] private float zoneDamage = 20f;        // 11/19 - 지속 틱 데미지 -> 분출 즉발 데미지 (분출~사라질 때까지 닿으면 1회)
    // 11/15 - 난이도 상향: 한 번에 여러 개 (플레이어 위치 + 이동 예측 위치 + 주변)
    [SerializeField] private int zoneCount = 3;
    [SerializeField] private float zoneSpreadMin = 3f;      // 주변 장판의 플레이어로부터 거리
    [SerializeField] private float zoneSpreadMax = 6f;
    [SerializeField] private float zonePredictTime = 0.8f;  // 이동 중이면 이 시간 뒤 도착할 위치에도 하나 깔림
    [SerializeField] private int zoneWaves = 1;             // 시전 1번에 깔리는 횟수
    [SerializeField] private float zoneWaveInterval = 0.7f; // 연속으로 깔릴 때 간격

    [Header("=== 패턴 2: 돌진 ===")]
    [SerializeField] private float dashWindupTime = 0.6f;   // 11/19 - 0.8 -> 0.6
    [Range(0f, 1f)] [SerializeField] private float dashTrackRatio = 0.6f; // 예열 앞부분은 플레이어를 따라 회전, 이후 방향 고정
    [SerializeField] private float dashSpeed = 30f;         // 11/19 - 18 -> 30
    [SerializeField] private float dashDistance = 24f;      // 11/19 - 15 -> 24
    [SerializeField] private float dashDamage = 25f;
    [SerializeField] private float dashHitRadius = 1.8f;
    [SerializeField] private float dashKnockback = 14f;
    [SerializeField] private float dashStunTime = 0.35f;    // 돌진 후 경직 (11/19 - 1.2 -> 0.35)
    [SerializeField] private float dashWallStunTime = 1.5f; // 11/19 - 벽에 박혔을 때 경직 (반격 타이밍, 연속 돌진도 여기서 끊김)

    // 11/19 - 패턴 3: 직선 투사체 (비유도)
    [Header("=== 패턴 3: 투사체 ===")]
    [SerializeField] private float projectileWindupTime = 0.4f;  // 예열 (방향은 예열 시작 시점에 고정)
    [SerializeField] private int projectileCount = 3;            // 부채꼴 발 수
    [SerializeField] private float projectileSpread = 12f;       // 발 사이 각도
    [SerializeField] private float projectileSpeed = 35f;
    [SerializeField] private float projectileRadius = 0.5f;
    [SerializeField] private float projectileDamage = 15f;
    [SerializeField] private float projectileLifetime = 2f;
    [SerializeField] private float projectileFireHeight = 2.5f;  // 발사 위치 (보스 발밑 기준 높이)
    [SerializeField] private float projectileFireForward = 1.6f; // 발사 위치 (보스 앞쪽 거리)

    [Header("=== 2페이즈 (HP 비율 이하) ===")]
    [Range(0f, 1f)] [SerializeField] private float phase2HPRatio = 0.5f;
    [SerializeField] private float phase2SpeedMultiplier = 1.2f;
    [SerializeField] private float phase2Cooldown = 0.7f;          // 11/19 - 1.5 -> 0.7
    [SerializeField] private int phase2ZoneCount = 5;              // 11/15 - 3 -> 5
    [SerializeField] private float phase2ZoneWarningTime = 0.75f;  // 11/15 - 추가
    [SerializeField] private int phase2ZoneWaves = 2;              // 11/15 - 추가: 0.7초 간격으로 2번 연속 (두 번째는 그때의 플레이어 위치 기준)
    [SerializeField] private int phase2DashCount = 3;              // 11/15 - 2 -> 3
    [SerializeField] private float phase2SecondWindupTime = 0.45f; // 11/15 - 0.6 -> 0.45
    [SerializeField] private int phase2ProjectileCount = 5;        // 11/19 - 투사체 부채꼴 발 수
    [SerializeField] private int phase2ProjectileVolleys = 2;      // 11/19 - 연속 발사 횟수 (두 번째는 틈을 메우도록 반 칸 엇갈림)
    [SerializeField] private float phase2VolleyInterval = 0.25f;
    [SerializeField] private int phase2ComboProjectileCount = 3;   // 11/19 - 마지막 돌진 직후 연계 투사체
    [SerializeField] private float phase2ComboWindupTime = 0.25f;
    [SerializeField] private Color phase2Tint = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color phase2Emission = new Color(0.35f, 0.02f, 0.02f, 1f);

    [Header("=== 피격 연출 ===")]
    [SerializeField] private Color hitFlashColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private float hitFlashTime = 0.08f;

    [Header("=== 연결 ===")]
    [SerializeField] private Animator animator;

    // 애니메이터 파라미터 (Tools/Boss 메뉴가 만드는 Boss_Anim.controller와 같은 이름)
    private const string AnimIdle = "Idle";
    private const string AnimRun = "Run";
    private const string AnimCast = "Cast";
    private const string AnimWindup = "Windup";
    private const string AnimDash = "Dash";
    private const string AnimStun = "Stun";
    private const string AnimIsDead = "IsDead";

    private BossState state = BossState.Intro;
    private float stateTimer = 0f;
    private float cooldownTimer = 0f;
    private float damageMultiplier = 1f;
    private bool isPhase2 = false;
    private bool wasPaused = false;

    private NavMeshAgent agent;
    private Transform player;
    private IDamageable playerDamageable;
    private PlayerMove playerMove;
    private Rigidbody playerBody;   // 장판 이동 예측용

    // 장판 연속 시전
    private int zoneWavesRemaining;
    private float zoneWaveTimer;

    // 돌진
    private float currentWindupTime;
    private Vector3 dashDirection;
    private float dashMaxDistance;
    private float dashTraveled;
    private bool dashHitPlayer;
    private int dashesRemaining;
    private Transform dashTelegraph;
    private Renderer dashTelegraphRenderer;
    private bool dashBlocked;

    // 투사체
    private float projectileWindup;
    private int projectileShotCount;
    private int volleysRemaining;
    private int volleysFired;
    private float volleyTimer;
    private bool projectileFiring;
    private Vector3 projectileAim;
    private Transform projectileTelegraph;
    private Transform projectileOrb;
    private Renderer projectileOrbRenderer;
    private readonly List<Renderer> projectileLineRenderers = new List<Renderer>();

    // 피격 연출
    private Renderer[] bodyRenderers;
    private readonly List<Color> baseColors = new List<Color>();
    private MaterialPropertyBlock propertyBlock;
    private float flashTimer = 0f;
    private bool visualDirty = true;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    public bool IsDead => state == BossState.Dead;

    // 11/15 - 몇 번째 보스인지 (HP 바 이름에 "LV.N"으로 표시). Instantiate 직후(Start 전)에 호출
    private int round = 1;

    public void SetRound(int _round)
    {
        round = Mathf.Max(1, _round);
    }

    // 무한 웨이브 강화: Instantiate 직후(Start 전)에 호출
    public void ApplyStatMultiplier(float hpMultiplier, float _damageMultiplier)
    {
        maxHP *= hpMultiplier;
        damageMultiplier = _damageMultiplier;
    }

    private void Start()
    {
        currentHP = maxHP;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerDamageable = playerObject.GetComponent<IDamageable>();
            playerMove = playerObject.GetComponent<PlayerMove>();
            playerBody = playerObject.GetComponent<Rigidbody>();
        }

        bodyRenderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        foreach (Renderer bodyRenderer in bodyRenderers)
        {
            Material material = bodyRenderer.sharedMaterial;
            baseColors.Add(material != null && material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white);
        }

        if (BossUI.Instance != null)
        {
            BossUI.Instance.Show(bossName + "  LV." + round);
            BossUI.Instance.SetHP(currentHP, maxHP);
        }

        CameraShake.Shake(0.15f, 0.6f);

        EnterIntro();
    }

    private void Update()
    {
        if (state == BossState.Dead)
        {
            return;
        }

        // 일시정지(ESC/특성 선택) 중에는 이동/애니메이션/패턴 시간 모두 정지
        bool paused = GameManager.Instance != null && GameManager.Instance.isPaused;
        if (paused)
        {
            if (!wasPaused)
            {
                wasPaused = true;
                if (agent.isOnNavMesh) agent.isStopped = true;
                if (animator != null) animator.speed = 0f;
            }
            return;
        }
        if (wasPaused)
        {
            wasPaused = false;
            if (animator != null) animator.speed = 1f;
            if (state == BossState.Chase && agent.isOnNavMesh) agent.isStopped = false;
        }

        if (player == null)
        {
            return;
        }

        float dt = Time.deltaTime;

        switch (state)
        {
            case BossState.Intro: UpdateIntro(dt); break;
            case BossState.Chase: UpdateChase(dt); break;
            case BossState.Casting: UpdateCasting(dt); break;
            case BossState.DashWindup: UpdateDashWindup(dt); break;
            case BossState.Dashing: UpdateDashing(dt); break;
            case BossState.ProjectileWindup: UpdateProjectile(dt); break;
            case BossState.Recover: UpdateRecover(dt); break;
        }

        UpdateBodyVisual(dt);
    }

    // 등장 ----------------------------------------------------------

    private void EnterIntro()
    {
        state = BossState.Intro;
        stateTimer = introTime;
        StopMoving();
        SetTrigger(AnimCast); // 포효 느낌으로 시전 모션 사용
    }

    private void UpdateIntro(float dt)
    {
        FacePlayer(dt, 180f);

        stateTimer -= dt;
        if (stateTimer <= 0f)
        {
            EnterChase(1.5f);
        }
    }

    // 추적 ----------------------------------------------------------

    private void EnterChase(float cooldown)
    {
        state = BossState.Chase;
        cooldownTimer = cooldown;

        if (agent.isOnNavMesh) agent.isStopped = false;
        SetTrigger(AnimRun);
    }

    private void UpdateChase(float dt)
    {
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
        }

        cooldownTimer -= dt;
        if (cooldownTimer <= 0f)
        {
            ChoosePattern();
        }
    }

    private void ChoosePattern()
    {
        float distance = FlatDistance(transform.position, player.position);
        bool far = distance >= nearDistance;

        float projectileWeight = Mathf.Max(0f, far ? farProjectileWeight : nearProjectileWeight);
        float dashWeight = Mathf.Max(0f, far ? farDashWeight : nearDashWeight);
        float zoneWeight = Mathf.Max(0f, far ? farZoneWeight : nearZoneWeight);
        float total = projectileWeight + dashWeight + zoneWeight;
        float roll = Random.value * (total > 0f ? total : 1f);

        if (roll < projectileWeight)
        {
            EnterProjectileWindup(projectileWindupTime,
                isPhase2 ? phase2ProjectileCount : projectileCount,
                isPhase2 ? phase2ProjectileVolleys : 1);
        }
        else if (roll < projectileWeight + dashWeight)
        {
            dashesRemaining = isPhase2 ? Mathf.Max(1, phase2DashCount) : 1;
            EnterDashWindup(dashWindupTime);
        }
        else
        {
            EnterCasting();
        }
    }

    // 패턴 1: 장판 ----------------------------------------------------

    private void EnterCasting()
    {
        state = BossState.Casting;
        StopMoving();
        SetTrigger(AnimCast);

        zoneWavesRemaining = Mathf.Max(1, isPhase2 ? phase2ZoneWaves : zoneWaves);
        zoneWaveTimer = 0f; // 첫 웨이브는 바로
        stateTimer = castTime + zoneWaveInterval * (zoneWavesRemaining - 1); // 마지막 웨이브까지 시전 유지
    }

    private void UpdateCasting(float dt)
    {
        FacePlayer(dt, 360f);

        zoneWaveTimer -= dt;
        if (zoneWavesRemaining > 0 && zoneWaveTimer <= 0f)
        {
            SpawnZones();
            zoneWavesRemaining--;
            zoneWaveTimer = zoneWaveInterval;
        }

        stateTimer -= dt;
        if (stateTimer <= 0f)
        {
            EnterRecover(recoverTime, false);
        }
    }

    private void SpawnZones()
    {
        float hitDamage = zoneDamage * damageMultiplier;
        float warning = isPhase2 ? phase2ZoneWarningTime : zoneWarningTime;
        int count = Mathf.Max(1, isPhase2 ? phase2ZoneCount : zoneCount);
        int spawned = 0;

        // 1. 시전 순간의 플레이어 위치
        SpawnZoneAt(player.position, hitDamage, warning);
        spawned++;

        // 2. 이동 중이면 예측 위치 (가만히 있어도, 한 방향으로 계속 달려도 피하기 어렵게)
        if (spawned < count && playerBody != null)
        {
            Vector3 velocity = playerBody.velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 1f)
            {
                Vector3 predicted = player.position + velocity * zonePredictTime;
                if (FlatDistance(predicted, player.position) > zoneRadius) // 첫 장판과 거의 겹치면 생략
                {
                    SpawnZoneAt(predicted, hitDamage, warning);
                    spawned++;
                }
            }
        }

        // 3. 나머지는 플레이어 주변에 고르게 흩뿌림
        int extra = count - spawned;
        float startAngle = Random.Range(0f, 360f);
        for (int i = 0; i < extra; i++)
        {
            float angle = (startAngle + 360f * i / extra + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
            float distance = Random.Range(zoneSpreadMin, zoneSpreadMax);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            SpawnZoneAt(player.position + offset, hitDamage, warning);
        }
    }

    private void SpawnZoneAt(Vector3 position, float hitDamage, float warningTime)
    {
        // NavMesh 위의 가장 가까운 지점 = 바닥 높이 (플레이어 피벗 높이, 점프 여부와 무관하게 바닥에 깔림)
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            position = hit.position;
        }

        BossHazardZone.Spawn(position, zoneRadius, warningTime, hitDamage, player);
    }

    // 패턴 2: 돌진 ----------------------------------------------------

    private void EnterDashWindup(float windupTime)
    {
        state = BossState.DashWindup;
        stateTimer = 0f;
        currentWindupTime = windupTime;
        StopMoving();
        SetTrigger(AnimWindup);

        dashDirection = FlatDirection(transform.position, player.position);
        CreateDashTelegraph();
    }

    private void UpdateDashWindup(float dt)
    {
        stateTimer += dt;
        bool tracking = stateTimer < currentWindupTime * dashTrackRatio;

        if (tracking)
        {
            FacePlayer(dt, 360f);
            dashDirection = transform.forward;
            dashDirection.y = 0f;
            dashDirection.Normalize();
        }

        UpdateDashTelegraph(tracking);

        if (stateTimer >= currentWindupTime)
        {
            EnterDashing();
        }
    }

    private void EnterDashing()
    {
        state = BossState.Dashing;
        dashTraveled = 0f;
        dashHitPlayer = false;
        dashMaxDistance = GetDashLength();
        dashBlocked = dashMaxDistance < dashDistance - 0.01f; // 끝에 벽/맵 끝이 있음
        DestroyDashTelegraph();
        SetTrigger(AnimDash);

        transform.rotation = Quaternion.LookRotation(dashDirection);
    }

    private void UpdateDashing(float dt)
    {
        float step = dashSpeed * dt;
        Vector3 before = transform.position;

        // agent.Move는 NavMesh 경계를 넘지 않으므로 벽/맵 끝에서 자연스럽게 막힘
        if (agent.isOnNavMesh)
        {
            agent.Move(dashDirection * step);
        }

        float moved = FlatDistance(before, transform.position);
        dashTraveled += step;

        CheckDashHit();

        // 돌진 거리는 벽까지로 잘려 있으므로(GetDashLength), 벽 앞에서 끝났거나 이동이 막히면 벽에 박힌 것
        bool blocked = moved < step * 0.3f || (dashBlocked && dashTraveled >= dashMaxDistance);
        if (dashTraveled >= dashMaxDistance || blocked)
        {
            if (blocked)
            {
                // 11/19 - 벽꿍: 연속 돌진도 끊고 긴 경직 (돌진을 벽으로 유도하는 플레이 보상)
                CameraShake.Shake(0.2f, 0.35f);
                EnterRecover(dashWallStunTime, true);
                return;
            }

            dashesRemaining--;
            if (dashesRemaining > 0)
            {
                EnterDashWindup(phase2SecondWindupTime);
            }
            else if (isPhase2)
            {
                // 11/19 - 2페이즈 연계: 마지막 돌진 직후 착지 지점에서 바로 투사체
                EnterProjectileWindup(phase2ComboWindupTime, phase2ComboProjectileCount, 1);
            }
            else
            {
                EnterRecover(dashStunTime, true);
            }
        }
    }

    private void CheckDashHit()
    {
        if (dashHitPlayer || playerDamageable == null)
        {
            return;
        }

        Vector3 offset = player.position - transform.position;
        if (Mathf.Abs(offset.y) > 2.5f)
        {
            return;
        }
        offset.y = 0f;

        if (offset.sqrMagnitude > dashHitRadius * dashHitRadius)
        {
            return;
        }

        dashHitPlayer = true;
        playerDamageable.TakeDamage(dashDamage * damageMultiplier);

        // 돌진 방향 + 옆으로 비켜나는 방향으로 밀어냄
        if (playerMove != null)
        {
            Vector3 side = Vector3.Project(offset, Vector3.Cross(Vector3.up, dashDirection));
            Vector3 push = (dashDirection + side.normalized * 0.6f).normalized * dashKnockback;
            playerMove.ApplyKnockback(push);
        }

        CameraShake.Shake(0.35f, 0.35f);
    }

    // 벽/맵 끝까지의 거리만큼만 돌진 (경고 표시 길이와 실제 돌진 거리를 일치시킴)
    private float GetDashLength()
    {
        Vector3 start = transform.position;
        Vector3 end = start + dashDirection * dashDistance;

        if (NavMesh.Raycast(start, end, out NavMeshHit hit, NavMesh.AllAreas))
        {
            return Mathf.Max(0.5f, hit.distance);
        }

        return dashDistance;
    }

    private void CreateDashTelegraph()
    {
        DestroyDashTelegraph();

        var root = new GameObject("Boss Dash Telegraph").transform;
        BossVisuals.CreateStrip("Strip", root, dashHitRadius * 2f, 1f, out dashTelegraphRenderer);
        dashTelegraph = root;
    }

    private void UpdateDashTelegraph(bool tracking)
    {
        if (dashTelegraph == null) return;

        dashTelegraph.position = transform.position + Vector3.up * 0.05f;
        dashTelegraph.rotation = Quaternion.LookRotation(dashDirection);
        dashTelegraph.localScale = new Vector3(1f, 1f, GetDashLength());

        // 방향이 고정되면 더 빠르고 진하게 점멸
        float frequency = tracking ? 5f : 12f;
        float blink = 0.5f + 0.5f * Mathf.Sin(stateTimer * frequency * Mathf.PI * 2f);
        float alpha = tracking ? Mathf.Lerp(0.1f, 0.3f, blink) : Mathf.Lerp(0.25f, 0.55f, blink);
        BossVisuals.SetColor(dashTelegraphRenderer, new Color(1f, 0.25f, 0.15f, 1f), alpha);
    }

    private void DestroyDashTelegraph()
    {
        if (dashTelegraph != null)
        {
            Destroy(dashTelegraph.gameObject);
            dashTelegraph = null;
            dashTelegraphRenderer = null;
        }
    }

    // 패턴 3: 투사체 ---------------------------------------------------

    private void EnterProjectileWindup(float windupTime, int shotCount, int volleys)
    {
        state = BossState.ProjectileWindup;
        stateTimer = 0f;
        projectileWindup = windupTime;
        projectileShotCount = Mathf.Max(1, shotCount);
        volleysRemaining = Mathf.Max(1, volleys);
        volleysFired = 0;
        projectileFiring = false;
        StopMoving();
        SetTrigger(AnimCast);

        // 방향은 예열 시작 시점에 고정 (비유도)
        Vector3 flat = FlatDirection(transform.position, player.position);
        transform.rotation = Quaternion.LookRotation(flat);
        projectileAim = (GetPlayerAimPoint() - GetFireOrigin()).normalized;

        CreateProjectileTelegraph();
    }

    private void UpdateProjectile(float dt)
    {
        stateTimer += dt;

        if (!projectileFiring)
        {
            UpdateProjectileTelegraph(Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, projectileWindup)));

            if (stateTimer >= projectileWindup)
            {
                DestroyProjectileTelegraph();
                projectileFiring = true;
                volleyTimer = 0f;
            }
            return;
        }

        volleyTimer -= dt;
        if (volleyTimer <= 0f && volleysRemaining > 0)
        {
            // 두 번째 발사는 반 칸 엇갈려서 첫 발사의 틈을 메움
            float offset = (volleysFired % 2 == 1) ? projectileSpread * 0.5f : 0f;
            FireVolley(offset);
            volleysFired++;
            volleysRemaining--;
            volleyTimer = phase2VolleyInterval;
        }

        if (volleysRemaining <= 0)
        {
            EnterRecover(recoverTime, false);
        }
    }

    private void FireVolley(float angleOffset)
    {
        Vector3 origin = GetFireOrigin();
        float damage = projectileDamage * damageMultiplier;

        for (int i = 0; i < projectileShotCount; i++)
        {
            float angle = (i - (projectileShotCount - 1) * 0.5f) * projectileSpread + angleOffset;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * projectileAim;
            BossProjectile.Spawn(origin, direction, projectileSpeed, projectileRadius, damage, projectileLifetime, player, gameObject);
        }

        CameraShake.Shake(0.05f, 0.12f);
    }

    private Vector3 GetFireOrigin()
    {
        return transform.position + Vector3.up * projectileFireHeight + transform.forward * projectileFireForward;
    }

    // 플레이어 몸통 중심 (콜라이더가 있으면 그 중심, 없으면 피벗 + 1m)
    private Vector3 GetPlayerAimPoint()
    {
        Collider playerCollider = player.GetComponent<Collider>();
        return playerCollider != null ? playerCollider.bounds.center : player.position + Vector3.up;
    }

    private void CreateProjectileTelegraph()
    {
        DestroyProjectileTelegraph();

        projectileTelegraph = new GameObject("Boss Projectile Telegraph").transform;

        // 발사 위치에 커지는 빛 구체
        projectileOrb = BossVisuals.CreateOrb("Orb", projectileTelegraph, out projectileOrbRenderer);
        projectileOrb.position = GetFireOrigin();
        projectileOrb.localScale = Vector3.zero;

        // 바닥에 발사 방향 사선 (FPS 시점에서도 보이도록 바닥에 그림)
        projectileLineRenderers.Clear();
        Vector3 flatAim = new Vector3(projectileAim.x, 0f, projectileAim.z).normalized;
        if (flatAim.sqrMagnitude < 0.0001f) flatAim = transform.forward;

        for (int i = 0; i < projectileShotCount; i++)
        {
            float angle = (i - (projectileShotCount - 1) * 0.5f) * projectileSpread;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * flatAim;

            var lineRoot = new GameObject("Line").transform;
            lineRoot.SetParent(projectileTelegraph, false);
            lineRoot.position = transform.position + Vector3.up * 0.06f;
            lineRoot.rotation = Quaternion.LookRotation(direction);
            BossVisuals.CreateStrip("Strip", lineRoot, 0.35f, projectileSpeed * projectileLifetime * 0.5f, out Renderer lineRenderer);
            projectileLineRenderers.Add(lineRenderer);
        }
    }

    private void UpdateProjectileTelegraph(float t)
    {
        if (projectileOrb != null)
        {
            projectileOrb.localScale = Vector3.one * Mathf.Lerp(0.2f, projectileRadius * 2.4f, t);
            BossVisuals.SetColor(projectileOrbRenderer, new Color(1f, 0.35f, 0.15f, 1f), Mathf.Lerp(0.5f, 0.95f, t));
        }

        float blink = 0.5f + 0.5f * Mathf.Sin(stateTimer * 14f * Mathf.PI * 2f);
        foreach (Renderer lineRenderer in projectileLineRenderers)
        {
            BossVisuals.SetColor(lineRenderer, new Color(1f, 0.3f, 0.1f, 1f), Mathf.Lerp(0.2f, 0.55f, blink));
        }
    }

    private void DestroyProjectileTelegraph()
    {
        if (projectileTelegraph != null)
        {
            Destroy(projectileTelegraph.gameObject);
            projectileTelegraph = null;
        }
        projectileOrb = null;
        projectileOrbRenderer = null;
        projectileLineRenderers.Clear();
    }

    // 회복/경직 ------------------------------------------------------

    private void EnterRecover(float time, bool stunned)
    {
        state = BossState.Recover;
        stateTimer = time;
        StopMoving();
        SetTrigger(stunned ? AnimStun : AnimIdle);
    }

    private void UpdateRecover(float dt)
    {
        stateTimer -= dt;
        if (stateTimer <= 0f)
        {
            EnterChase(isPhase2 ? phase2Cooldown : patternCooldown);
        }
    }

    // 데미지, 사망 ----------------------------------------------------

    public void TakeDamage(float _damage)
    {
        if (IsDead || currentHP <= 0f)
        {
            return;
        }

        currentHP = Mathf.Max(currentHP - _damage, 0f);

        flashTimer = hitFlashTime;
        visualDirty = true;

        if (BossUI.Instance != null)
        {
            BossUI.Instance.SetHP(currentHP, maxHP);
        }

        if (currentHP <= 0f)
        {
            Die();
            return;
        }

        if (!isPhase2 && currentHP <= maxHP * phase2HPRatio)
        {
            EnterPhase2();
        }
    }

    private void EnterPhase2()
    {
        isPhase2 = true;
        visualDirty = true;

        if (agent != null) agent.speed = moveSpeed * phase2SpeedMultiplier;
        if (BossUI.Instance != null) BossUI.Instance.SetPhase2();

        CameraShake.Shake(0.2f, 0.5f);
    }

    private void Die()
    {
        state = BossState.Dead;

        DestroyDashTelegraph();
        DestroyProjectileTelegraph();
        StopMoving();

        foreach (Collider bodyCollider in GetComponentsInChildren<Collider>())
        {
            bodyCollider.enabled = false; // 시체가 총알/플레이어를 막지 않도록
        }

        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetBool(AnimIsDead, true);
        }

        flashTimer = 0f;
        isPhase2 = false; // 붉은 발광 해제
        visualDirty = true;
        UpdateBodyVisual(0f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKillEXP(exp, transform.position + Vector3.up * 3f);
            GameManager.Instance.OnBossDefeated();
        }

        if (BossUI.Instance != null)
        {
            BossUI.Instance.Hide();
        }

        CameraShake.Shake(0.25f, 0.6f);

        Destroy(gameObject, 5f); // 사망 모션이 끝난 뒤 제거
    }

    private void OnDestroy()
    {
        DestroyDashTelegraph();
        DestroyProjectileTelegraph();
    }

    // 몸 색 (피격 깜빡임 + 2페이즈 붉은색) -----------------------------
    // 몬스터 셰이더의 _Color(곱)와 _EmissionColor(더하기)를 MaterialPropertyBlock으로 바꿈 (머티리얼 원본은 건드리지 않음)

    private void UpdateBodyVisual(float dt)
    {
        if (flashTimer > 0f)
        {
            flashTimer -= dt;
            if (flashTimer <= 0f)
            {
                visualDirty = true;
            }
        }

        if (!visualDirty || bodyRenderers == null)
        {
            return;
        }
        visualDirty = false;

        Color emission = flashTimer > 0f ? hitFlashColor : (isPhase2 ? phase2Emission : Color.black);

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            Renderer bodyRenderer = bodyRenderers[i];
            if (bodyRenderer == null) continue;

            Color color = isPhase2 ? baseColors[i] * phase2Tint : baseColors[i];

            bodyRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(EmissionId, emission);
            bodyRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    // 유틸 ----------------------------------------------------------

    private void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
    }

    private void FacePlayer(float dt, float degreesPerSecond)
    {
        Vector3 direction = FlatDirection(transform.position, player.position);
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degreesPerSecond * dt);
    }

    private void SetTrigger(string trigger)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        // 소비되지 않은 다른 트리거가 남아 엉뚱한 모션으로 넘어가지 않도록 정리
        animator.ResetTrigger(AnimIdle);
        animator.ResetTrigger(AnimRun);
        animator.ResetTrigger(AnimCast);
        animator.ResetTrigger(AnimWindup);
        animator.ResetTrigger(AnimDash);
        animator.ResetTrigger(AnimStun);

        animator.SetTrigger(trigger);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashHitRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, nearDistance);
    }
}
