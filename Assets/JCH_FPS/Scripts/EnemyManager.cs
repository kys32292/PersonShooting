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

    public void WaveStart()
    {
        isWaveStarted = true;
    }

    private void SpawnEnemy()
    {
        List<SubPhase> currentSubPhases = waveData.subPhases; // 웨이브 데이터에서 서브 페이즈를 가져옴

        if(phaseCount >= currentSubPhases.Count) // 페이즈 숫자를 넘길 경우 정지
        {
            return;
        }

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

                    spawnPointCount++; // 스폰 포인트 위치 변경

                    if (spawnPointCount >= spawnPoints.Count) // 스폰 포인트 리스트 범위를 벗어나면 초기화
                    {
                        spawnPointCount = 0;
                    }
                }
            }

            phaseCount++; // 소환이 끝나면 다음 페이즈로 카운트 변경
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
