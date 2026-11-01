using UnityEngine;

// 2026-11-01: 타이틀 설정 화면에서 바꾸는 값들을 PlayerPrefs에 저장/불러오기.
// 게임 시작 시 자동으로 볼륨/전체화면을 적용하고, 마우스 감도는 PlayerMove.Start에서 읽어간다.
public static class GameSettings
{
    private const string MouseSensitivityKey = "Settings.MouseSensitivity";
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string FullscreenKey = "Settings.Fullscreen";

    public const float MinMouseSensitivity = 0.5f;
    public const float MaxMouseSensitivity = 10f;
    public const float DefaultMouseSensitivity = 3f;

    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
        set => PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity));
    }

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        set
        {
            float v = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, v);
            AudioListener.volume = v;
        }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        set
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Screen.fullScreen = value;
        }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettings()
    {
        AudioListener.volume = MasterVolume;
#if !UNITY_EDITOR
        Screen.fullScreen = Fullscreen;
#endif
    }
}
