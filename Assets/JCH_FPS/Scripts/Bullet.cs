using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletDamage = 10f;
    public float bulletSpeed = 10f;

    // 2026-10-06: 발사자 참조. 자기 자신에게 맞아 터지는 것을 막기 위해 PlayerMove/Spitter에서 설정해줌
    [HideInInspector] public GameObject owner;

    Rigidbody rb;

    private bool isMove = true;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = transform.forward * bulletSpeed;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            rb.velocity = Vector3.zero;
            isMove = false;

            return;
        }
        else if (!isMove && !GameManager.Instance.isPaused)
        {
            isMove = true;
            rb.velocity = transform.forward * bulletSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 2026-10-06: 발사한 본인과는 충돌하지 않음
        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return;
        }

        // 2026-10-06: bulletCount>1(샷건형)일 때 같은 지점에서 동시 생성되는 총알끼리
        // 서로 트리거를 감지해 즉시 파괴되는 버그가 있었음 -> 총알끼리는 서로 무시
        if (other.TryGetComponent<Bullet>(out _))
        {
            return;
        }

        if(other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(bulletDamage);
        }

        Destroy(gameObject);
    }
}
