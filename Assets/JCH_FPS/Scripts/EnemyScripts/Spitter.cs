using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spitter : MonoBehaviour
{
    private Animator animator;
    [SerializeField]
    private EnemyBase enemyBase;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    // ���� ���� -----------------------------------

    [Header("�Ѿ� ������")]
    [SerializeField]
    private GameObject bulletPrefab;
    [SerializeField]
    private Transform firePos;

    protected void Spitter_Attack()
    {
        if(enemyBase.targetPlayer == null)
        {
            return;
        }

        Vector3 fireDir = (enemyBase.targetPlayer.position - firePos.position).normalized;

        Quaternion fireRot = Quaternion.LookRotation(fireDir);

        GameObject bullet = Instantiate(bulletPrefab, firePos.position, fireRot);

        // 2026-10-06: Bullet.owner 설정 (자기 자신과 충돌 방지, PlayerMove.FireGun과 동일한 패턴)
        Bullet bulletComp = bullet.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            bulletComp.owner = gameObject;
        }
    }

    public void Spitter_Die()
    {
        Destroy(transform.root.gameObject);
    }
}
