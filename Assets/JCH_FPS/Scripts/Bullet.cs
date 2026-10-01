using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletDamage = 10f;
    public float bulletSpeed = 10f;

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
        if(other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(bulletDamage);
        }

        Destroy(gameObject);
    }
}
