// 11/15 - 보스 생성/연결 에디터 도구
// Tools/Boss/Build Boss (Main) 한 번 실행으로
// 1. 보스 애니메이터(Assets/JCH_FPS/Anim/Boss_Anim.controller) 생성
// 2. 보스 프리팹(Assets/JCH_FPS/Prefabs/Enemy (Boss).prefab) 생성 - Monster01 모델 x2.5, 다른 색 머티리얼
// 3. Main 씬: EnemyManager.bossPrefab 연결 + 상단 보스 HP 바/등장 경고 UI(BossUI) 생성
// 4. Ending 씬: "보스 처치" 기록 카드 추가 + EndingController.bossText 연결
// 여러 번 실행해도 기존 것을 지우고 다시 만듦 (프리팹/컨트롤러 덮어씀)
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BossBuilder
{
    const string MonsterRoot = "Assets/Art/Character/Stylized3DMonster/Monster01/";
    const string ModelPath = MonsterRoot + "Monster01_AllAnim.fbx";
    const string MaterialPath = MonsterRoot + "Shader_Texture/Shader/Monster01_04.mat"; // Spitter(Monster01_01)와 다른 색
    const string InPlace = MonsterRoot + "Anim/InPlace_Anim/";

    const string ControllerPath = "Assets/JCH_FPS/Anim/Boss_Anim.controller";
    const string PrefabPath = "Assets/JCH_FPS/Prefabs/Enemy (Boss).prefab";
    const string MainScenePath = "Assets/Scenes/Main.unity";
    const string EndingScenePath = "Assets/Scenes/Ending.unity";
    const string BarSpritePath = "Assets/JCH_FPS/Image/Bar.png";

    const float ModelScale = 2.5f;

    [MenuItem("Tools/Boss/Build Boss (Main)")]
    public static void BuildBoss()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        AnimatorController controller = BuildController();
        if (controller == null) return;

        GameObject prefab = BuildPrefab(controller);
        if (prefab == null) return;

        if (!SetupMainScene(prefab)) return;
        SetupEndingScene();

        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        Debug.Log("[Boss] 보스 생성 및 연결 완료: " + PrefabPath + " / Main 씬 BossHUD / Ending 씬 보스 기록");
    }

    // 1. 애니메이터 ---------------------------------------------------

    static AnimatorController BuildController()
    {
        AnimationClip idle = LoadClip(InPlace + "Monster01_Idle.anim");
        AnimationClip run = LoadClip(InPlace + "Monster01_Run_InPlace.anim");
        AnimationClip cast = LoadClip(InPlace + "Monster01_Shoot.anim");
        AnimationClip windup = LoadClip(InPlace + "Monster01_Attack02_InPlace.anim");
        AnimationClip stun = LoadClip(InPlace + "Monster01_Stunned.anim");
        AnimationClip die = LoadClip(MonsterRoot + "Anim/Monster01_Die.anim");
        if (idle == null || run == null || cast == null || windup == null || stun == null || die == null)
        {
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        foreach (string trigger in new[] { "Idle", "Run", "Cast", "Windup", "Dash", "Stun" })
        {
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
        }
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idleState = AddState(sm, "Idle", idle, 1f, new Vector3(300, 0));
        AnimatorState runState = AddState(sm, "Run", run, 1f, new Vector3(300, 80));
        AnimatorState castState = AddState(sm, "Cast", cast, 1f, new Vector3(300, 160));
        AnimatorState windupState = AddState(sm, "Windup", windup, 0.7f, new Vector3(300, 240));
        AnimatorState dashState = AddState(sm, "Dash", run, 2f, new Vector3(300, 320)); // 달리기 모션을 2배속으로
        AnimatorState stunState = AddState(sm, "Stunned", stun, 1f, new Vector3(300, 400));
        AnimatorState dieState = AddState(sm, "Die", die, 1f, new Vector3(300, 480));
        sm.defaultState = idleState;

        AddAnyStateTrigger(sm, idleState, "Idle");
        AddAnyStateTrigger(sm, runState, "Run");
        AddAnyStateTrigger(sm, castState, "Cast");
        AddAnyStateTrigger(sm, windupState, "Windup");
        AddAnyStateTrigger(sm, dashState, "Dash");
        AddAnyStateTrigger(sm, stunState, "Stun");

        // 시전 모션이 끝나면 대기 모션으로
        var castExit = castState.AddTransition(idleState);
        castExit.hasExitTime = true;
        castExit.exitTime = 0.95f;
        castExit.duration = 0.15f;

        var dieTransition = sm.AddAnyStateTransition(dieState);
        dieTransition.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        dieTransition.canTransitionToSelf = false;
        dieTransition.duration = 0.1f;
        dieTransition.hasExitTime = false;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    static AnimationClip LoadClip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogError("[Boss] 애니메이션 클립을 찾지 못했습니다: " + path);
        }
        return clip;
    }

    static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip, float speed, Vector3 position)
    {
        AnimatorState state = sm.AddState(name, position);
        state.motion = clip;
        state.speed = speed;
        state.writeDefaultValues = true;
        return state;
    }

    static void AddAnyStateTrigger(AnimatorStateMachine sm, AnimatorState target, string trigger)
    {
        AnimatorStateTransition transition = sm.AddAnyStateTransition(target);
        transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
        transition.canTransitionToSelf = false;
        transition.hasExitTime = false;
        transition.duration = 0.12f;
    }

    // 2. 프리팹 ---------------------------------------------------------

    static GameObject BuildPrefab(AnimatorController controller)
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelAsset == null)
        {
            Debug.LogError("[Boss] 모델을 찾지 못했습니다: " + ModelPath);
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Debug.LogWarning("[Boss] 머티리얼을 찾지 못해 모델 기본 머티리얼을 사용합니다: " + MaterialPath);
        }

        var root = new GameObject("Enemy (Boss)");

        // 루트는 스케일 1 유지 (NavMeshAgent/콜라이더 수치를 그대로 쓰기 위해), 모델만 키움
        var agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.angularSpeed = 240f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 2.5f;
        agent.radius = 1.2f;
        agent.height = 5f;
        agent.autoBraking = true;

        var body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        var capsule = root.AddComponent<CapsuleCollider>();
        capsule.radius = 1.2f;
        capsule.height = 5f;
        capsule.center = new Vector3(0f, 2.5f, 0f);

        var boss = root.AddComponent<BossEnemy>();

        var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * ModelScale;

        if (material != null)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Monster01 클립에 들어 있는 Spitter용 애니메이션 이벤트를 무시하는 수신기
        if (animator.GetComponent<BossAnimationEvents>() == null)
        {
            animator.gameObject.AddComponent<BossAnimationEvents>();
        }

        var so = new SerializedObject(boss);
        so.FindProperty("animator").objectReferenceValue = animator;
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        return prefab;
    }

    // 3. Main 씬 ---------------------------------------------------------

    static bool SetupMainScene(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        var enemyManager = Object.FindObjectOfType<EnemyManager>(true);
        if (enemyManager == null)
        {
            Debug.LogError("[Boss] Main 씬에서 EnemyManager를 찾지 못했습니다.");
            return false;
        }

        var emSO = new SerializedObject(enemyManager);
        emSO.FindProperty("bossPrefab").objectReferenceValue = prefab;
        emSO.ApplyModifiedPropertiesWithoutUndo();

        // 경험치 바가 있는 HUD 캔버스에 붙이고, 같은 TMP 폰트를 사용
        var gameManager = Object.FindObjectOfType<GameManager>(true);
        Canvas canvas = null;
        TMP_FontAsset font = null;
        if (gameManager != null)
        {
            var gmSO = new SerializedObject(gameManager);
            var expBar = gmSO.FindProperty("expBar").objectReferenceValue as Image;
            var levelText = gmSO.FindProperty("levelText").objectReferenceValue as TMP_Text;
            if (expBar != null) canvas = expBar.canvas != null ? expBar.canvas.rootCanvas : expBar.GetComponentInParent<Canvas>();
            if (levelText != null) font = levelText.font;
        }
        if (canvas == null)
        {
            foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            {
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay) { canvas = c; break; }
            }
        }
        if (canvas == null)
        {
            Debug.LogError("[Boss] Main 씬에서 HUD 캔버스를 찾지 못했습니다.");
            return false;
        }

        Transform existing = canvas.transform.Find("BossHUD");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        RectTransform hud = CreateRect("BossHUD", canvas.transform);
        hud.anchorMin = Vector2.zero;
        hud.anchorMax = Vector2.one;
        hud.offsetMin = hud.offsetMax = Vector2.zero;
        hud.SetAsLastSibling();
        var bossUI = hud.gameObject.AddComponent<BossUI>();

        // ---- HP 바 (기존 체력/경험치 바 아래) ----
        RectTransform bar = CreateRect("BossBar", hud);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(680, 46);
        bar.anchoredPosition = new Vector2(0, -104);

        TMP_Text nameText = CreateTMP("BossName", bar, "BEHEMOTH", 18, new Color(1f, 0.85f, 0.8f, 1f), TextAlignmentOptions.BottomLeft, font,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(2, 0), new Vector2(0, 22));

        RectTransform barBG = CreateRect("BossBar_BG", bar);
        barBG.anchorMin = new Vector2(0f, 0f);
        barBG.anchorMax = new Vector2(1f, 0f);
        barBG.pivot = new Vector2(0.5f, 0f);
        barBG.sizeDelta = new Vector2(0, 20);
        barBG.anchoredPosition = Vector2.zero;
        barBG.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.02f, 0.02f, 0.85f);

        RectTransform fillRT = CreateRect("BossBar_Fill", barBG);
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(2, 2);
        fillRT.offsetMax = new Vector2(-2, -2);
        var fill = fillRT.gameObject.AddComponent<Image>();
        // 11/15 - 플레이어 체력/경험치 바와 같은 스프라이트 (Filled 이미지는 스프라이트가 있어야 fillAmount가 적용됨)
        fill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
        if (fill.sprite == null) Debug.LogWarning("[Boss] 바 스프라이트를 찾지 못했습니다: " + BarSpritePath + " (앵커 방식으로 표시됨)");
        fill.color = new Color(0.86f, 0.24f, 0.2f, 1f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        TMP_Text hpText = CreateTMP("BossHPValue", barBG, "1500 / 1500", 12, Color.white, TextAlignmentOptions.Center, font,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // ---- 등장 경고 ----
        RectTransform warning = CreateRect("BossWarning", hud);
        warning.anchorMin = warning.anchorMax = warning.pivot = new Vector2(0.5f, 0.5f);
        warning.sizeDelta = new Vector2(900, 160);
        warning.anchoredPosition = new Vector2(0, 170);
        var warningGroup = warning.gameObject.AddComponent<CanvasGroup>();
        warningGroup.alpha = 0f;
        warningGroup.blocksRaycasts = false;
        warningGroup.interactable = false;

        TMP_Text warningTitle = CreateTMP("Title", warning, "WARNING", 84, new Color(1f, 0.2f, 0.15f, 1f), TextAlignmentOptions.Center, font,
            new Vector2(0f, 0.4f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        warningTitle.fontStyle = FontStyles.Bold;
        CreateTMP("Sub", warning, "BOSS APPROACHING", 26, new Color(1f, 0.85f, 0.8f, 1f), TextAlignmentOptions.Center, font,
            new Vector2(0f, 0f), new Vector2(1f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var uiSO = new SerializedObject(bossUI);
        uiSO.FindProperty("barRoot").objectReferenceValue = bar.gameObject;
        uiSO.FindProperty("hpFill").objectReferenceValue = fill;
        uiSO.FindProperty("nameText").objectReferenceValue = nameText;
        uiSO.FindProperty("hpText").objectReferenceValue = hpText;
        uiSO.FindProperty("warningGroup").objectReferenceValue = warningGroup;
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        bar.gameObject.SetActive(false); // 보스 등장 전에는 숨김

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    // 4. Ending 씬 -------------------------------------------------------
    // 기존 엔딩 화면(폰트 등 수정된 상태)을 그대로 두고, "도달 레벨" 카드를 복제해서 보스 카드만 추가

    static void SetupEndingScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EndingScenePath) == null)
        {
            Debug.LogWarning("[Boss] Ending 씬이 없어 보스 기록 카드를 추가하지 못했습니다.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(EndingScenePath, OpenSceneMode.Single);

        var ending = Object.FindObjectOfType<EndingController>(true);
        if (ending == null)
        {
            Debug.LogWarning("[Boss] Ending 씬에서 EndingController를 찾지 못했습니다.");
            return;
        }

        var so = new SerializedObject(ending);
        var levelText = so.FindProperty("levelText").objectReferenceValue as Text;
        if (levelText == null || levelText.transform.parent == null)
        {
            Debug.LogWarning("[Boss] EndingController.levelText가 비어 있어 보스 기록 카드를 추가하지 못했습니다.");
            return;
        }

        Transform levelCard = levelText.transform.parent;
        Transform stats = levelCard.parent;

        Transform oldCard = stats.Find("BossCard");
        if (oldCard != null) Object.DestroyImmediate(oldCard.gameObject);

        GameObject bossCard = Object.Instantiate(levelCard.gameObject, stats);
        bossCard.name = "BossCard";
        bossCard.transform.SetSiblingIndex(levelCard.GetSiblingIndex() + 1);

        Text bossValue = null;
        foreach (Text text in bossCard.GetComponentsInChildren<Text>(true))
        {
            if (text.name == levelText.name)
            {
                bossValue = text;
                text.text = "0마리";
            }
            else
            {
                text.text = "보스 처치";
            }
        }

        so.FindProperty("bossText").objectReferenceValue = bossValue;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // 유틸 ----------------------------------------------------------------

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    static TMP_Text CreateTMP(string name, RectTransform parent, string content, float size, Color color, TextAlignmentOptions alignment,
        TMP_FontAsset font, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        RectTransform rt = CreateRect(name, parent);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }
}
