using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spitter : EnemyBase
{
    protected override void SetState()
    {
        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        if (distance < attackRange && DetectWall())
        {
            state = EnemyState.Attack;
        }
        else
        {
            state = EnemyState.Track;
        }
    }

    // ���� ���� -----------------------------------

    [Header("�Ѿ� ������")]
    [SerializeField]
    private GameObject bulletPrefab;
    [SerializeField]
    private Transform firePos;

    protected override void Attack()
    {
        if(targetPlayer == null)
        {
            return;
        }

        Vector3 fireDir = (targetPlayer.position - firePos.position).normalized;

        Quaternion fireRot = Quaternion.LookRotation(fireDir);

        GameObject bullet = Instantiate(bulletPrefab, firePos.position, fireRot);
        Bullet bulletComp = bullet.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            bulletComp.owner = gameObject;
        }
    }

    // ���� ------------------------------------------

    [Header("���Ÿ� �� ����")]
    [SerializeField]
    private LayerMask wallLayer;

    private bool DetectWall()
    {
        if (targetPlayer == null)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = targetPlayer.position + Vector3.up * 1.5f;

        Vector3 dir = (targetPos - origin).normalized;
        float distance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, wallLayer))
        {
            print("Block!");
            return false;
        }

        return true;
    }
}
