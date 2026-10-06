using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    // 2026-10-06: 경험치 바 UI 연결용 필드 추가 (체력/탄환 UI와 동일한 패턴)
    [Header("����ġ UI")]
    [SerializeField] private Image expBar;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expValueText; // 2026-10-06: 경험치 바 가운데 "현재/다음레벨 필요치" 텍스트

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

        RefreshEXPUI(); // 2026-10-06: 시작 시 경험치 바 초기값 표시
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

        RefreshEXPUI(); // 2026-10-06: 경험치 획득 시 바/텍스트 갱신
    }

    public void LevelUp()
    {
        currentLeve++;
        // 2026-10-06: 레벨업해도 0으로 초기화하지 않고 이전 레벨 필요치 그대로 유지
        // (예: 1->2는 100/175, 2->3은 175/306 식으로 누적)
        currentEXP = maxEXP;
        maxEXP = maxEXP * 1.75f; // 2026-10-06: 레벨업 요구치 증가율 1.2 -> 1.75로 변경

        ShowPerkSelection();
    }

    // 2026-10-06: 체력/탄환 UI와 동일하게 경험치 바/레벨/진행치 텍스트를 갱신하는 메서드 추가
    private void RefreshEXPUI()
    {
        if (expBar != null)
        {
            expBar.fillAmount = currentEXP / maxEXP;
        }

        if (levelText != null)
        {
            levelText.text = "Lv. " + currentLeve;
        }

        if (expValueText != null)
        {
            expValueText.text = Mathf.FloorToInt(currentEXP) + " / " + Mathf.FloorToInt(maxEXP);
        }
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
