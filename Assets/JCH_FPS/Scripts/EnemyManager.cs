// 11/09 - 파일 인코딩 CP949 -> UTF-8 변환 (한글 주석 깨짐 해결)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        WaveStart();
    }

    // Update is called once per frame
    void Update()
    {
        if (isWaveStarted)
        {
            SpawnEnemy();
        }

        RelocateFarEnemies(); // 11/22 - 너무 멀어진 적을 플레이어 근처로 재배치
    }

    // 11/22 - 먼 적 재배치 ---------------------------------------
    // relocateCheckInterval마다 검사해서 플레이어와 relocateDistance보다 먼 일반 적(보스 제외)을
    // 플레이어와 가까운 스폰 포인트로 옮김. 여러 마리가 한 점에 겹치지 않도록 가까운 포인트 몇 곳을 돌아가며 사용

    [Header("=== 먼 적 재배치 ===")]
    [SerializeField]
    private float relocateDistance = 60f;
    [SerializeField]
    private float relocateCheckInterval = 1f;
    [SerializeField]
    private int relocateNearestPointCount = 3; // 플레이어와 가까운 스폰 포인트 중 몇 곳을 돌아가며 쓸지

    private float relocateTimer = 0f;
    private Transform playerTransform;

    private void RelocateFarEnemies()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;
        if (spawnPoints.Count == 0) return;

        relocateTimer += Time.deltaTime;
        if (relocateTimer < relocateCheckInterval) return;
        relocateTimer = 0f;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            playerTransform = player.transform;
        }

        Vector3 playerPos = playerTransform.position;
        float limitSqr = relocateDistance * relocateDistance;
        List<Transform> nearestPoints = null;
        int nextPoint = 0;

        foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            if ((enemy.transform.position - playerPos).sqrMagnitude <= limitSqr) continue;

            // 옮길 적이 있을 때만 가까운 스폰 포인트 목록을 계산
            if (nearestPoints == null)
            {
                nearestPoints = new List<Transform>();
                foreach (Transform point in spawnPoints)
                {
                    if (point != null) nearestPoints.Add(point);
                }
                if (nearestPoints.Count == 0) return;

                nearestPoints.Sort((a, b) =>
                    (a.position - playerPos).sqrMagnitude.CompareTo((b.position - playerPos).sqrMagnitude));
            }

            int usable = Mathf.Clamp(relocateNearestPointCount, 1, nearestPoints.Count);
            Vector3 target = nearestPoints[nextPoint % usable].position;
            nextPoint++;

            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                // NavMesh 위의 가장 가까운 지점으로 순간이동 (못 찾으면 이번엔 건너뜀)
                if (UnityEngine.AI.NavMesh.SamplePosition(target, out UnityEngine.AI.NavMeshHit hit, 5f, agent.areaMask))
                {
                    agent.Warp(hit.position);
                }
            }
            else
            {
                enemy.transform.position = target;
            }
        }
    }

    // 적 소환 -------------------------------------------

    [Header("=== 웨이브 시작 여부 ===")]
    public bool isWaveStarted = false;

    [Header("=== 웨이브 데이터 ===")]
    [SerializeField]
    private WaveData waveData;

    [Header("=== 스폰 위치 리스트 ===")]
    [SerializeField]
    private List<Transform> spawnPoints = new List<Transform>();

    private int spawnPointCount; // 스폰 위치 카운트

    [Header("=== 페이즈 카운트 ===")]
    [SerializeField]
    private int phaseCount = 0; // 현재 웨이브의 페이즈

    private float phaseDelay = 0;
    private float phaseDelayTime = 0;

    // 11/09 - 무한 웨이브: 웨이브 데이터를 한 바퀴 돌 때마다 새로 나오는 적의 체력/공격력 증가
    [Header("=== 무한 웨이브 강화 (바퀴당 증가율) ===")]
    [SerializeField]
    private float hpIncreasePerLoop = 0.2f; // 체력 +20%
    [SerializeField]
    private float damageIncreasePerLoop = 0.1f; // 공격력 +10%

    [SerializeField]
    private int loopCount = 0; // 웨이브 데이터를 반복한 횟수

    public void WaveStart()
    {
        isWaveStarted = true;
    }

    private void SpawnEnemy()
    {
        List<SubPhase> currentSubPhases = waveData.subPhases; // 웨이브 데이터에서 서브 페이즈를 가져옴

        if(currentSubPhases.Count == 0) // 11/09 - 무한 반복으로 바뀌어 페이즈가 비어있을 때만 정지
        {
            return;
        }

        // 11/09 - 이번 바퀴의 강화 배율 (0바퀴 = x1.0, 1바퀴 = 체력 x1.2 / 공격력 x1.1 ...)
        float hpMultiplier = 1f + hpIncreasePerLoop * loopCount;
        float damageMultiplier = 1f + damageIncreasePerLoop * loopCount;

        if(Timer(ref phaseDelayTime, phaseDelay))
        {
            SubPhase currentPhase = currentSubPhases[phaseCount]; // 현재 페이즈

            phaseDelay = currentPhase.spawnInterval; // 페이즈간의 시간차 설정

            foreach (EnemySpawnInfo spawnInfo in currentPhase.enemyList) // spawInfo는 enemyList의 적 종류와 숫자
            {
                GameObject enemyPrefab = spawnInfo.enemyPrefab; // EnemySpawnInfo의 적 종류

                for (int i = 0; i < spawnInfo.count; i++) // EnemySpawnInfo의 적 수
                {
                    GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPoints[spawnPointCount].position, Quaternion.identity); // 적 소환

                    // 11/09 - 바퀴 수에 따라 체력/공격력 강화 적용
                    EnemyBase enemy = spawnedEnemy.GetComponentInChildren<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.ApplyStatMultiplier(hpMultiplier, damageMultiplier);
                    }

                    spawnPointCount++; // 스폰 포인트 위치 변경

                    if (spawnPointCount >= spawnPoints.Count) // 스폰 포인트 리스트 범위를 벗어나면 초기화
                    {
                        spawnPointCount = 0;
                    }
                }
            }

            phaseCount++; // 소환이 끝나면 다음 페이즈로 카운트 변경

            // 11/09 - 마지막 페이즈까지 끝나면 처음부터 다시 반복하고 강화 단계 증가
            if (phaseCount >= currentSubPhases.Count)
            {
                phaseCount = 0;
                loopCount++;
            }
        }
    }

    // 11/15 - 보스 소환 -------------------------------------------
    // GameManager가 등장 조건을 만족했을 때 호출. 플레이어에게서 가장 먼 스폰 포인트에 소환하고
    // 현재 무한 웨이브 바퀴 수 + 보스 회차만큼 강화한다. (일반 몹 스폰은 그대로 계속됨)

    [Header("=== 보스 ===")]
    [SerializeField]
    private GameObject bossPrefab;

    // 11/15 - 보스 회차(몇 번째 보스인지)마다 강화. 무한 웨이브 바퀴 강화와 곱해짐
    [Header("=== 보스 회차당 증가율 (2번째 보스부터 적용) ===")]
    [SerializeField]
    private float bossHpIncreasePerRound = 0.3f; // 체력 +30%
    [SerializeField]
    private float bossDamageIncreasePerRound = 0.3f; // 공격력 +30%

    [SerializeField]
    private int bossRound = 0; // 지금까지 소환한 보스 수 (확인용)

    public bool SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("[Boss] EnemyManager에 bossPrefab이 연결되지 않았습니다. Tools/Boss/Build Boss (Main)을 실행하세요.");
            return false; // GameManager가 다음 주기까지 기다렸다가 다시 시도
        }

        if (spawnPoints.Count == 0)
        {
            return false;
        }

        bossRound++;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Transform spawnPoint = spawnPoints[0];

        if (player != null)
        {
            float farthest = -1f;
            foreach (Transform point in spawnPoints)
            {
                if (point == null) continue;

                float distance = (point.position - player.transform.position).sqrMagnitude;
                if (distance > farthest)
                {
                    farthest = distance;
                    spawnPoint = point;
                }
            }
        }

        GameObject spawnedBoss = Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);

        BossEnemy boss = spawnedBoss.GetComponentInChildren<BossEnemy>();
        if (boss != null)
        {
            // 웨이브 바퀴 강화 x 보스 회차 강화 (1번째 보스는 회차 강화 없음)
            int extraRounds = bossRound - 1;
            float hpMultiplier = (1f + hpIncreasePerLoop * loopCount) * (1f + bossHpIncreasePerRound * extraRounds);
            float damageMultiplier = (1f + damageIncreasePerLoop * loopCount) * (1f + bossDamageIncreasePerRound * extraRounds);

            boss.ApplyStatMultiplier(hpMultiplier, damageMultiplier);
            boss.SetRound(bossRound);
        }

        return true;
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
