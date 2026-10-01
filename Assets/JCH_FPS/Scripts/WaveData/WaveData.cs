using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemySpawnInfo
{
    public GameObject enemyPrefab; // 소환할 적의 종류
    public int count; // 적의 수
}

[System.Serializable]
public struct SubPhase
{
    [Header("===현재 웨이브의 페이즈===")]
    public string phaseName;

    [Header("=== 스폰 될 적 목록===")]
    public List<EnemySpawnInfo> enemyList;
    public float spawnInterval;
}

[CreateAssetMenu(fileName = "NewWaveData", menuName = "Wave Data/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("서브 페이즈 리스트")]
    public List<SubPhase> subPhases = new List<SubPhase>();
}
