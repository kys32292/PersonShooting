using UnityEngine;
using UnityEngine.Rendering;

namespace PesonShooting.VFX
{
    // 외형만 담당합니다. 적 충돌과 피해 주기는 FireRing의 기존 공전 지점에서 처리합니다.
    public sealed class FireRingVfx : MonoBehaviour
    {
        private Material material;
        private Mesh mesh;
        private float flowSpeed;

        public void Initialize(float radius, VfxConfig.FireRingSettings settings, int centerCount = 3)
        {
            if (settings == null || !settings.enabled) return;
            Shader shader = Resources.Load<Shader>("VFX/FireRing");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[VFX] FireRing 셰이더를 사용할 수 없습니다.", this);
                return;
            }

            material = new Material(shader);
            material.SetColor("_HotColor", settings.hotColor);
            material.SetColor("_FlameColor", settings.flameColor);
            material.SetColor("_EdgeColor", settings.edgeColor);
            flowSpeed = Mathf.Max(0f, settings.flowSpeed);
            mesh = CreateRibbon(Mathf.Max(0.01f, radius), settings.bandWidth, settings.flameHeight,
                Mathf.Clamp(centerCount, 1, 32), settings.trailArcDegrees);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // 호출자의 게임 시간을 사용해 일시정지와 재발동 수명을 기존 처리에 맞춥니다.
        public void Draw(float orbitAngle, float elapsed, float opacity)
        {
            if (material == null) return;
            transform.localRotation = Quaternion.Euler(0f, -orbitAngle, 0f);
            material.SetFloat("_Age", elapsed * flowSpeed);
            material.SetFloat("_Opacity", Mathf.Clamp01(opacity));
        }

        private static Mesh CreateRibbon(float radius, float width, float height, int centerCount, float arcDegrees)
        {
            const int segments = 64;
            const int rows = 5;
            width = Mathf.Clamp(width, 0.05f, radius * 1.8f);
            height = Mathf.Max(0f, height);
            float sector = 360f / centerCount;
            float arc = Mathf.Clamp(arcDegrees, 1f, sector - Mathf.Min(8f, sector * 0.2f));
            int verticesPerTrail = (segments + 1) * rows;
            var vertices = new Vector3[verticesPerTrail * centerCount];
            var uv = new Vector2[vertices.Length];
            var uv2 = new Vector2[vertices.Length];
            var triangles = new int[segments * (rows - 1) * 6 * centerCount];
            for (int trail = 0; trail < centerCount; trail++)
            {
                for (int i = 0; i <= segments; i++)
                {
                    // t=1은 실제 공전 중심점이며, 꼬리는 회전 반대 방향으로 가늘게 뻗습니다.
                    float t = i / (float)segments;
                    float degrees = trail * sector - arc * (1f - t);
                    float angle = degrees * Mathf.Deg2Rad;
                    float taper = Mathf.Pow(t, 0.7f);
                    Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    for (int j = 0; j < rows; j++)
                    {
                        float v = j / (float)(rows - 1);
                        int index = trail * verticesPerTrail + i * rows + j;
                        vertices[index] = outward * (radius + (v - 0.5f) * width * taper)
                            + Vector3.up * (Mathf.Sin(v * Mathf.PI) * height * 0.35f * taper);
                        uv[index] = new Vector2(degrees / 360f, v);
                        uv2[index] = new Vector2(t, 0f);
                        if (i == segments || j == rows - 1) continue;
                        int triangle = (trail * segments * (rows - 1) + i * (rows - 1) + j) * 6;
                        triangles[triangle] = index;
                        triangles[triangle + 1] = index + rows;
                        triangles[triangle + 2] = index + 1;
                        triangles[triangle + 3] = index + 1;
                        triangles[triangle + 4] = index + rows;
                        triangles[triangle + 5] = index + rows + 1;
                    }
                }
            }
            var result = new Mesh { name = "Separated tapered orbiting flame trails" };
            result.vertices = vertices;
            result.uv = uv;
            result.uv2 = uv2;
            result.triangles = triangles;
            result.RecalculateBounds();
            return result;
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (mesh != null) Destroy(mesh);
        }
    }
}
