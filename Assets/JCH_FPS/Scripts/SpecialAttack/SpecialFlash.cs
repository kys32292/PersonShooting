// 11/16 - 폭발탄/유탄 폭발 연출: 반경 크기까지 커지면서 투명해지는 구체
using UnityEngine;

public class SpecialFlash : MonoBehaviour
{
    private float radius;
    private float duration;
    private float elapsed = 0f;
    private Material material;
    private Color baseColor;

    public static void Spawn(Vector3 position, float radius, Color color, float duration)
    {
        color.a = 0.6f;
        GameObject sphere = SpecialAttackUtil.CreateSphere(color);
        sphere.name = "SpecialFlash";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * radius * 0.4f;

        SpecialFlash flash = sphere.AddComponent<SpecialFlash>();
        flash.radius = radius;
        flash.duration = Mathf.Max(0.01f, duration);
        flash.material = sphere.GetComponent<Renderer>().sharedMaterial;
        flash.baseColor = color;
    }

    private void Update()
    {
        if (SpecialAttackUtil.IsPaused) return;

        elapsed += Time.deltaTime;
        float p = Mathf.Clamp01(elapsed / duration);

        transform.localScale = Vector3.one * radius * 2f * Mathf.Lerp(0.2f, 1f, p);

        Color c = baseColor;
        c.a = baseColor.a * (1f - p);
        material.color = c;

        if (p >= 1f) Destroy(gameObject);
    }
}
