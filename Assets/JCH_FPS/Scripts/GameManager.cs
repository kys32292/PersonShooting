using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("경험치, 레벨")]
    public float currentEXP = 0;
    public float maxEXP = 100;
    public int currentLeve = 1;

    [Header("특성 데이터베이스")]
    [SerializeField] private List<PerkData> allPerks;

    [Header("UI 연결")]
    [SerializeField] private PerkUIController perkUI;

    private PlayerMove playerPrefab;

    public bool isPaused = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        perkUI = GetComponent<PerkUIController>();

        perkUI.CloseUI();
    }

    public void GameStarted()
    {
        playerPrefab = FindObjectOfType<PlayerMove>();
    }

    public void AddEXP(float exp)
    {
        currentEXP += exp;

        if(currentEXP >= maxEXP)
        {
            LevelUp();
        }
    }

    public void LevelUp()
    {
        currentLeve++;
        currentEXP -= maxEXP;
        maxEXP = maxEXP * 1.2f;

        ShowPerkSelection();
    }

    private void ShowPerkSelection()
    {
        // 1. 전체 특성 목록 중 중복 없이 3개 뽑기
        List<PerkData> selectedPerks = GetRandomPerks(3);

        // 2. UI 띄우기
        if (perkUI != null)
        {
            perkUI.DisplayPerks(selectedPerks);
        }
    }

    // 중복 없는 3개 뽑기 로직
    private List<PerkData> GetRandomPerks(int count)
    {
        List<PerkData> tempList = new List<PerkData>(allPerks);
        List<PerkData> result = new List<PerkData>();

        for (int i = 0; i < count; i++)
        {
            if (tempList.Count == 0) break;

            int randomIndex = Random.Range(0, tempList.Count);
            result.Add(tempList[randomIndex]);

            // 이미 뽑힌 항목은 임시 리스트에서 제거하여 중복 방지
            tempList.RemoveAt(randomIndex);
        }

        return result;
    }

    // 플레이어가 특성을 선택했을 때 호출됨
    public void SelectPerk(PerkData perk)
    {
        PlayerMove player = FindObjectOfType<PlayerMove>();
        if (player != null)
        {
            player.ApplyPerk(perk);
        }

        // 게임 재개 및 UI 닫기
        Time.timeScale = 1f;
        perkUI.CloseUI();
    }

    public void GameOver()
    {

    }

    private void ResetData()
    {
        currentEXP = 0;
        maxEXP = 100;
        currentLeve = 1;
    }
}
