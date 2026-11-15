// 11/15 - 보스 장판/경고 표시용
using UnityEngine;

// 런타임에 만든 머티리얼이 오브젝트와 함께 정리되도록 함 (메모리 누수 방지)
public class DestroyMaterialOnDestroy : MonoBehaviour
{
    private void OnDestroy()
    {
        var meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            Destroy(meshRenderer.sharedMaterial);
        }
    }
}
