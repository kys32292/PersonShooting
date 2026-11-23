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

        // 2026-11-23: 테스트 씬도 실제 게임과 같은 중력장 외형 설정을 조회합니다.
        internal static VfxConfig.GravityWellSettings GravityWell => LoadConfig() ? config.GravityWell : null;

        public static FireRingVfx AttachFireRing(Transform ring, float radius, int centerCount = 3)
        {
            if (!Application.isPlaying || ring == null || !LoadConfig()) return null;
            var instance = new GameObject("Orbiting stylized flame ring");
            instance.transform.SetParent(ring, false);
            var effect = instance.AddComponent<FireRingVfx>();
            effect.Initialize(radius, config.FireRing, centerCount);
            return effect;
        }

        // 2026-11-23: 착탄 지점의 GravityField 자식으로 붙여 이동/일시정지/소멸 수명을 맞춥니다.
        // 전투 범위와 유지 시간은 호출자가 전달하고, 외형 수치는 VfxConfig에서 가져옵니다.
        public static GravityWellVfx AttachGravityWell(Transform field, float radius, float duration)
        {
            VfxConfig.GravityWellSettings settings = GravityWell;
            if (!Application.isPlaying || field == null || settings == null || !settings.enabled) return null;
            var instance = new GameObject("Gravity Well VFX");
            instance.transform.SetParent(field, false);
            GravityWellVfx effect = instance.AddComponent<GravityWellVfx>();
            effect.Initialize(radius, duration, settings);
            return effect;
        }

        // 2026-11-23: 발사체 자식으로 붙여 이동/착탄/수명 종료 때 함께 정리합니다.
        // 실제 중력장 반경을 전달받아 외형만 확대하며 충돌 판정에는 관여하지 않습니다.
        public static GravityProjectileVfx AttachGravityProjectile(Transform projectile, float radius)
        {
            if (!Application.isPlaying || projectile == null || !LoadConfig()) return null;
            VfxConfig.GravityProjectileSettings settings = config.GravityProjectile;
            if (settings == null || !settings.enabled) return null;
            var instance = new GameObject("Gravity Projectile VFX");
            instance.transform.SetParent(projectile, false);
            GravityProjectileVfx effect = instance.AddComponent<GravityProjectileVfx>();
            effect.Initialize(radius, settings);
            return effect;
        }

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

        // 2026-11-23: 일반탄 궤적 설정을 복사하고 색만 빨갛게 덮어씁니다. 공유 Config는 수정하지 않습니다.
        public static void AttachExplosiveProjectile(Transform projectile)
        {
            if (!Application.isPlaying || projectile == null || !LoadConfig()) return;
            VfxConfig.ExplosiveAmmoSettings ammo = config.ExplosiveAmmo;
            // 2026-11-23: 색 복사/궤적 생성은 유탄과 같은 경로를 사용합니다.
            if (ammo == null || !ammo.projectileEnabled) return;
            AttachColoredBulletTrail(projectile, ammo.projectileHead, ammo.projectileTail, "Explosive Red Bullet Trail", ammo.projectileWidthScale);
        }

        // 2026-11-23: 유탄은 일반탄 궤적의 초록색 버전을 사용하며 기존 포물선 이동을 그대로 따릅니다.
        public static void AttachGrenadeProjectile(Transform projectile)
        {
            if (!Application.isPlaying || projectile == null || !LoadConfig()) return;
            VfxConfig.GrenadeAmmoSettings ammo = config.GrenadeAmmo;
            if (ammo == null || !ammo.projectileEnabled) return;
            AttachColoredBulletTrail(projectile, ammo.projectileHead, ammo.projectileTail, "Grenade Green Bullet Trail");
        }

        // 2026-11-23: 일반탄 설정을 변경하지 않고 색만 다른 인스턴스 설정으로 복사합니다.
        private static void AttachColoredBulletTrail(Transform projectile, Color head, Color tail, string label, float widthScale = 1f)
        {
            VfxConfig.BulletTrailSettings source = config.BulletTrail;
            if (source == null) return;
            var coloredTrail = new VfxConfig.BulletTrailSettings
            {
                length = source.length, width = source.width * Mathf.Max(0f, widthScale), tailWidthRatio = source.tailWidthRatio,
                headColor = head, tailColor = tail,
                fadeDuration = source.fadeDuration, minVertexDistance = source.minVertexDistance, material = source.material
            };
            var instance = new GameObject(label);
            instance.layer = projectile.gameObject.layer;
            // 2026-11-23: 궤적은 탄의 자식 대신 독립 오브젝트로 두어 탄 제거 후에도 잔광이 끝까지 사라지게 합니다.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, projectile.gameObject.scene);
            instance.AddComponent<BulletTrailVfx>().Initialize(projectile, coloredTrail);
        }

        // 2026-11-23: 관통탄 자식으로 붙여 적을 통과해도 유지하고 벽 충돌/수명 종료 시 함께 제거합니다.
        public static PiercingProjectileVfx AttachPiercingProjectile(Transform projectile)
        {
            if (!Application.isPlaying || projectile == null || !LoadConfig()) return null;
            VfxConfig.PiercingAmmoSettings settings = config.PiercingAmmo;
            if (settings == null || !settings.enabled) return null;
            var instance = new GameObject("Piercing Golden Ray VFX");
            instance.transform.SetParent(projectile, false);
            PiercingProjectileVfx effect = instance.AddComponent<PiercingProjectileVfx>();
            effect.Initialize(settings);
            return effect;
        }

        // 2026-11-23: 유탄 충돌/수명 종료 위치에 화염 기둥과 연기/먼지를 한 번 재생합니다.
        public static GrenadeImpactVfx PlayGrenadeImpact(Vector3 position, float radius)
        {
            if (!Application.isPlaying || !LoadConfig()) return null;
            VfxConfig.GrenadeAmmoSettings settings = config.GrenadeAmmo;
            if (settings == null || !settings.impactEnabled) return null;
            var instance = new GameObject("Grenade Flame Smoke Burst");
            instance.transform.position = position;
            GrenadeImpactVfx effect = instance.AddComponent<GrenadeImpactVfx>();
            effect.Initialize(radius, settings);
            return effect;
        }

        // 2026-11-23: 착탄한 투사체가 제거되어도 화염 폭발은 끝까지 재생되는 독립 오브젝트입니다.
        public static ExplosiveImpactVfx PlayExplosiveImpact(Vector3 position, float radius)
        {
            if (!Application.isPlaying || !LoadConfig()) return null;
            VfxConfig.ExplosiveAmmoSettings settings = config.ExplosiveAmmo;
            if (settings == null || !settings.impactEnabled) return null;
            var instance = new GameObject("Explosive Flame Burst");
            instance.transform.position = position;
            ExplosiveImpactVfx effect = instance.AddComponent<ExplosiveImpactVfx>();
            effect.Initialize(radius, settings);
            return effect;
        }

        // 2026-11-23: 폭발 적이 즉시 제거돼도 구형 화염이 끝까지 재생되도록 독립 오브젝트로 생성합니다.
        public static ExplosiveImpactVfx PlayBomberExplosion(Vector3 position, float radius)
        {
            if (!Application.isPlaying || !LoadConfig()) return null;
            VfxConfig.BomberExplosionSettings settings = config.BomberExplosion;
            if (settings == null || !settings.enabled) return null;
            var instance = new GameObject("Bomber yellow spherical explosion");
            instance.transform.position = position;
            ExplosiveImpactVfx effect = instance.AddComponent<ExplosiveImpactVfx>();
            // 2026-11-23: 폭발탄 Config를 수정하지 않고 노란색 전용 값을 복사해 같은 화염 셰이더를 재사용합니다.
            effect.Initialize(radius, new VfxConfig.ExplosiveAmmoSettings
            {
                duration = settings.duration, radiusScale = settings.radiusScale,
                flameCount = settings.flameCount, hotColor = settings.hotColor,
                flameColor = settings.flameColor, edgeColor = settings.edgeColor,
                noiseScale = settings.noiseScale, distortion = settings.distortion,
                flameSpeed = settings.flameSpeed, rise = 0f,
                sparkCount = settings.sparkCount, sparkWidth = settings.sparkWidth, sparkLength = settings.sparkLength
            }, 1f, true);
            return effect;
        }

        // 2026-11-23: 모든 신규 특수탄도 같은 Resources 설정을 한 번 읽어 공유하고 누락 경고는 반복하지 않습니다.
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
