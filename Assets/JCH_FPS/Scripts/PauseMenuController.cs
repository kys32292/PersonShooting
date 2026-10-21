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

    private void Awake()
    {
        perkUI = FindObjectOfType<PerkUIController>();

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        if (pausePanel != null) pausePanel.SetActive(false);
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

        pausePanel.SetActive(true);
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
