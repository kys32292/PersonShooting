using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletDamage = 10f;
    public float bulletSpeed = 10f;

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
        if (owner != null && other.transform.IsChildOf(owner.transform))
        {
            return; // 발사한 본인(플레이어/적)과는 충돌하지 않음
        }

        if (other.TryGetComponent<Bullet>(out _))
        {
            return; // 같은 총구에서 동시에 나간 총알끼리는 서로 충돌하지 않음
        }

        if(other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(bulletDamage);
        }

        Destroy(gameObject);
    }
}
