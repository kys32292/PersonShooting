// 11/09 - 파일 인코딩 CP949 -> UTF-8 변환 (한글 주석 깨짐 해결)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomber : EnemyBase
{
    protected override void Attack()
    {
        Explode();
    }

    [Header("폭발 피해 반경")]
    // 자폭 시작 거리와 같은 피해 반경 3을 사용합니다. VFX 외형 크기는 별도 설정입니다.
    [Tooltip("자폭 피해 반경입니다. VFX 크기는 바꾸지 않습니다.")]
    [SerializeField]
    private float explosionRadius = 3f;
    // 2026-11-23: 기존 VFX 기준 반경 7.5를 유지해 사용자가 줄여 둔 Config 배율이 중복 축소되지 않게 합니다.
    [Header("폭발 VFX 기준 반경")]
    [Tooltip("VfxConfig의 폭발 외형 크기 배율에 곱할 기준 반경입니다. 실제 피해 반경과 독립적입니다.")]
    [SerializeField, Min(0.01f)] private float explosionVfxRadius = 7.5f;
    public float ExplosionVfxRadius => explosionVfxRadius;
    // 2026-11-23: 테스트 씬이 실제 프리팹의 피해 반경으로 VFX를 비교하도록 읽기 전용으로 공개합니다.
    public float ExplosionRadius => explosionRadius;

    private void Explode()
    {
        // 2026-11-23: 자폭 공격에만 노란 구형 화염을 재생합니다. 일반 피격 사망과 기존 피해 판정은 유지합니다.
        // 2026-11-23: 피해 반경 대신 독립적인 시각 기준 반경으로 호출하여 조절된 외형 크기를 유지합니다.
        PesonShooting.VFX.VfxService.PlayBomberExplosion(transform.position, explosionVfxRadius);
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
