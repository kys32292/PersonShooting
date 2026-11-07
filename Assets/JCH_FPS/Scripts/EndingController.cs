using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 2026-11-07: 엔딩(게임 오버) 화면. UI는 Tools/HUD/Build Ending Scene 으로 생성/연결됨.
// GameManager.GameOver()가 RunResult에 남긴 기록을 표시한다.
public class EndingController : MonoBehaviour
{
    [Header("기록 텍스트")]
    [SerializeField] private Text survivalTimeText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text levelText;

    [Header("선택한 빌드(특성)")]
    [SerializeField] private Transform perkListRoot;
    [SerializeField] private GameObject perkEntryTemplate; // 꺼진 상태로 두고 복제해서 사용 (자식 Text에 이름 표시)
    [SerializeField] private GameObject noPerkText;

    [Header("버튼")]
    [SerializeField] private Button titleButton;

    [Header("타이틀 씬 이름")]
    [SerializeField] private string titleSceneName = "Title";

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titleButton != null) titleButton.onClick.AddListener(GoToTitle);

        ShowResult();
    }

    private void Start()
    {
        if (titleButton != null) titleButton.Select();
    }

    private void ShowResult()
    {
        int seconds = Mathf.FloorToInt(RunResult.SurvivalTime);
        if (survivalTimeText != null) survivalTimeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        if (scoreText != null) scoreText.text = Mathf.FloorToInt(RunResult.Score).ToString("N0");
        if (levelText != null) levelText.text = "Lv. " + Mathf.Max(RunResult.Level, 1);

        // 같은 특성은 한 줄로 묶어 "이름 ×횟수"로 표시 (처음 고른 순서 유지)
        var order = new List<PerkData>();
        var counts = new Dictionary<PerkData, int>();
        foreach (var perk in RunResult.Perks)
        {
            if (perk == null) continue;
            if (!counts.ContainsKey(perk))
            {
                counts[perk] = 0;
                order.Add(perk);
            }
            counts[perk]++;
        }

        if (perkEntryTemplate != null) perkEntryTemplate.SetActive(false);
        if (noPerkText != null) noPerkText.SetActive(order.Count == 0);
        if (perkListRoot == null || perkEntryTemplate == null) return;

        foreach (var perk in order)
        {
            var entry = Instantiate(perkEntryTemplate, perkListRoot);
            entry.name = "Perk_" + perk.perkName;
            var label = entry.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = counts[perk] > 1 ? $"{perk.perkName}  <color=#E0453A>×{counts[perk]}</color>" : perk.perkName;
            }
            entry.SetActive(true);
        }
    }

    private void GoToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
