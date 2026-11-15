// 11/15 - 보스 UI: 상단 보스 HP 바 + 등장 경고 텍스트
// UI는 Tools/Boss/Build Boss (Main) 메뉴로 Main 씬에 생성/연결됨. BossEnemy가 Instance를 통해 호출함.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossUI : MonoBehaviour
{
    public static BossUI Instance { get; private set; }

    [Header("HP 바")]
    [SerializeField] private GameObject barRoot;
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text hpText;

    [Header("등장 경고")]
    [SerializeField] private CanvasGroup warningGroup;
    [SerializeField] private float warningDuration = 2f;

    [Header("색")]
    [SerializeField] private Color phase1Color = new Color(0.86f, 0.24f, 0.2f, 1f);
    [SerializeField] private Color phase2Color = new Color(1f, 0.08f, 0.35f, 1f);

    private float warningTimer = -1f;

    private void Awake()
    {
        Instance = this;

        if (barRoot != null) barRoot.SetActive(false);
        if (warningGroup != null) warningGroup.alpha = 0f;
        if (hpFill != null) fillAnchorWidth = hpFill.rectTransform.anchorMax.x - hpFill.rectTransform.anchorMin.x;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string bossName)
    {
        if (barRoot != null) barRoot.SetActive(true);
        if (nameText != null) nameText.text = bossName;
        if (hpFill != null)
        {
            hpFill.color = phase1Color;
        }
        ApplyFill(1f);

        warningTimer = 0f;
    }

    public void SetHP(float current, float max)
    {
        if (hpText != null) hpText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);

        // 11/15 - 플레이어 HP 바와 똑같이 맞는 즉시 줄어들도록 (이전: 천천히 따라가는 보간)
        ApplyFill(max > 0f ? current / max : 0f);
    }

    // 11/15 - Filled 이미지는 스프라이트가 없으면 fillAmount가 무시되어 바가 항상 가득 차 보였음
    // 스프라이트가 있으면 fillAmount(플레이어 바와 동일), 없으면 가로 앵커를 줄여서 표시
    private void ApplyFill(float ratio)
    {
        if (hpFill == null) return;

        ratio = Mathf.Clamp01(ratio);

        if (hpFill.sprite != null && hpFill.type == Image.Type.Filled)
        {
            hpFill.fillAmount = ratio;
            return;
        }

        RectTransform rt = hpFill.rectTransform;
        Vector2 anchorMax = rt.anchorMax;
        anchorMax.x = rt.anchorMin.x + (fillAnchorWidth * ratio);
        rt.anchorMax = anchorMax;
    }

    private float fillAnchorWidth = 1f; // Awake 시점의 앵커 가로폭 (기본 0~1)

    public void SetPhase2()
    {
        if (hpFill != null) hpFill.color = phase2Color;
    }

    public void Hide()
    {
        if (barRoot != null) barRoot.SetActive(false);
        if (warningGroup != null) warningGroup.alpha = 0f;
        warningTimer = -1f;
    }

    private void Update()
    {
        // 일시정지 중에도 UI는 자연스럽게 마무리되도록 unscaled 시간 사용
        float dt = Time.unscaledDeltaTime;

        if (warningGroup != null && warningTimer >= 0f)
        {
            warningTimer += dt;

            if (warningTimer >= warningDuration)
            {
                warningGroup.alpha = 0f;
                warningTimer = -1f;
            }
            else
            {
                // 3번 깜빡이고, 마지막 0.3초 동안 사라짐
                float blink = 0.55f + 0.45f * Mathf.Cos(warningTimer * Mathf.PI * 2f * 1.5f);
                float fadeOut = Mathf.Clamp01((warningDuration - warningTimer) / 0.3f);
                warningGroup.alpha = blink * fadeOut;
            }
        }
    }
}
