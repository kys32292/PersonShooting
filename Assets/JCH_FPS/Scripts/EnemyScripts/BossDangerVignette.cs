// 11/19 - 화면 가장자리 위험 경고 (QA 피드백: FPS 시점에서 발밑 장판이 잘 안 보임)
// 플레이어가 '경고 중인' 보스 장판 안에 서 있으면 화면 가장자리가 붉게 맥동하고, 발동이 가까울수록 진하고 빨라짐
// 씬에 따로 배치하지 않음: 첫 장판이 생길 때 BossHazardZone이 Ensure()로 오버레이 캔버스를 자동 생성
using UnityEngine;
using UnityEngine.UI;

public class BossDangerVignette : MonoBehaviour
{
    private static BossDangerVignette instance;

    private const int TextureSize = 128;
    private static readonly Color VignetteColor = new Color(0.9f, 0.05f, 0.05f, 1f);

    private RawImage image;
    private float alpha = 0f;
    private float pulseTime = 0f;

    public static void Ensure()
    {
        if (instance != null) return;

        var go = new GameObject("Boss Danger Vignette");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // HUD 위, 일시정지/특성 선택 창보다는 아래가 되도록 적당히

        var imageGO = new GameObject("Vignette", typeof(RectTransform));
        imageGO.transform.SetParent(go.transform, false);
        var rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        instance = go.AddComponent<BossDangerVignette>();
        instance.image = imageGO.AddComponent<RawImage>();
        instance.image.texture = BuildTexture();
        instance.image.raycastTarget = false;
        instance.image.color = new Color(VignetteColor.r, VignetteColor.g, VignetteColor.b, 0f);
    }

    // 가운데는 투명, 가장자리로 갈수록 진해지는 텍스처 (화면 비율에 맞게 늘어남)
    private static Texture2D BuildTexture()
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float u = (x + 0.5f) / TextureSize * 2f - 1f;
                float v = (y + 0.5f) / TextureSize * 2f - 1f;
                float distance = Mathf.Sqrt(u * u + v * v) / 1.4142f; // 모서리 = 1
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, distance));
                pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;

        if (image != null && image.texture != null)
        {
            Destroy(image.texture);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            return;
        }

        float progress = BossHazardZone.GetPlayerWarningProgress(); // -1 = 위험 없음

        float target = 0f;
        if (progress >= 0f)
        {
            pulseTime += Time.deltaTime * Mathf.Lerp(3f, 9f, progress); // 발동이 가까울수록 빠르게
            float pulse = 0.5f + 0.5f * Mathf.Sin(pulseTime * Mathf.PI * 2f);
            target = Mathf.Lerp(0.35f, 0.85f, progress) * Mathf.Lerp(0.6f, 1f, pulse);
        }

        // 켜질 때는 즉시, 꺼질 때는 부드럽게
        alpha = target > alpha ? target : Mathf.MoveTowards(alpha, target, Time.deltaTime * 4f);
        image.color = new Color(VignetteColor.r, VignetteColor.g, VignetteColor.b, alpha);
    }
}
