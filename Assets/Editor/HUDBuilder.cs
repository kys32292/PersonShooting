using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    // 2026-10-06: 아래부터 PlayerTestScene 전용 HUD 생성/자동 연결 기능 추가.
    // Main.unity용 BuildGameplayHUD()는 그대로 두고, 같은 스타일(체력/경험치 바)을
    // TMP_Text로 다시 만들어서 PlayerMove/GameManager 필드에 SerializedObject로 직접 연결함.
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

    // 2026-10-06: 기존에 씬에 있던 AmmoCount 텍스트를 PlayerMove.ammoText에 연결만 해주는 도구
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

    // 2026-10-06: PlayerTestScene에서 작업한 Player/GameManager/PerkUI/체력-경험치 바/스폰포인트를
    // Main.unity로 옮기는 도구. Main의 바닥/벽 등 기존 맵 지오메트리는 전혀 건드리지 않고,
    // 중복되는 예전 legacy Text 체력/경험치 바(TopCenter_HealthExp)만 새 TMP 버전으로 교체한다.
    // 탄환 UI는 Main에 있던 AmmoCurrent/AmmoReserve 디자인을 그대로 유지하되, 코드와 연결하기 위해
    // 컴포넌트만 legacy Text -> TextMeshProUGUI로 바꾼다(위치/크기/글자는 그대로).
    [MenuItem("Tools/HUD/Migrate PlayerTestScene Into Main")]
    public static void MigratePlayerTestSceneIntoMain()
    {
        var mainScene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

        // Main만 로드된 상태에서 먼저 처리해야 이름이 겹치는 PlayerTestScene 쪽 오브젝트와 혼동되지 않음
        GameObject oldTopCenter = GameObject.Find("TopCenter_HealthExp");
        if (oldTopCenter != null)
        {
            Object.DestroyImmediate(oldTopCenter);
        }

        TMP_Text ammoCurrentTMP = ConvertTextToTMP(GameObject.Find("AmmoCurrent"));
        TMP_Text ammoReserveTMP = ConvertTextToTMP(GameObject.Find("AmmoReserve"));
        GameObject mainHudRoot = GameObject.Find("HUD");

        var testScene = EditorSceneManager.OpenScene("Assets/JCH_FPS/Scenes/PlayerTestScene.unity", OpenSceneMode.Additive);

        GameObject player = FindRootInScene(testScene, "Player");
        GameObject gameManager = FindRootInScene(testScene, "GameManager");
        GameObject spawnPoints = FindRootInScene(testScene, "SpawnPoints");
        GameObject testCanvas = FindRootInScene(testScene, "Canvas");

        Transform perkUI = testCanvas != null ? testCanvas.transform.Find("PerkUI") : null;
        Transform hudHealthExp = testCanvas != null ? testCanvas.transform.Find("HUD_HealthExp") : null;

        if (perkUI != null && mainHudRoot != null)
        {
            SceneManager.MoveGameObjectToScene(perkUI.gameObject, mainScene);
            perkUI.SetParent(mainHudRoot.transform, false);
        }

        if (hudHealthExp != null && mainHudRoot != null)
        {
            SceneManager.MoveGameObjectToScene(hudHealthExp.gameObject, mainScene);
            hudHealthExp.SetParent(mainHudRoot.transform, false);
        }

        if (player != null) SceneManager.MoveGameObjectToScene(player, mainScene);
        if (gameManager != null) SceneManager.MoveGameObjectToScene(gameManager, mainScene);
        if (spawnPoints != null) SceneManager.MoveGameObjectToScene(spawnPoints, mainScene);

        if (player != null)
        {
            // PlayerTestScene용 구 탄환 텍스트(AmmoCount)는 Main 자체 탄환 UI로 대체되므로 비활성화만 함
            Transform oldAmmoCount = null;
            foreach (var t in player.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "AmmoCount") { oldAmmoCount = t; break; }
            }
            if (oldAmmoCount != null)
            {
                oldAmmoCount.gameObject.SetActive(false);
            }

            var playerMove = player.GetComponent<PlayerMove>();
            if (playerMove != null)
            {
                var so = new SerializedObject(playerMove);
                so.FindProperty("ammoText").objectReferenceValue = ammoCurrentTMP;
                so.FindProperty("ammoMaxText").objectReferenceValue = ammoReserveTMP;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        else
        {
            Debug.LogWarning("PlayerTestScene에서 Player를 찾지 못했습니다.");
        }

        // PlayerTestScene.unity 파일 자체는 그대로 보존 (옮기고 남은 빈 상태를 저장하지 않음)
        EditorSceneManager.CloseScene(testScene, true);

        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.SaveScene(mainScene);
        Debug.Log("PlayerTestScene의 작업 내용을 Assets/Scenes/Main.unity로 이동 완료.");
    }

    // 2026-10-06: MigratePlayerTestSceneIntoMain() 보정용. PlayerTestScene의 "Canvas"가 실제로는
    // Player 프리팹 내부 계층에 매달려 있던 오브젝트라 루트 탐색에 안 걸렸고, 그 결과 PerkUI/HUD_HealthExp가
    // Player 안쪽 깊숙이 끌려 들어갔음. 이걸 Main의 HUDCanvas/HUD 밑으로 바로 옮기고, 비어버린
    // 껍데기 Canvas는 제거한다. Main.unity를 이미 연 상태에서 실행.
    [MenuItem("Tools/HUD/Fix Main HUD Hierarchy (Run After Migrate)")]
    public static void FixMainHudHierarchyAfterMigrate()
    {
        var mainScene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

        GameObject hudRoot = GameObject.Find("HUD");
        GameObject perkUI = GameObject.Find("PerkUI");
        GameObject hudHealthExp = GameObject.Find("HUD_HealthExp");

        if (hudRoot == null)
        {
            Debug.LogWarning("Main.unity에서 HUD를 찾지 못했습니다.");
            return;
        }

        if (perkUI != null)
        {
            Transform strayCanvas = perkUI.transform.parent; // Player 안쪽 껍데기 Canvas
            perkUI.transform.SetParent(hudRoot.transform, false);

            if (strayCanvas != null && strayCanvas.name == "Canvas" && strayCanvas.childCount == 0)
            {
                Object.DestroyImmediate(strayCanvas.gameObject);
            }
        }

        if (hudHealthExp != null)
        {
            Transform strayCanvas = hudHealthExp.transform.parent;
            hudHealthExp.transform.SetParent(hudRoot.transform, false);

            if (strayCanvas != null && strayCanvas.name == "Canvas" && strayCanvas.childCount == 0)
            {
                Object.DestroyImmediate(strayCanvas.gameObject);
            }
        }

        EditorSceneManager.MarkSceneDirty(mainScene);
        EditorSceneManager.SaveScene(mainScene);
        Debug.Log("Main HUD 계층 보정 완료: PerkUI/HUD_HealthExp -> HUDCanvas/HUD");
    }

    // 2026-10-21: ESC 일시정지 메뉴 생성 도구. HUDCanvas 맨 위(마지막 자식)에 PauseMenu를 만들고
    // PauseMenuController 필드를 자동 연결한다. 한글 표시를 위해 TMP 대신 legacy Text(OS 폰트 폴백) 사용.
    // "메인 메뉴로" 버튼이 동작하도록 Title.unity를 빌드 세팅에 없으면 추가한다.
    static readonly Color PauseDim = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color PausePanelBG = new Color(0.086f, 0.098f, 0.122f, 0.98f);
    static readonly Color PausePanelBorder = new Color(0.22f, 0.24f, 0.28f, 1f);
    static readonly Color PauseButtonBG = new Color(0.157f, 0.173f, 0.204f, 1f);
    static readonly Color PauseButtonHover = new Color(0.22f, 0.24f, 0.28f, 1f);
    static readonly Color PauseButtonBorder = new Color(0.23f, 0.25f, 0.29f, 1f);
    static readonly Color PausePrimaryBG = new Color(0.23f, 0.33f, 0.28f, 1f);
    static readonly Color PausePrimaryHover = new Color(0.28f, 0.40f, 0.33f, 1f);
    static readonly Color PausePrimaryBorder = new Color(0.37f, 0.68f, 0.48f, 1f);
    static readonly Color PauseSubText = new Color(0.55f, 0.58f, 0.62f, 1f);

    [MenuItem("Tools/HUD/Build Pause Menu (Main)")]
    public static void BuildPauseMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

        GameObject canvasGO = GameObject.Find("HUDCanvas");
        if (canvasGO == null)
        {
            Debug.LogWarning("Main.unity에서 HUDCanvas를 찾지 못했습니다. 먼저 Build Gameplay HUD를 실행하세요.");
            return;
        }

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Clear previous pause menu if it exists, so re-running the tool is idempotent
        var existing = canvasGO.transform.Find("PauseMenu");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        RectTransform root = CreateUIObject("PauseMenu", canvasGO.transform);
        Stretch(root);
        root.SetAsLastSibling();
        var controller = root.gameObject.AddComponent<PauseMenuController>();

        // ---- 화면 어둡게 + 클릭 차단 ----
        RectTransform overlay = CreateUIObject("PausePanel", root);
        Stretch(overlay);
        overlay.gameObject.AddComponent<Image>().color = PauseDim;

        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // ---- 가운데 창 (테두리 + 안쪽 배경) ----
        RectTransform window = CreateUIObject("Window", overlay);
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(560, 0);
        var windowBorder = window.gameObject.AddComponent<Image>();
        windowBorder.sprite = rounded;
        windowBorder.type = Image.Type.Sliced;
        windowBorder.color = PausePanelBorder;
        window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 28, 20);
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform windowBG = CreateUIObject("Background", window);
        Stretch(windowBG);
        windowBG.offsetMin = new Vector2(1, 1);
        windowBG.offsetMax = new Vector2(-1, -1);
        windowBG.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var windowBGImg = windowBG.gameObject.AddComponent<Image>();
        windowBGImg.sprite = rounded;
        windowBGImg.type = Image.Type.Sliced;
        windowBGImg.color = PausePanelBG;

        // ---- 제목 ----
        RectTransform header = CreateUIObject("Header", window);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
        CreateText(header, "Title", "일시정지", 34, TextWhite, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(400, 44));
        CreateText(header, "Subtitle", "PAUSED", 14, PauseSubText, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(400, 20));

        // ---- 버튼 ----
        Button resume = CreatePauseButton(window, rounded, "ResumeButton", "게임 계속하기", PausePrimaryBG, PausePrimaryHover, PausePrimaryBorder);
        Button settings = CreatePauseButton(window, rounded, "SettingsButton", "설정", PauseButtonBG, PauseButtonHover, PauseButtonBorder);
        Button mainMenu = CreatePauseButton(window, rounded, "MainMenuButton", "메인 메뉴로 돌아가기", PauseButtonBG, PauseButtonHover, PauseButtonBorder);
        Button quit = CreatePauseButton(window, rounded, "QuitButton", "게임 종료", PauseButtonBG, PauseButtonHover, PauseButtonBorder);

        // ---- 하단 안내 ----
        RectTransform footer = CreateUIObject("Footer", window);
        footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
        CreateText(footer, "Hint", "ESC — 게임으로 돌아가기", 15, PauseSubText, FontStyle.Normal, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var so = new SerializedObject(controller);
        so.FindProperty("pausePanel").objectReferenceValue = overlay.gameObject;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("mainMenuButton").objectReferenceValue = mainMenu;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 에디터에서 보기 편하도록 생성 직후엔 꺼둠 (런타임 Awake에서도 꺼짐)
        overlay.gameObject.SetActive(false);

        AddSceneToBuildSettings("Assets/Scenes/Title.unity");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("일시정지 메뉴 생성 및 연결 완료: Assets/Scenes/Main.unity (HUDCanvas/PauseMenu)");
    }

    static Button CreatePauseButton(RectTransform parent, Sprite sprite, string name, string label, Color normal, Color hover, Color border)
    {
        RectTransform rt = CreateUIObject(name, parent);
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
        var borderImg = rt.gameObject.AddComponent<Image>();
        borderImg.sprite = sprite;
        borderImg.type = Image.Type.Sliced;
        borderImg.color = border;

        RectTransform bg = CreateUIObject("Background", rt);
        Stretch(bg);
        bg.offsetMin = new Vector2(1, 1);
        bg.offsetMax = new Vector2(-1, -1);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = sprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white; // 실제 색은 Button ColorBlock 틴트로 결정

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = bgImg;
        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = hover;
        colors.selectedColor = hover;
        colors.pressedColor = normal * 0.85f;
        colors.disabledColor = normal * 0.6f;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        CreateText(bg, "Label", label, 20, TextWhite, FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return button;
    }

    static void AddSceneToBuildSettings(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;

        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static GameObject FindRootInScene(Scene scene, string name)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == name) return go;
        }
        return null;
    }

    // 2026-10-06: 위치/크기/색/글자는 그대로 두고 legacy UI.Text -> TextMeshProUGUI로 컴포넌트만 교체
    static TMP_Text ConvertTextToTMP(GameObject go)
    {
        if (go == null) return null;

        var legacy = go.GetComponent<Text>();
        if (legacy == null)
        {
            return go.GetComponent<TMP_Text>();
        }

        string content = legacy.text;
        float fontSize = legacy.fontSize;
        Color color = legacy.color;
        FontStyles style = legacy.fontStyle switch
        {
            FontStyle.Bold => FontStyles.Bold,
            FontStyle.Italic => FontStyles.Italic,
            FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
            _ => FontStyles.Normal,
        };
        TextAlignmentOptions alignment = legacy.alignment switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
            TextAnchor.MiddleCenter => TextAlignmentOptions.Midline,
            TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Midline,
        };

        Object.DestroyImmediate(legacy);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    // 2026-10-06: PlayerTestScene용 체력/경험치 바. BuildTopCenter()(legacy Text, Main.unity용)와
    // 거의 같은 레이아웃이지만 TMP_Text로 만들어서 PlayerMove.hpText / GameManager.levelText,
    // expValueText 필드 타입과 맞춤. 크기는 기존 대비 20% 확대, EXPLabel은 "Lv.N"으로 동적 표시,
    // EXPValue는 바 가운데에 "현재/다음레벨" 진행치를 표시.
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

    // 2026-10-06: CreateText()의 TextMeshProUGUI 버전
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
