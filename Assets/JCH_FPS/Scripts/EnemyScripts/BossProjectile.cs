// 11/19 - 보스 패턴 3: 직선 투사체 (비유도)
// Rigidbody 없이 직접 이동하고, 매 프레임 이동 경로를 SphereCast로 검사해서 빠른 속도에서도 뚫고 지나가지 않게 함
// 플레이어만 맞고, 다른 몹(IDamageable)은 통과, 벽/바닥에 닿으면 사라짐. 일시정지 중에는 멈춤
// 기존 Bullet을 쓰지 않는 이유: Bullet은 맞은 IDamageable 모두에게 피해를 줘서 일반 몹도 맞기 때문
using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    private Vector3 velocity;
    private float radius;
    private float damage;
    private float lifeTime;

    private Transform player;
    private IDamageable playerDamageable;
    private GameObject owner;

    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    public static BossProjectile Spawn(Vector3 origin, Vector3 direction, float speed, float radius, float damage,
        float lifeTime, Transform player, GameObject owner)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
        direction.Normalize();

        var go = new GameObject("Boss Projectile");
        go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));

        // 본체: 빛 구체
        Transform orb = BossVisuals.CreateOrb("Orb", go.transform, out Renderer orbRenderer);
        orb.localScale = Vector3.one * radius * 2f;
        BossVisuals.SetColor(orbRenderer, new Color(1f, 0.4f, 0.15f, 1f), 0.95f);

        // 꼬리: 트레일
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.startWidth = radius * 1.6f;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.1f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.55f, 0.2f), 0f), new GradientColorKey(new Color(0.9f, 0.1f, 0.05f), 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;

        var projectile = go.AddComponent<BossProjectile>();
        projectile.velocity = direction * speed;
        projectile.radius = radius;
        projectile.damage = damage;
        projectile.lifeTime = lifeTime;
        projectile.player = player;
        projectile.playerDamageable = player != null ? player.GetComponent<IDamageable>() : null;
        projectile.owner = owner;
        return projectile;
    }

    private void OnDestroy()
    {
        // 런타임에 만든 트레일 머티리얼 정리
        var trail = GetComponent<TrailRenderer>();
        if (trail != null && trail.sharedMaterial != null)
        {
            Destroy(trail.sharedMaterial);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            return;
        }

        float dt = Time.deltaTime;
        lifeTime -= dt;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 position = transform.position;
        float speed = velocity.magnitude;
        float step = speed * dt;
        Vector3 direction = velocity / Mathf.Max(speed, 0.0001f);

        int count = Physics.SphereCastNonAlloc(position, radius, direction, hitBuffer, step,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // 가까운 것부터 처리
        float nearestBlock = float.MaxValue;
        bool hitPlayer = false;
        float playerDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            Transform hitTransform = hit.collider.transform;

            if (owner != null && hitTransform.IsChildOf(owner.transform)) continue; // 보스 자신

            if (player != null && hitTransform.IsChildOf(player))
            {
                hitPlayer = true;
                playerDistance = Mathf.Min(playerDistance, hit.distance);
                continue;
            }

            if (hit.collider.GetComponentInParent<IDamageable>() != null) continue; // 다른 몹은 통과

            nearestBlock = Mathf.Min(nearestBlock, hit.distance); // 벽/바닥
        }

        if (hitPlayer && playerDistance <= nearestBlock)
        {
            if (playerDamageable != null)
            {
                playerDamageable.TakeDamage(damage);
            }
            CameraShake.Shake(0.15f, 0.2f);
            Destroy(gameObject);
            return;
        }

        if (nearestBlock < float.MaxValue)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = position + velocity * dt;
    }
}
