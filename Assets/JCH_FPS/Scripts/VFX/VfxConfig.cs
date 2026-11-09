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
