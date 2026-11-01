using UnityEngine;
using UnityEngine.UI;

// 2026-11-01: 설정 창(마우스 감도 / 마스터 볼륨 / 전체 화면). 타이틀과 일시정지 메뉴가 같이 사용한다.
// UI는 Tools/HUD/Build Title Menu, Tools/HUD/Build Pause Menu (Main) 으로 생성/연결됨.
// 이 컴포넌트는 설정 창 루트(SettingsPanel)에 붙고, 열고 닫는 건 각 메뉴 컨트롤러가 SetActive로 한다.
public class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Text sensitivityValueText;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Text volumeValueText;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = GameSettings.MinMouseSensitivity;
            sensitivitySlider.maxValue = GameSettings.MaxMouseSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void OnEnable()
    {
        // 열 때마다 저장된 값으로 다시 맞춤
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
        RefreshValueTexts();

        if (closeButton != null) closeButton.Select();
    }

    private void OnDisable()
    {
        GameSettings.Save();
    }

    private void OnSensitivityChanged(float value)
    {
        GameSettings.MouseSensitivity = value;

        // 게임 중(일시정지 메뉴)이라면 플레이어에 바로 적용
        var player = FindObjectOfType<PlayerMove>();
        if (player != null) player.mouseSensitivity = GameSettings.MouseSensitivity;

        RefreshValueTexts();
    }

    private void OnVolumeChanged(float value)
    {
        GameSettings.MasterVolume = value;
        RefreshValueTexts();
    }

    private void OnFullscreenChanged(bool isOn)
    {
        GameSettings.Fullscreen = isOn;
    }

    private void RefreshValueTexts()
    {
        if (sensitivityValueText != null) sensitivityValueText.text = GameSettings.MouseSensitivity.ToString("0.0");
        if (volumeValueText != null) volumeValueText.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
    }
}
