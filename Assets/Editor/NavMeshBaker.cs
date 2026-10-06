using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;

// 2026-10-06: Main.unity 바닥에 NavMesh가 전혀 구워져 있지 않아 적(EnemyManager)이
// "Failed to create agent because there is no valid NavMesh" 경고를 내며 스폰에 실패하던 문제 수정.
// 바닥/벽의 메시나 위치는 건드리지 않고, Navigation Static 플래그만 켜고 NavMesh를 굽는다.
public static class NavMeshBaker
{
    [MenuItem("Tools/Navigation/Bake NavMesh For Main")]
    public static void BakeForMain()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");

        int marked = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            // Player/적/스폰포인트/UI 등 플레이 중 움직이거나 내비게이션과 무관한 것은 건너뛰고,
            // 맵 지오메트리(MeshRenderer가 있는 static 오브젝트)만 Navigation Static으로 표시
            MarkStaticRecursive(root);
        }

        Debug.Log($"[NAVMESH] Navigation Static 표시한 MeshRenderer 오브젝트 수: {marked}");

        NavMeshBuilder.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Main.unity NavMesh 베이크 완료.");

        void MarkStaticRecursive(GameObject go)
        {
            if (go.GetComponent<PlayerMove>() != null || go.GetComponent<EnemyBase>() != null ||
                go.GetComponent<EnemyManager>() != null || go.GetComponent<GameManager>() != null ||
                go.GetComponent<Canvas>() != null)
            {
                return; // 플레이어/적/매니저/UI 트리는 내비게이션 대상에서 제외
            }

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.NavigationStatic);
                marked++;
            }

            foreach (Transform child in go.transform)
            {
                MarkStaticRecursive(child.gameObject);
            }
        }
    }
}
