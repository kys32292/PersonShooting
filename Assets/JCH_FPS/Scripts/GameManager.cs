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
    [Header("경험치 UI")] // 11/09 - 깨진 헤더 글자 복구
    [SerializeField] private Image expBar;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expValueText; // 2026-10-06: 경험치 바 가운데 "현재/다음레벨 필요치" 텍스트

    private PlayerMove playerPrefab;

    [Header("Paused Panel")]
    [SerializeField]
    private GameObject ESCPausePanel;

    public bool isPaused = false;

    // 2026-11-07: 엔딩 화면용 이번 판 기록
    [Header("엔딩 씬 이름")]
    [SerializeField] private string endingSceneName = "Ending";

    private float survivalTime = 0f;
    private float totalEXP = 0f; // 레벨업 때 잘리는 초과분까지 포함한 획득 경험치 총합 (= 점수)
    private readonly List<PerkData> selectedPerks = new List<PerkData>();
    private bool isGameOver = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        perkUI = GetComponent<PerkUIController>();

        perkUI.CloseUI();

        // 11/09 - 경험치 연출 매니저가 연결 안 되어 있으면 자동으로 붙이고 경험치 바를 도착점으로 설정
        if (expPopup == null)
        {
            expPopup = GetComponent<ExpPopupManager>();
            if (expPopup == null)
            {
                expPopup = gameObject.AddComponent<ExpPopupManager>();
            }
        }
        if (expBar != null)
        {
            expPopup.Init(expBar.canvas, expBar.rectTransform);
        }

        RefreshEXPUI(); // 2026-10-06: 시작 시 경험치 바 초기값 표시
    }

    private void Update()
    {
        if (!isPaused && !isGameOver)
        {
            survivalTime += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (ESCPausePanel.activeSelf == false)
            {
                ESCPausePanel.SetActive(true);
                isPaused = true;
            }
            else
            {
                ESCPausePanel.SetActive(false);
                isPaused = false;
            }
        }
    }

    public void GameStarted()
    {
        playerPrefab = FindObjectOfType<PlayerMove>();
    }

    public void AddEXP(float exp)
    {
        AddEXPInternal(exp);

        RefreshEXPUI(); // 2026-10-06: 경험치 획득 시 바/텍스트 갱신
    }

    // 11/09 - 내부 수치만 올림 (UI 갱신은 호출하는 쪽에서 결정)
    private void AddEXPInternal(float exp)
    {
        currentEXP += exp;
        totalEXP += exp;

        if(currentEXP >= maxEXP)
        {
            LevelUp();
        }
    }

    // 11/09 - 적 처치 경험치 ----------------------------------------
    // 처치 방식에 따라 배율을 적용하고, 경험치 연출(숫자 -> 이미지 S자 이동)이 끝난 뒤 경험치 바를 갱신
    // 내부 수치/레벨업(특성 선택 창)은 즉시 처리됨

    [Header("처치 경험치 배율 (보너스는 서로 더해짐)")]
    [SerializeField] private float closeDistance = 5f;   // 이 거리 이하 = 근거리
    [SerializeField] private float farDistance = 20f;    // 이 거리 이상 = 원거리
    [SerializeField] private float closeBonus = 0.5f;    // 근거리 처치 +50%
    [SerializeField] private float farBonus = 0.5f;      // 원거리 처치 +50%
    [SerializeField] private float spinBonus = 1.0f;     // 360도 회전 처치 +100%
    [SerializeField] private float jumpBonus = 0.5f;     // 점프 중 처치 +50%

    [Header("경험치 연출")]
    [SerializeField] private ExpPopupManager expPopup;

    public void AddKillEXP(float baseExp, Vector3 deathPosition)
    {
        float finalExp = baseExp * GetKillMultiplier(deathPosition);

        AddEXPInternal(finalExp);

        if (expPopup != null)
        {
            expPopup.Play(finalExp, deathPosition, RefreshEXPUI);
        }
        else
        {
            RefreshEXPUI();
        }
    }

    private float GetKillMultiplier(Vector3 deathPosition)
    {
        if (playerPrefab == null)
        {
            playerPrefab = FindObjectOfType<PlayerMove>();
        }
        if (playerPrefab == null)
        {
            return 1f;
        }

        float multiplier = 1f;

        // 1. 거리 구간 (가까울 때, 멀 때 모두 보너스)
        Vector3 playerPos = playerPrefab.transform.position;
        float distance = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(deathPosition.x, deathPosition.z));
        if (distance <= closeDistance)
        {
            multiplier += closeBonus;
        }
        else if (distance >= farDistance)
        {
            multiplier += farBonus;
        }

        // 2. 0.5초 안에 약 360도 회전
        if (playerPrefab.RecentlySpun)
        {
            multiplier += spinBonus;
        }

        // 3. 점프 중
        if (playerPrefab.IsAirborne)
        {
            multiplier += jumpBonus;
        }

        return multiplier;
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
        selectedPerks.Add(perk);

        // 게임 재개 및 UI 닫기
        Time.timeScale = 1f;
        perkUI.CloseUI();
    }

    // 2026-11-07: 기록을 RunResult에 넘기고 엔딩 씬으로 이동
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        RunResult.Set(survivalTime, totalEXP, currentLeve, selectedPerks);

        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(endingSceneName);
    }

    private void ResetData()
    {
        currentEXP = 0;
        maxEXP = 100;
        currentLeve = 1;
    }
}
