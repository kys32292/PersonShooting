using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PesonShooting.VFX
{
    // 총알 코드는 시작 요청만 전달하고, 궤적의 추적/길이 제한/정리는 여기서 처리합니다.
    public sealed class BulletTrailVfx : MonoBehaviour
    {
        private readonly List<Vector3> points = new List<Vector3>(); // 머리 -> 꼬리
        private Transform target;
        private VfxConfig.BulletTrailSettings settings;
        private LineRenderer line;
        private float fadeElapsed;
        private bool stopped;

        public void Initialize(Transform bullet, VfxConfig.BulletTrailSettings trailSettings)
        {
            target = bullet;
            settings = trailSettings;
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = settings.material != null
                ? settings.material
                : Resources.Load<Material>("VFX/BulletTrail");
            if (line.sharedMaterial == null)
            {
                Debug.LogWarning("[VFX] 기본 총알 궤적 머티리얼이 없습니다.");
                Destroy(gameObject);
                return;
            }
            points.Add(target.position);
            points.Add(target.position);
            Draw(1f);
        }

        private void LateUpdate()
        {
            if (line == null || settings == null) return;
            if (Time.timeScale == 0f || (GameManager.Instance != null && GameManager.Instance.isPaused)) return;

            if (!stopped && target != null && target.gameObject.activeInHierarchy)
            {
                Vector3 head = target.position;
                float spacing = Mathf.Max(0.001f, settings.minVertexDistance);
                if ((head - points[1]).sqrMagnitude >= spacing * spacing)
                    points.Insert(0, head);
                else
                    points[0] = head;
                TrimToLength(Mathf.Max(0.01f, settings.length));
                Draw(1f);
                return;
            }

            stopped = true;
            fadeElapsed += Time.deltaTime;
            if (settings.fadeDuration <= 0f || fadeElapsed >= settings.fadeDuration)
            {
                Destroy(gameObject);
                return;
            }
            Draw(1f - fadeElapsed / settings.fadeDuration);
        }

        private void TrimToLength(float length)
        {
            float remaining = length;
            for (int i = 1; i < points.Count; i++)
            {
                float distance = Vector3.Distance(points[i - 1], points[i]);
                if (distance >= remaining)
                {
                    points[i] = Vector3.MoveTowards(points[i - 1], points[i], remaining);
                    if (i + 1 < points.Count) points.RemoveRange(i + 1, points.Count - i - 1);
                    return;
                }
                remaining -= distance;
            }
        }

        private void Draw(float opacity)
        {
            line.startWidth = Mathf.Max(0.001f, settings.width);
            line.endWidth = line.startWidth * Mathf.Clamp01(settings.tailWidthRatio);
            Color head = settings.headColor;
            Color tail = settings.tailColor;
            head.a *= opacity;
            tail.a *= opacity;
            line.startColor = head;
            line.endColor = tail;
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
        }
    }
}
