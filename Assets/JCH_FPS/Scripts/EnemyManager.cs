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
