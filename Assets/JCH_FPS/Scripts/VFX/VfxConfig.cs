using System;
using UnityEngine;

namespace PesonShooting.VFX
{
    // 2026-11-09: 하이어라키 대신 Project 창에 보관하는 상황별 연출 설정입니다.
    // 자동 로드 대상은 VFX/Resources/VFX/VfxConfig.asset 한 개로 유지합니다.
    [CreateAssetMenu(fileName = "VfxConfig", menuName = "VFX/Config")]
    public sealed class VfxConfig : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public VfxId id;
            [Tooltip("파티클과 전용 연출 스크립트가 구성된 프리팹")]
            public GameObject prefab;
            [Min(0.01f)] public float scale = 1f;
            [Tooltip("생성 후 제거까지의 게임 시간(초). 반복 연출도 이 시간이 지나면 제거됩니다.")]
            [Min(0.01f)] public float lifetime = 2f;
            [Tooltip("표면에 파묻히지 않도록 충돌 표면 바깥으로 이동하는 거리")]
            [Min(0f)] public float surfaceOffset = 0.03f;
            [Tooltip("프리팹의 제작 방향을 보정하는 로컬 회전값")]
            public Vector3 rotationOffset;
        }

        [Serializable]
        public sealed class BulletTrailSettings
        {
            public bool enabled = true;
            [Tooltip("총알 뒤에 남는 궤적의 최대 길이 (월드 단위)")]
            [Min(0.01f)] public float length = 2f;
            [Tooltip("총알 쪽 선의 굵기 (월드 단위)")]
            [Min(0.001f)] public float width = 0.06f;
            [Range(0f, 1f)] public float tailWidthRatio = 0.15f;
            [ColorUsage(true, true)] public Color headColor = new Color(0.3f, 0.9f, 1f, 1f);
            [ColorUsage(true, true)] public Color tailColor = new Color(0.1f, 0.5f, 1f, 0f);
            [Tooltip("총알이 사라진 뒤 궤적이 페이드 아웃되는 시간 (초)")]
            [Min(0f)] public float fadeDuration = 0.12f;
            [Tooltip("궤적 점 사이의 최소 거리. 작을수록 곡선을 세밀하게 표현합니다.")]
            [Min(0.001f)] public float minVertexDistance = 0.05f;
            [Tooltip("비워 두면 기본 레이저 머티리얼 사용. 지정 시 정점 색상과 투명도를 지원해야 합니다.")]
            public Material material;
        }

        [SerializeField] private BulletTrailSettings bulletTrail = new BulletTrailSettings();
        public BulletTrailSettings BulletTrail => bulletTrail;

        [SerializeField] private Entry[] entries = { new Entry { id = VfxId.EnemyHit } };

        // 2026-11-09: 같은 ID의 유효한 프리팹 중 하나를 같은 확률로 선택합니다.
        // 빈 항목은 제외하고, 각 후보의 크기/수명/회전 설정도 함께 반환합니다.
        public bool TryGetRandom(VfxId id, out Entry entry)
        {
            entry = null;
            int candidateCount = 0;
            if (entries != null)
            {
                foreach (Entry candidate in entries)
                {
                    if (candidate != null && candidate.id == id && candidate.prefab != null)
                    {
                        candidateCount++;
                    }
                }

                if (candidateCount == 0) return false;

                // 2026-11-09: 매 피격마다 독립 선택하므로 같은 이펙트가 연속으로 나올 수 있습니다.
                int selectedIndex = UnityEngine.Random.Range(0, candidateCount);
                foreach (Entry candidate in entries)
                {
                    if (candidate == null || candidate.id != id || candidate.prefab == null) continue;
                    if (selectedIndex-- != 0) continue;

                    entry = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
