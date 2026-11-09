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

    // 원거리 공격 ----------------------------------- (11/09 - 깨진 주석 복구)

    [Header("총알 프리펩")] // 11/09 - 깨진 헤더 글자 복구
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
            bulletComp.bulletDamage = enemyBase.damage; // 11/09 - 투사체 피해량을 적 공격력과 연결 (무한 웨이브 강화 반영)
        }
    }

    public void Spitter_Die()
    {
        Destroy(transform.root.gameObject);
    }
}
