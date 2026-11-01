using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 2026-11-01: 타이틀 화면(게임 시작 / 설정 / 종료). UI는 Tools/HUD/Build Title Menu 로 생성/연결됨.
// 설정 창 내용은 SettingsPanelController가 담당한다.
public class TitleMenuController : MonoBehaviour
{
    [Header("메인 버튼")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("설정 화면")]
    [SerializeField] private GameObject settingsPanel;

    [Header("게임 씬 이름")]
    [SerializeField] private string gameSceneName = "Main";

    private void Awake()
    {
        // 게임 중 메인 메뉴로 돌아왔을 때 커서/시간이 잠긴 상태일 수 있으므로 초기화
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Start()
    {
        if (startButton != null) startButton.Select();
    }

    private void Update()
    {
        // 설정 화면이 열려 있으면 ESC로 닫기
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
            if (settingsButton != null) settingsButton.Select();
        }
    }

    private void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
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
