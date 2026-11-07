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
        // 2026-11-01: 타이틀과 같은 설정 창을 일시정지 메뉴 위에 띄움
        so.FindProperty("settingsPanel").objectReferenceValue = BuildSettingsPanel(root, rounded);
        so.ApplyModifiedPropertiesWithoutUndo();

        // 에디터에서 보기 편하도록 생성 직후엔 꺼둠 (런타임 Awake에서도 꺼짐)
        overlay.gameObject.SetActive(false);

        AddSceneToBuildSettings("Assets/Scenes/Title.unity");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("일시정지 메뉴 생성 및 연결 완료: Assets/Scenes/Main.unity (HUDCanvas/PauseMenu)");
    }

    // 2026-11-01: 타이틀 화면 생성 도구. Title.unity에 TitleCanvas(배경/제목/게임 시작·설정·종료 버튼/설정 창)를 만들고
    // TitleMenuController 필드를 자동 연결한다. 배경 이미지는 코드로 그려 Assets/Art/UI/TitleBG.png로 저장한다.
    // 실행 파일이 타이틀부터 시작하도록 Title.unity를 빌드 세팅 0번으로 옮긴다.
    const string TitleScenePath = "Assets/Scenes/Title.unity";
    const string TitleBGPath = "Assets/Art/UI/TitleBG.png";
    static readonly Color TitleAccent = new Color(0.85f, 0.12f, 0.10f, 1f);
    static readonly Color TitlePrimaryBG = new Color(0.45f, 0.09f, 0.08f, 1f);
    static readonly Color TitlePrimaryHover = new Color(0.60f, 0.12f, 0.10f, 1f);
    static readonly Color TitlePrimaryBorder = new Color(0.90f, 0.25f, 0.20f, 1f);

    [MenuItem("Tools/HUD/Build Title Menu")]
    public static void BuildTitleMenu()
    {
        Sprite bgSprite = GenerateTitleBackground(TitleBGPath);

        var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        var cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        // Clear previous title canvas if it exists, so re-running the tool is idempotent
        var existing = FindRootInScene(scene, "TitleCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        var canvasGO = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGO.GetComponent<RectTransform>();
        var controller = canvasGO.AddComponent<TitleMenuController>();

        // ---- 배경 (화면 비율이 달라도 꽉 차게) ----
        RectTransform bg = CreateUIObject("Background", root);
        bg.anchorMin = bg.anchorMax = bg.pivot = new Vector2(0.5f, 0.5f);
        bg.gameObject.AddComponent<Image>().sprite = bgSprite;
        var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;

        // ---- 제목 ----
        RectTransform titleGroup = CreateUIObject("TitleGroup", root);
        titleGroup.anchorMin = titleGroup.anchorMax = titleGroup.pivot = new Vector2(0f, 0.5f);
        titleGroup.anchoredPosition = new Vector2(160, 200);
        titleGroup.sizeDelta = new Vector2(1000, 200);

        Text title = CreateText(titleGroup, "GameTitle", "PERSON<color=#D91F1A>SHOOTING</color>", 124, TextWhite, FontStyle.Bold, TextAnchor.LowerLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1000, 140));
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(4, -4);

        RectTransform accent = CreateUIObject("AccentBar", titleGroup);
        accent.anchorMin = accent.anchorMax = accent.pivot = new Vector2(0f, 1f);
        accent.anchoredPosition = new Vector2(6, -152);
        accent.sizeDelta = new Vector2(140, 5);
        accent.gameObject.AddComponent<Image>().color = TitleAccent;

        CreateText(titleGroup, "Subtitle", "WAVE SURVIVAL  ·  FIRST PERSON SHOOTER", 20, PauseSubText, FontStyle.Bold, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6, -170), new Vector2(800, 28));

        // ---- 메뉴 버튼 ----
        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        RectTransform menu = CreateUIObject("Menu", root);
        menu.anchorMin = menu.anchorMax = new Vector2(0f, 0.5f);
        menu.pivot = new Vector2(0f, 1f);
        menu.anchoredPosition = new Vector2(166, 30);
        menu.sizeDelta = new Vector2(400, 0);
        menu.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var menuLayout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
        menuLayout.spacing = 14;
        menuLayout.childControlWidth = true;
        menuLayout.childControlHeight = true;
        menuLayout.childForceExpandWidth = true;
        menuLayout.childForceExpandHeight = false;

        Button start = CreatePauseButton(menu, rounded, "StartButton", "게임 시작", TitlePrimaryBG, TitlePrimaryHover, TitlePrimaryBorder);
        Button settings = CreatePauseButton(menu, rounded, "SettingsButton", "설정", PauseButtonBG, PauseButtonHover, PauseButtonBorder);
        Button quit = CreatePauseButton(menu, rounded, "QuitButton", "종료", PauseButtonBG, PauseButtonHover, PauseButtonBorder);

        CreateText(root, "Version", "v" + PlayerSettings.bundleVersion, 16, PauseSubText, FontStyle.Normal, TextAnchor.LowerRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30, 24), new Vector2(300, 24));

        // ---- 설정 창 ----
        GameObject settingsPanel = BuildSettingsPanel(root, rounded);

        var so = new SerializedObject(controller);
        so.FindProperty("startButton").objectReferenceValue = start;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 타이틀을 빌드 세팅 맨 앞으로 (실행 시 첫 씬)
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == TitleScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(TitleScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("타이틀 화면 생성 및 연결 완료: " + TitleScenePath + " (TitleCanvas)");
    }

    // 타이틀/일시정지 메뉴 공용 설정 창. SettingsPanelController까지 붙여 연결하고 꺼진 상태로 반환한다.
    static GameObject BuildSettingsPanel(RectTransform root, Sprite rounded)
    {
        RectTransform overlay = CreateUIObject("SettingsPanel", root);
        Stretch(overlay);
        overlay.gameObject.AddComponent<Image>().color = PauseDim;

        RectTransform window = CreateUIObject("Window", overlay);
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(640, 0);
        var windowBorder = window.gameObject.AddComponent<Image>();
        windowBorder.sprite = rounded;
        windowBorder.type = Image.Type.Sliced;
        windowBorder.color = PausePanelBorder;
        window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 24);
        layout.spacing = 12;
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

        RectTransform header = CreateUIObject("Header", window);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
        CreateText(header, "Title", "설정", 34, TextWhite, FontStyle.Bold, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(400, 44));
        CreateText(header, "Subtitle", "SETTINGS", 14, PauseSubText, FontStyle.Normal, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(400, 20));

        var res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = rounded,
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
        };

        Slider sensSlider = CreateSettingSlider(window, res, "MouseSensitivity", "마우스 감도", out Text sensValue);
        Slider volSlider = CreateSettingSlider(window, res, "MasterVolume", "마스터 볼륨", out Text volValue);

        RectTransform fsRow = CreateSettingRow(window, "Fullscreen", "전체 화면");
        GameObject toggleGO = DefaultControls.CreateToggle(res);
        toggleGO.name = "Toggle";
        Object.DestroyImmediate(toggleGO.transform.Find("Label").gameObject);
        var toggleRT = toggleGO.GetComponent<RectTransform>();
        toggleRT.SetParent(fsRow, false);
        toggleRT.anchorMin = toggleRT.anchorMax = toggleRT.pivot = new Vector2(1f, 0.5f);
        toggleRT.anchoredPosition = new Vector2(-4, 0);
        toggleRT.sizeDelta = new Vector2(32, 32);
        var toggleBG = toggleGO.transform.Find("Background").GetComponent<RectTransform>();
        Stretch(toggleBG);
        toggleBG.GetComponent<Image>().color = PauseButtonBG;
        var check = toggleBG.Find("Checkmark").GetComponent<Image>();
        check.color = TitlePrimaryBorder;
        Toggle fullscreen = toggleGO.GetComponent<Toggle>();

        RectTransform spacer = CreateUIObject("Spacer", window);
        spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 4;

        Button close = CreatePauseButton(window, rounded, "CloseButton", "닫기", PauseButtonBG, PauseButtonHover, PauseButtonBorder);

        RectTransform footer = CreateUIObject("Footer", window);
        footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
        CreateText(footer, "Hint", "ESC — 닫기", 15, PauseSubText, FontStyle.Normal, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var panelController = overlay.gameObject.AddComponent<SettingsPanelController>();
        var so = new SerializedObject(panelController);
        so.FindProperty("sensitivitySlider").objectReferenceValue = sensSlider;
        so.FindProperty("sensitivityValueText").objectReferenceValue = sensValue;
        so.FindProperty("volumeSlider").objectReferenceValue = volSlider;
        so.FindProperty("volumeValueText").objectReferenceValue = volValue;
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.ApplyModifiedPropertiesWithoutUndo();

        overlay.gameObject.SetActive(false);
        return overlay.gameObject;
    }

    static RectTransform CreateSettingRow(RectTransform parent, string name, string label)
    {
        RectTransform row = CreateUIObject(name + "Row", parent);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
        CreateText(row, "Label", label, 20, TextWhite, FontStyle.Bold, TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4, 0), new Vector2(200, 40));
        return row;
    }

    static Slider CreateSettingSlider(RectTransform parent, DefaultControls.Resources res, string name, string label, out Text valueText)
    {
        RectTransform row = CreateSettingRow(parent, name, label);

        valueText = CreateText(row, "Value", "", 18, TextGray, FontStyle.Bold, TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4, 0), new Vector2(64, 40));

        GameObject sliderGO = DefaultControls.CreateSlider(res);
        sliderGO.name = "Slider";
        var rt = sliderGO.GetComponent<RectTransform>();
        rt.SetParent(row, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-84, 0);
        rt.sizeDelta = new Vector2(280, 20);

        sliderGO.transform.Find("Background").GetComponent<Image>().color = PauseButtonBG;
        sliderGO.transform.Find("Fill Area/Fill").GetComponent<Image>().color = TitleAccent;
        sliderGO.transform.Find("Handle Slide Area/Handle").GetComponent<Image>().color = TextWhite;

        var slider = sliderGO.GetComponent<Slider>();
        var colors = slider.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.85f, 1f);
        colors.selectedColor = colors.highlightedColor;
        slider.colors = colors;
        return slider;
    }

    // 어두운 배경 + 붉은 지평선 빛 + 원근 바닥 격자 + 조준선 + 스캔라인/비네트를 코드로 그려 PNG로 저장
    static Sprite GenerateTitleBackground(string path)
    {
        const int W = 1920, H = 1080;
        const float horizon = 0.40f;
        var focus = new Vector2(0.70f, horizon);
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        var pixels = new Color32[W * H];
        var rng = new System.Random(1102);

        Color top = new Color(0.020f, 0.024f, 0.035f);
        Color bottom = new Color(0.060f, 0.062f, 0.075f);
        Color glow = new Color(0.80f, 0.09f, 0.06f);
        Color grid = new Color(0.55f, 0.16f, 0.13f);

        for (int y = 0; y < H; y++)
        {
            float v = (y + 0.5f) / H;
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W;
                Color c = Color.Lerp(bottom, top, v);

                // 지평선 붉은 빛
                float gx = (u - focus.x) / 0.50f;
                float gy = (v - focus.y) / 0.22f;
                c += glow * (Mathf.Exp(-(gx * gx + gy * gy) * 1.6f) * 0.55f);
                c += glow * (Mathf.Exp(-Mathf.Abs(v - horizon) * 90f) * 0.35f * Mathf.Exp(-gx * gx * 0.6f));

                // 원근 바닥 격자
                float z = horizon - v;
                if (z > 0.002f)
                {
                    float fx = (u - focus.x) * (16f / 9f) / z * 0.9f;
                    float pxX = 0.9f * (16f / 9f) / (z * W);
                    float lineX = 1f - Mathf.Clamp01(Mathf.Abs(fx - Mathf.Round(fx)) / (pxX * 2.5f));
                    float fz = 0.35f / z;
                    float pxZ = 0.35f / (z * z * H);
                    float lineZ = pxZ < 0.5f ? 1f - Mathf.Clamp01(Mathf.Abs(fz - Mathf.Round(fz)) / (pxZ * 2.5f)) : 0f;
                    float fade = Mathf.SmoothStep(0f, 0.30f, z) * 0.9f + 0.1f;
                    c += grid * (Mathf.Max(lineX, lineZ) * 0.45f * fade);
                }

                // 조준선 (원 + 십자 눈금)
                float ax = (u - focus.x) * (16f / 9f);
                float ay = v - (focus.y + 0.10f);
                float r = Mathf.Sqrt(ax * ax + ay * ay);
                float px = 1f / H;
                float ring = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.17f) / (px * 1.5f));
                float ring2 = (1f - Mathf.Clamp01(Mathf.Abs(r - 0.205f) / (px * 1.0f))) * 0.5f;
                float tick = 0f;
                if (r > 0.12f && r < 0.25f)
                {
                    tick = Mathf.Max(1f - Mathf.Clamp01(Mathf.Abs(ax) / (px * 1.5f)), 1f - Mathf.Clamp01(Mathf.Abs(ay) / (px * 1.5f)));
                }
                float dot = 1f - Mathf.Clamp01((r - px * 3f) / (px * 1.5f));
                c += new Color(0.95f, 0.35f, 0.30f) * (Mathf.Max(Mathf.Max(ring, ring2), Mathf.Max(tick, dot)) * 0.28f);

                // 왼쪽은 글자가 잘 보이도록 어둡게
                c *= Mathf.Lerp(0.45f, 1f, Mathf.SmoothStep(0.05f, 0.65f, u));

                // 비네트
                float vx = (u - 0.5f) * 1.6f, vy = (v - 0.5f) * 1.9f;
                c *= Mathf.Clamp01(1f - Mathf.Pow(vx * vx + vy * vy, 1.3f) * 0.55f);

                // 스캔라인 + 노이즈
                if (y % 3 == 0) c *= 0.88f;
                float n = ((float)rng.NextDouble() - 0.5f) * 0.025f;
                c += new Color(n, n, n);

                pixels[y * W + x] = (Color32)c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 2026-11-07: 엔딩(게임 오버) 화면 생성 도구. Ending.unity를 새로 만들고(있으면 EndingCanvas만 교체)
    // 생존 시간/점수/레벨, 선택한 빌드(특성) 목록, 타이틀로 돌아가기 버튼을 만들어 EndingController에 연결한다.
    const string EndingScenePath = "Assets/Scenes/Ending.unity";

    [MenuItem("Tools/HUD/Build Ending Scene")]
    public static void BuildEndingScene()
    {
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleBGPath);
        if (bgSprite == null) bgSprite = GenerateTitleBackground(TitleBGPath);

        Scene scene;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EndingScenePath) != null)
        {
            scene = EditorSceneManager.OpenScene(EndingScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, EndingScenePath);
        }

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        var cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        // Clear previous ending canvas if it exists, so re-running the tool is idempotent
        var existing = FindRootInScene(scene, "EndingCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        var canvasGO = new GameObject("EndingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = canvasGO.GetComponent<RectTransform>();
        var controller = canvasGO.AddComponent<EndingController>();

        // ---- 배경 (타이틀 배경 재사용 + 어둡게) ----
        RectTransform bg = CreateUIObject("Background", root);
        bg.anchorMin = bg.anchorMax = bg.pivot = new Vector2(0.5f, 0.5f);
        bg.gameObject.AddComponent<Image>().sprite = bgSprite;
        var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;

        RectTransform dim = CreateUIObject("Dim", root);
        Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        // ---- 가운데 창 ----
        RectTransform window = CreateUIObject("Window", root);
        window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
        window.sizeDelta = new Vector2(840, 0);
        var windowBorder = window.gameObject.AddComponent<Image>();
        windowBorder.sprite = rounded;
        windowBorder.type = Image.Type.Sliced;
        windowBorder.color = PausePanelBorder;
        window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 32, 32);
        layout.spacing = 18;
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
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 112;
        Text gameOver = CreateText(header, "Title", "GAME OVER", 76, TitleAccent, FontStyle.Bold, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(700, 86));
        var shadow = gameOver.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(3, -3);
        CreateText(header, "Subtitle", "이번 생존 기록", 18, PauseSubText, FontStyle.Normal, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(400, 24));

        // ---- 기록 카드 3개 ----
        RectTransform stats = CreateUIObject("Stats", window);
        stats.gameObject.AddComponent<LayoutElement>().preferredHeight = 126;
        var statsLayout = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
        statsLayout.spacing = 16;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;
        statsLayout.childForceExpandWidth = true;
        statsLayout.childForceExpandHeight = true;

        Text survivalValue = CreateStatCard(stats, rounded, "SurvivalTime", "생존 시간", "00:00");
        Text scoreValue = CreateStatCard(stats, rounded, "Score", "점수 (EXP)", "0");
        Text levelValue = CreateStatCard(stats, rounded, "Level", "도달 레벨", "Lv. 1");

        // ---- 선택한 빌드 ----
        RectTransform buildHeader = CreateUIObject("BuildHeader", window);
        buildHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 30;
        CreateText(buildHeader, "Label", "선택한 빌드", 22, TextWhite, FontStyle.Bold, TextAnchor.MiddleLeft,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(4, 0), Vector2.zero);

        RectTransform perkGrid = CreateUIObject("PerkList", window);
        var grid = perkGrid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(372, 48);
        grid.spacing = new Vector2(16, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        RectTransform template = CreateUIObject("PerkEntryTemplate", perkGrid);
        var chipImg = template.gameObject.AddComponent<Image>();
        chipImg.sprite = rounded;
        chipImg.type = Image.Type.Sliced;
        chipImg.color = PauseButtonBG;
        RectTransform chipBar = CreateUIObject("Accent", template);
        chipBar.anchorMin = new Vector2(0f, 0f);
        chipBar.anchorMax = new Vector2(0f, 1f);
        chipBar.pivot = new Vector2(0f, 0.5f);
        chipBar.offsetMin = new Vector2(0, 8);
        chipBar.offsetMax = new Vector2(4, -8);
        chipBar.gameObject.AddComponent<Image>().color = TitleAccent;
        Text chipLabel = CreateText(template, "Label", "Perk", 20, TextWhite, FontStyle.Bold, TextAnchor.MiddleLeft,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        chipLabel.rectTransform.offsetMin = new Vector2(20, 0);
        chipLabel.rectTransform.offsetMax = new Vector2(-12, 0);
        chipLabel.supportRichText = true;
        template.gameObject.SetActive(false);

        RectTransform noPerk = CreateUIObject("NoPerkText", window);
        noPerk.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
        CreateText(noPerk, "Label", "선택한 특성이 없습니다", 18, PauseSubText, FontStyle.Normal, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform spacer = CreateUIObject("Spacer", window);
        spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 4;

        Button titleButton = CreatePauseButton(window, rounded, "TitleButton", "타이틀로 돌아가기", TitlePrimaryBG, TitlePrimaryHover, TitlePrimaryBorder);

        var so = new SerializedObject(controller);
        so.FindProperty("survivalTimeText").objectReferenceValue = survivalValue;
        so.FindProperty("scoreText").objectReferenceValue = scoreValue;
        so.FindProperty("levelText").objectReferenceValue = levelValue;
        so.FindProperty("perkListRoot").objectReferenceValue = perkGrid;
        so.FindProperty("perkEntryTemplate").objectReferenceValue = template.gameObject;
        so.FindProperty("noPerkText").objectReferenceValue = noPerk.gameObject;
        so.FindProperty("titleButton").objectReferenceValue = titleButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        AddSceneToBuildSettings(EndingScenePath);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("엔딩 화면 생성 및 연결 완료: " + EndingScenePath + " (EndingCanvas)");
    }

    static Text CreateStatCard(RectTransform parent, Sprite sprite, string name, string label, string value)
    {
        RectTransform card = CreateUIObject(name + "Card", parent);
        var img = card.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = PauseButtonBG;

        CreateText(card, "Label", label, 17, PauseSubText, FontStyle.Bold, TextAnchor.UpperCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(0, 24));
        return CreateText(card, "Value", value, 44, TextWhite, FontStyle.Bold, TextAnchor.LowerCenter,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 16), new Vector2(0, 56));
    }

    // 2026-11-01: 이미 만들어진 Main의 일시정지 메뉴는 그대로 두고 설정 창만 추가/교체해서 연결
    [MenuItem("Tools/HUD/Add Settings Panel To Pause Menu (Main)")]
    public static void AddSettingsPanelToPauseMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);

        var controller = Object.FindObjectOfType<PauseMenuController>(true);
        if (controller == null)
        {
            Debug.LogWarning("Main.unity에서 PauseMenuController를 찾지 못했습니다. 먼저 Build Pause Menu (Main)를 실행하세요.");
            return;
        }

        var root = (RectTransform)controller.transform;
        var existing = root.Find("SettingsPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        var so = new SerializedObject(controller);
        so.FindProperty("settingsPanel").objectReferenceValue = BuildSettingsPanel(root, rounded);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("일시정지 메뉴에 설정 창 연결 완료: Assets/Scenes/Main.unity (PauseMenu/SettingsPanel)");
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
