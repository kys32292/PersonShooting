using System.Collections.Generic;
using UnityEngine;

namespace PesonShooting.VFX
{
    // 2026-11-09: 씬에 붙이지 않고 호출하는 연출 창구입니다.
    // 프리팹 안의 전용 스크립트는 Awake/OnEnable에서 필요한 연출을 실행할 수 있습니다.
    public static class VfxService
    {
        private const string ConfigPath = "VFX/VfxConfig";
        private static VfxConfig config;
        private static bool configLoaded;
        private static readonly HashSet<VfxId> warnedIds = new HashSet<VfxId>();

        // 2026-11-09: Domain Reload를 꺼도 이전 플레이의 설정 캐시와 경고 상태가 남지 않습니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            config = null;
            configLoaded = false;
            warnedIds.Clear();
        }

        // 2026-11-09: 트리거는 정확한 접촉점/법선을 주지 않으므로 표면의 가까운 점과
        // 총알 진행 방향의 반대를 사용합니다. 적 자식으로 붙이지 않아 사망 후에도 재생됩니다.
        public static void PlayEnemyHit(Collider hitCollider, Vector3 bulletPosition, Vector3 bulletDirection)
        {
            if (hitCollider == null) return;

            Vector3 position = hitCollider.ClosestPoint(bulletPosition);
            Vector3 outward = bulletDirection.sqrMagnitude > 0.0001f
                ? -bulletDirection.normalized
                : Vector3.up;
            Play(VfxId.EnemyHit, position, Quaternion.LookRotation(outward));
        }

        // 2026-11-09: 호출 지점은 ID, 월드 위치, 방향만 전달합니다.
        // 설정 누락은 경고 한 번으로 끝내고 피해 처리 등 게임 진행을 막지 않습니다.
        public static void Play(VfxId id, Vector3 position, Quaternion rotation)
        {
            if (!Application.isPlaying) return;

            if (!configLoaded)
            {
                config = Resources.Load<VfxConfig>(ConfigPath);
                configLoaded = true;
                if (config == null)
                {
                    Debug.LogWarning("[VFX] Resources/VFX/VfxConfig.asset을 찾지 못했습니다.");
                }
            }

            if (config == null) return;

            if (!config.TryGet(id, out VfxConfig.Entry entry) || entry.prefab == null)
            {
                if (warnedIds.Add(id))
                {
                    Debug.LogWarning($"[VFX] {id}의 설정 또는 프리팹이 없습니다.", config);
                }
                return;
            }

            Vector3 spawnPosition = position + rotation * Vector3.forward * Mathf.Max(0f, entry.surfaceOffset);
            Quaternion spawnRotation = rotation * Quaternion.Euler(entry.rotationOffset);
            GameObject instance = Object.Instantiate(entry.prefab, spawnPosition, spawnRotation);
            instance.transform.localScale *= Mathf.Max(0.01f, entry.scale);

            // 2026-11-09: Play On Awake가 꺼진 파티클도 실행하되, 이미 재생 중이면 재시작하지 않습니다.
            foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>())
            {
                if (!particle.isPlaying) particle.Play(false);
            }

            // 2026-11-09: 별도의 매니저/코루틴 없이 자동 제거합니다. 게임 시간 기준입니다.
            Object.Destroy(instance, Mathf.Max(0.01f, entry.lifetime));
        }
    }
}
