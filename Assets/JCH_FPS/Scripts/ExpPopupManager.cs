using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 11/09 - 획득 경험치 연출
// 1. 적이 죽은 월드 좌표를 화면 좌표로 바꿔 경험치 수치 표시 (textDuration)
// 2. 수치가 이미지로 바뀌어 경험치 바까지 S자 곡선으로 이동 후 사라짐 (flyDuration)
// 3. 도착하면 onArrive 호출 (GameManager가 경험치 바 갱신)
// GameManager.Awake에서 자동으로 붙고 Init으로 캔버스/도착점(경험치 바)이 설정된다.
public class ExpPopupManager : MonoBehaviour
{
    [Header("연결 (비워두면 GameManager가 경험치 바 기준으로 설정)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform target;

    [Header("이미지 (비워두면 노란 원 사용)")]
    [SerializeField] private Sprite orbSprite;
    [SerializeField] private Vector2 orbSize = new Vector2(28f, 28f);
    [SerializeField] private Color orbColor = new Color(1f, 0.82f, 0.2f, 1f);

    [Header("수치 텍스트")]
    [SerializeField] private float fontSize = 40f;
    [SerializeField] private Color textColor = new Color(1f, 0.85f, 0.25f, 1f);

    [Header("시간")]
    [SerializeField] private float textDuration = 0.1f;
    [SerializeField] private float flyDuration = 0.2f;

    [Header("S자 곡선 휨 정도 (이동 거리 대비 비율)")]
    [SerializeField] private float curveStrength = 0.3f;

    private static Sprite defaultOrbSprite;

    public void Init(Canvas _canvas, RectTransform _target)
    {
        if (canvas == null) canvas = _canvas != null ? _canvas.rootCanvas : null;
        if (target == null) target = _target;
    }

    public void Play(float amount, Vector3 worldPosition, Action onArrive)
    {
        Camera cam = Camera.main;

        // 연출을 할 수 없는 상황이면 바로 바 갱신
        if (canvas == null || target == null || cam == null || !isActiveAndEnabled)
        {
            onArrive?.Invoke();
            return;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        if (screenPos.z < 0f) // 카메라 뒤에서 죽은 경우 화면 중앙에서 시작
        {
            screenPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }

        StartCoroutine(PlayRoutine(amount, screenPos, onArrive));
    }

    private IEnumerator PlayRoutine(float amount, Vector2 screenPos, Action onArrive)
    {
        RectTransform canvasRect = (RectTransform)canvas.transform;
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 start);

        // 1. 수치 표시 ------------------------------------------
        RectTransform textRect = CreateUIObject("ExpPopupText", canvasRect, start);
        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = "+" + Mathf.RoundToInt(amount);
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        textRect.sizeDelta = new Vector2(200f, 60f);

        float t = 0f;
        while (t < textDuration)
        {
            if (!IsPaused())
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t / textDuration)); // 살짝 커지면서 등장
                textRect.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        Destroy(textRect.gameObject);

        // 2. 이미지로 바뀌어 S자 곡선 이동 -------------------------
        RectTransform orbRect = CreateUIObject("ExpPopupOrb", canvasRect, start);
        Image orb = orbRect.gameObject.AddComponent<Image>();
        orb.sprite = orbSprite != null ? orbSprite : GetDefaultOrbSprite();
        orb.color = orbSprite != null ? Color.white : orbColor;
        orb.raycastTarget = false;
        orbRect.sizeDelta = orbSize;

        t = 0f;
        while (t < flyDuration)
        {
            if (!IsPaused())
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / flyDuration);

                // 경험치 바가 움직이지 않더라도 매 프레임 도착점을 다시 계산 (해상도 변경 대비)
                Vector2 end = GetTargetLocalPoint(canvasRect, uiCam);
                orbRect.anchoredPosition = SCurve(start, end, p);
                orbRect.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, p);
            }
            yield return null;
        }

        Destroy(orbRect.gameObject);

        // 3. 도착 -> 경험치 바 갱신 ----------------------------------
        onArrive?.Invoke();
    }

    // 시작점 -> 끝점 S자 곡선 (3차 베지어, 두 제어점을 서로 반대 방향으로 휘게 함)
    private Vector2 SCurve(Vector2 start, Vector2 end, float p)
    {
        Vector2 dir = end - start;
        Vector2 normal = new Vector2(-dir.y, dir.x).normalized * dir.magnitude * curveStrength;

        Vector2 c1 = start + dir / 3f + normal;
        Vector2 c2 = start + dir * 2f / 3f - normal;

        float u = 1f - p;
        return u * u * u * start + 3f * u * u * p * c1 + 3f * u * p * p * c2 + p * p * p * end;
    }

    private Vector2 GetTargetLocalPoint(RectTransform canvasRect, Camera uiCam)
    {
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(uiCam, target.TransformPoint(target.rect.center));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, uiCam, out Vector2 local);
        return local;
    }

    private RectTransform CreateUIObject(string name, RectTransform parent, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.SetAsLastSibling(); // 다른 UI 위에 그려지도록
        return rect;
    }

    private bool IsPaused()
    {
        return GameManager.Instance != null && GameManager.Instance.isPaused;
    }

    // 이미지가 지정되지 않았을 때 쓰는 원 모양 스프라이트 (한 번만 생성)
    private static Sprite GetDefaultOrbSprite()
    {
        if (defaultOrbSprite != null) return defaultOrbSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01(radius - d); // 가장자리 부드럽게
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        defaultOrbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return defaultOrbSprite;
    }
}
