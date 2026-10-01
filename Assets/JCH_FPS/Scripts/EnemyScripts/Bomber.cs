using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomber : EnemyBase
{
    protected override void Attack()
    {
        Explode();
    }

    [Header("폭발 범위")]
    [SerializeField]
    private float explosionRadius = 5f;

    private void Explode()
    {
        Collider[] hirCols = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach(Collider col in hirCols)
        {
            if(col.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(damage);
            }
        }

        ExplodedDie();
    }

    // 에디터 씬 뷰에서 폭발 반경을 시각적으로 확인하기 위한 드로잉
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    private void ExplodedDie()
    {
        Destroy(gameObject);
    }
}
