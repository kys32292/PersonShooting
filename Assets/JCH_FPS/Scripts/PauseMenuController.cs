using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 2026-10-21: ESC 일시정지 메뉴. UI는 Tools/HUD/Build Pause Menu (Main) 으로 생성/연결됨.
// 이 컴포넌트는 항상 켜져 있는 PauseMenu 루트에 붙고, 실제 화면(pausePanel)만 껐다 켰다 한다.
public class PauseMenuController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("설정 화면 (없으면 비워둠)")]
    [SerializeField] private GameObject settingsPanel;

    [Header("메인 메뉴 씬 이름")]
    [SerializeField] private string mainMenuSceneName = "Title";

    public bool IsOpen { get; private set; }

    // 일시정지 직전 상태를 기억해 두었다가 재개할 때 그대로 되돌림
    private float prevTimeScale = 1f;
    private bool prevGamePaused;
    private CursorLockMode prevLockState;
    private bool prevCursorVisible;

    private PerkUIController perkUI;
    private Text perkListText; // 2026-11-25: 선택한 증강 목록 (Awake에서 코드로 생성)

    private void Awake()
    {
        perkUI = FindObjectOfType<PerkUIController>();

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        if (pausePanel != null)
        {
            CreatePerkListPanel();
            pausePanel.SetActive(false);
        }
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (IsOpen)
        {
            // 설정 화면이 열려 있으면 ESC는 설정만 닫음
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
                return;
            }
            Resume();
        }
        else if (perkUI == null || !perkUI.IsOpen) // 특성 선택 중에는 일시정지 막음
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (IsOpen) return;
        IsOpen = true;

        prevTimeScale = Time.timeScale;
        prevLockState = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameManager.Instance != null)
        {
            prevGamePaused = GameManager.Instance.isPaused;
            GameManager.Instance.isPaused = true;
        }

        RefreshPerkList();
        pausePanel.SetActive(true);
        UISorting.BringToFront(gameObject, UISorting.PauseOrder); // 2026-11-25: 일시정지/설정 창을 경험치 팝업·증강 UI보다 위에
        if (resumeButton != null) resumeButton.Select();
    }

    public void Resume()
    {
        if (!IsOpen) return;
        IsOpen = false;

        pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = prevTimeScale;
        Cursor.lockState = prevLockState;
        Cursor.visible = prevCursorVisible;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isPaused = prevGamePaused;
        }
    }

    // 2026-11-25: 일시정지 창 오른쪽에 "선택한 증강" 패널 생성 (씬 수정 없이 코드로 생성)
    private void CreatePerkListPanel()
    {
        var panel = new GameObject("PerkListPanel", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var rt = (RectTransform)panel.transform;
        rt.SetParent(pausePanel.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(310f, 0f); // 가운데 창(폭 560) 오른쪽
        rt.sizeDelta = new Vector2(340f, 0f);
        panel.GetComponent<Image>().color = new Color(0.086f, 0.098f, 0.122f, 0.98f);

        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 20, 20);
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText(rt, "Title", 26, FontStyle.Bold).text = "선택한 증강";
        perkListText = CreateText(rt, "List", 20, FontStyle.Normal);
    }

    private static Text CreateText(Transform parent, string name, int size, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        text.lineSpacing = 1.3f;
        text.supportRichText = true;
        text.raycastTarget = false;
        return text;
    }

    // 같은 증강은 한 줄로 묶고 LV(선택 횟수) 표시. 선택한 순서 유지.
    private void RefreshPerkList()
    {
        if (perkListText == null) return;

        var order = new List<PerkData>();
        var counts = new Dictionary<PerkData, int>();
        if (GameManager.Instance != null)
        {
            foreach (var perk in GameManager.Instance.SelectedPerks)
            {
                if (perk == null) continue;
                if (!counts.ContainsKey(perk)) { counts[perk] = 0; order.Add(perk); }
                counts[perk]++;
            }
        }

        if (order.Count == 0)
        {
            perkListText.text = "<color=#8C949E>아직 선택한 증강이 없습니다</color>";
            return;
        }

        var sb = new StringBuilder();
        foreach (var perk in order)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(perk.perkName).Append("  <color=#E0453A>LV.").Append(counts[perk]).Append("</color>");
        }
        perkListText.text = sb.ToString();
    }

    private void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.Log("설정 화면이 아직 연결되지 않았습니다. PauseMenuController.settingsPanel에 연결하세요.");
        }
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
