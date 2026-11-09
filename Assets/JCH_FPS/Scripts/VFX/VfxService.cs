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

        // 2026-11-09: 카드 VFX도 기존 Config 로더와 캐시를 공유합니다.
        internal static VfxConfig.PerkAbsorbSettings PerkAbsorb => LoadConfig() ? config.PerkAbsorb : null;

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

        // 총알에서는 한 번 호출하고, 추적과 정리는 VFX가 담당합니다.
        public static void AttachBulletTrail(Transform bullet)
        {
            if (!Application.isPlaying || bullet == null || !LoadConfig()) return;
            if (config.BulletTrail == null || !config.BulletTrail.enabled) return;

            var instance = new GameObject("Bullet Laser Trail");
            instance.layer = bullet.gameObject.layer;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, bullet.gameObject.scene);
            instance.AddComponent<BulletTrailVfx>().Initialize(bullet, config.BulletTrail);
        }

        private static bool LoadConfig()
        {
            if (!configLoaded)
            {
                config = Resources.Load<VfxConfig>(ConfigPath);
                configLoaded = true;
                if (config == null)
                {
                    Debug.LogWarning("[VFX] Resources/VFX/VfxConfig.asset을 찾지 못했습니다.");
                }
            }

            return config != null;
        }

        public static void Play(VfxId id, Vector3 position, Quaternion rotation)
        {
            if (!Application.isPlaying || !LoadConfig()) return;

            // 2026-11-09: 같은 상황에 등록된 여러 연출 중 하나를 Config에서 랜덤 선택합니다.
            if (!config.TryGetRandom(id, out VfxConfig.Entry entry))
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
