using UnityEngine;
using UnityEngine.UI;

// 2026-11-25: 같은 캔버스 안에서 나중에 생성된 UI(경험치 팝업 등)에 가려지지 않도록
// 대상에 자체 Canvas를 붙여 정렬 순서를 올린다. 클릭을 받기 위해 GraphicRaycaster도 함께 붙인다.
// 정렬 순서: HUD 0 < 보스 비네트 500 < 증강 선택 600 < 일시정지 700 < 증강 흡수 VFX 1000
public static class UISorting
{
    public const int PerkOrder = 600;
    public const int PauseOrder = 700;

    // 꺼진 상태에서 설정하면 overrideSorting이 초기화될 수 있으므로 오브젝트가 켜진 뒤에 호출
    public static void BringToFront(GameObject target, int sortingOrder)
    {
        if (target == null) return;

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null) canvas = target.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        if (target.GetComponent<GraphicRaycaster>() == null) target.AddComponent<GraphicRaycaster>();
    }
}
