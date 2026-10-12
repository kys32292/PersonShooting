using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Walker : MonoBehaviour
{
    private Animator animator;
    [SerializeField]
    private EnemyBase enemyBase;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Walker_Attack()
    {
        int randomIndex = Random.Range(0, 3);

        animator.SetInteger("AttackIndex", randomIndex);

        if (enemyBase.playerPrefab.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(enemyBase.damage);
        }
    }

    public void Walker_Die()
    {
        Destroy(transform.root.gameObject);
    }
}
