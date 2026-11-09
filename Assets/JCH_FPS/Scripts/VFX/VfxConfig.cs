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

        // 2026-11-09: Inspector 설정을 공통 서비스가 조회합니다. 같은 ID는 한 번만 등록합니다.
        public bool TryGet(VfxId id, out Entry entry)
        {
            if (entries != null)
            {
                foreach (Entry candidate in entries)
                {
                    if (candidate != null && candidate.id == id)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }
    }
}
