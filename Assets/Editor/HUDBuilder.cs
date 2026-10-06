using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HUDBuilder
{
    static readonly Color PanelBG = new Color(0.05f, 0.05f, 0.05f, 0.75f);
    static readonly Color HealthBG = new Color(0.10f, 0.02f, 0.02f, 0.85f);
    static readonly Color HealthFill = new Color(0.85f, 0.08f, 0.08f, 1f);
    static readonly Color ExpBG = new Color(0.06f, 0.06f, 0.06f, 0.8f);
    static readonly Color ExpFill = new Color(0.95f, 0.72f, 0.10f, 1f);
    static readonly Color TextWhite = new Color(0.95f, 0.95f, 0.95f, 1f);
    static readonly Color TextGray = new Color(0.75f, 0.75f, 0.75f, 1f);

    [MenuItem("Tools/HUD/Build Gameplay HUD")]
    public static void BuildGameplayHUD()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        // Canvas
        GameObject canvasGO = GameObject.Find("HUDCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // EventSystem
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Clear previous HUD root if it exists, so re-running the tool is idempotent
        var existingHud = canvasGO.transform.Find("HUD");
        if (existingHud != null) Object.DestroyImmediate(existingHud.gameObject);

        RectTransform hudRoot = CreateUIObject("HUD", canvasGO.transform);
        Stretch(hudRoot);

        BuildTopCenter(hudRoot);
        BuildBottomRightAmmo(hudRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("HUD built and scene saved: Assets/Scenes/Main.unity");
    }

    [MenuItem("Tools/HUD/Build Gameplay HUD (PlayerTestScene)")]
    public static void BuildGameplayHUD_PlayerTestScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/JCH_FPS/Scenes/PlayerTestScene.unity");

        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        }

        // Clear previous HUD root if it exists, so re-running the tool is idempotent
        var existingHud = canvasGO.transform.Find("HUD_HealthExp");
        if (existingHud != null) Object.DestroyImmediate(existingHud.gameObject);

        RectTransform hudRoot = CreateUIObject("HUD_HealthExp", canvasGO.transform);
        Stretch(hudRoot);

        BuildTopCenterTMP(hudRoot, out Image healthFillImg, out TMP_Text healthValueTMP, out Image expFillImg, out TMP_Text levelValueTMP, out TMP_Text expValueTMP);

        var player = Object.FindObjectOfType<PlayerMove>();
        if (player != null)
        {
            var so = new SerializedObject(player);
            so.FindProperty("hpBar").objectReferenceValue = healthFillImg;
            so.FindProperty("hpText").objectReferenceValue = healthValueTMP;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("PlayerMove를 씬에서 찾지 못해 체력 UI를 연결하지 못했습니다.");
        }

        var gm = Object.FindObjectOfType<GameManager>();
        if (gm != null)
        {
            var so = new SerializedObject(gm);
            so.FindProperty("expBar").objectReferenceValue = expFillImg;
            so.FindProperty("levelText").objectReferenceValue = levelValueTMP;
            so.FindProperty("expValueText").objectReferenceValue = expValueTMP;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("GameManager를 씬에서 찾지 못해 경험치 UI를 연결하지 못했습니다.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("HUD(체력/경험치) 생성 및 연결 완료: Assets/JCH_FPS/Scenes/PlayerTestScene.unity");
    }

    [MenuItem("Tools/HUD/Wire Ammo UI (PlayerTestScene)")]
    public static void WireAmmoUI_PlayerTestScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/JCH_FPS/Scenes/PlayerTestScene.unity");

        var ammoGO = GameObject.Find("AmmoCount");
        var player = Object.FindObjectOfType<PlayerMove>();

        if (ammoGO == null)
        {
            Debug.LogWarning("AmmoCount 오브젝트를 씬에서 찾지 못해 탄환 UI를 연결하지 못했습니다.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("PlayerMove를 씬에서 찾지 못해 탄환 UI를 연결하지 못했습니다.");
            return;
        }

        var ammoTMP = ammoGO.GetComponent<TMP_Text>();
        var so = new SerializedObject(player);
        so.FindProperty("ammoText").objectReferenceValue = ammoTMP;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("탄환 UI(AmmoCount -> ammoText) 연결 완료: Assets/JCH_FPS/Scenes/PlayerTestScene.unity");
    }

    static void BuildTopCenterTMP(RectTransform hudRoot, out Image healthFillImg, out TMP_Text healthValueTMP, out Image expFillImg, out TMP_Text levelValueTMP, out TMP_Text expValueTMP)
    {
        // Health/Exp bars sized 20% larger than the original prototype layout.
        RectTransform group = CreateUIObject("TopCenter_HealthExp", hudRoot);
        group.anchorMin = group.anchorMax = new Vector2(0.5f, 1f);
        group.pivot = new Vector2(0.5f, 1f);
        group.sizeDelta = new Vector2(456, 66);
        group.anchoredPosition = new Vector2(0, -22);

        // ---- Health bar ----
        RectTransform healthBG = CreateBar("HealthBar_BG", group, HealthBG, new Vector2(432, 29), new Vector2(0, 0));
        healthFillImg = CreateFillBar(healthBG, "HealthBar_Fill", HealthFill, 1f);

        CreateTextTMP(healthBG, "HealthLabel", "HP", 15, TextGray, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(60, 24));
        healthValueTMP = CreateTextTMP(healthBG, "HealthValue", "100", 19, TextWhite, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(84, 24));

        // ---- Exp bar (below health bar) ----
        RectTransform expBG = CreateBar("EXPBar_BG", group, ExpBG, new Vector2(432, 12), new Vector2(0, -36));
        expFillImg = CreateFillBar(expBG, "EXPBar_Fill", ExpFill, 0f);

        levelValueTMP = CreateTextTMP(expBG, "EXPLabel", "Lv. 1", 10, TextGray, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6, 0), new Vector2(50, 12));

        // Centered "현재 경험치 / 레벨업 필요치" progress text
        expValueTMP = CreateTextTMP(expBG, "EXPValue", "0 / 100", 9, TextWhite, FontStyles.Bold, TextAlignmentOptions.Midline,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 12));
    }

    static TMP_Text CreateTextTMP(RectTransform parent, string name, string content, float fontSize, Color color, FontStyles style, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        RectTransform rt = CreateUIObject(name, parent);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static void BuildTopCenter(RectTransform hudRoot)
    {
        RectTransform group = CreateUIObject("TopCenter_HealthExp", hudRoot);
        group.anchorMin = group.anchorMax = new Vector2(0.5f, 1f);
        group.pivot = new Vector2(0.5f, 1f);
        group.sizeDelta = new Vector2(520, 70);
        group.anchoredPosition = new Vector2(0, -24);

        // ---- Health bar ----
        RectTransform healthBG = CreateBar("HealthBar_BG", group, HealthBG, new Vector2(480, 30), new Vector2(0, 0));
        Image healthFillImg = CreateFillBar(healthBG, "HealthBar_Fill", HealthFill, 1f);

        CreateText(healthBG, "HealthLabel", "HP", 16, TextGray, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(60, 26));
        CreateText(healthBG, "HealthValue", "100", 20, TextWhite, FontStyle.Bold, TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10, 0), new Vector2(80, 26));

        // ---- Exp bar (below health bar) ----
        RectTransform expBG = CreateBar("EXPBar_BG", group, ExpBG, new Vector2(480, 12), new Vector2(0, -38));
        Image expFillImg = CreateFillBar(expBG, "EXPBar_Fill", ExpFill, 0.35f);

        CreateText(expBG, "EXPLabel", "EXP", 10, TextGray, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 12));
    }

    static void BuildBottomRightAmmo(RectTransform hudRoot)
    {
        RectTransform ammoBG = CreateUIObject("BottomRight_Ammo", hudRoot);
        ammoBG.anchorMin = ammoBG.anchorMax = new Vector2(1f, 0f);
        ammoBG.pivot = new Vector2(1f, 0f);
        ammoBG.sizeDelta = new Vector2(240, 96);
        ammoBG.anchoredPosition = new Vector2(-40, 40);
        var bgImg = ammoBG.gameObject.AddComponent<Image>();
        bgImg.color = PanelBG;

        CreateText(ammoBG, "WeaponLabel", "PISTOL", 14, TextGray, FontStyle.Bold, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(220, 20));

        CreateText(ammoBG, "AmmoCurrent", "24", 46, TextWhite, FontStyle.Bold, TextAnchor.LowerRight,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-6, 10), new Vector2(90, 54));

        CreateText(ammoBG, "AmmoSlash", "/", 24, TextGray, FontStyle.Bold, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(20, 30));

        CreateText(ammoBG, "AmmoReserve", "120", 22, TextGray, FontStyle.Bold, TextAnchor.LowerLeft,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(6, 16), new Vector2(70, 28));
    }

    // ---------- helpers ----------

    static RectTransform CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static RectTransform CreateBar(string name, RectTransform parent, Color color, Vector2 size, Vector2 anchoredPos)
    {
        RectTransform rt = CreateUIObject(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    static Image CreateFillBar(RectTransform parent, string name, Color color, float fillAmount)
    {
        RectTransform rt = CreateUIObject(name, parent);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(2, 2);
        rt.offsetMax = new Vector2(-2, -2);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = fillAmount;
        return img;
    }

    static Text CreateText(RectTransform parent, string name, string content, int fontSize, Color color, FontStyle style, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        RectTransform rt = CreateUIObject(name, parent);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var text = rt.gameObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
