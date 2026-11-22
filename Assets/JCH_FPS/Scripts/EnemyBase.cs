// 11/09 - 파일 인코딩 CP949 -> UTF-8 변환 (한글 주석 깨짐 해결)
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Track,
    Attack
}

public class EnemyBase : MonoBehaviour, IDamageable
{
    public EnemyState state;

    [Header("체력")]
    public float maxHP = 100f;
    [SerializeField]
    private float currentHP = 0f;
    [Header("경험치")]
    [SerializeField]
    private float exp = 100f;

    [Header("플레이어 프리펩")]
    [SerializeField]
    public GameObject playerPrefab;
    [SerializeField]
    public Transform targetPlayer;
    private NavMeshAgent agent;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        currentHP = maxHP;
        agent = GetComponent<NavMeshAgent>();

        if(playerPrefab == null)
        {
            playerPrefab = GameObject.FindGameObjectWithTag("Player");
            targetPlayer = playerPrefab.transform;
        }

        SetSpeed(moveSpeed);
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (IsDead)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            if (!agent.isStopped)
            {
                agent.isStopped = true;
            }

            return;
        }

        if (targetPlayer != null && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPlayer.position);
        }

        SetState();

        // 11/15 - 매 프레임 SetTrigger를 호출하면 트리거가 쌓여 애니메이션이 튀므로, 상태가 바뀐 순간에만 호출
        bool stateChanged = state != prevState;
        prevState = state;

        switch (state)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                if (stateChanged) SetMoveTrigger("Idle", "Track");
                break;
            case EnemyState.Track:
                if (stateChanged) SetMoveTrigger("Track", "Idle");
                agent.isStopped = false;
                break;
            case EnemyState.Attack:
                if(Timer(ref attackRateTime, attackRate))
                {
                    Attack();
                }
                agent.isStopped = true;
                break;
        }
    }

    // 11/15 - 이전 프레임 상태 (트리거 중복 호출 방지용). 첫 프레임에 반드시 트리거가 나가도록 범위 밖 값으로 시작
    private EnemyState prevState = (EnemyState)(-1);

    private void SetMoveTrigger(string trigger, string opposite)
    {
        if (animator == null) return;

        animator.ResetTrigger(opposite); // 아직 소비되지 않은 반대 트리거가 남아 있으면 지움
        animator.SetTrigger(trigger);
    }

    // 행동 상태 전환 ------------------------------------------------

    protected virtual void SetState()
    {
        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        if(distance >= attackRange)
        {
            state = EnemyState.Track;
        }
        else if(distance < attackRange && DetectWall())
        {
            state = EnemyState.Attack;
        }
        else
        {
            state = EnemyState.Idle;
        }
    }

    // 이동 ----------------------------------------------------------

    public float moveSpeed = 5f;

    private void SetSpeed(float _speed)
    {
        agent.speed = _speed;
    }

    // 공격 ------------------------------------------------------------

    [SerializeField]
    public float damage = 10f;
    [SerializeField]
    protected float attackRange = 2f;

    [SerializeField]
    private float attackRate = 1f;
    private float attackRateTime = 0f;

    protected virtual void Attack()
    {
        print("Attack!");

        if (animator != null) // 11/15 - 애니메이터가 없는 적(Bomber) 대비
        {
            animator.SetTrigger("Attack");
        }
    }

    // 벽 감지 ------------------------------------------

    [Header("벽 감지 레이어")]
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

    // 애니메이션 -----------------------------------------------

    [Header("애니메이터")]
    [SerializeField]
    protected Animator animator;

    // 데미지, 사망 처리 ----------------------------------------

    public bool IsDead => currentHP <= 0;

    public void TakeDamage(float _damage)
    {
        if (IsDead) // 이미 죽었을 경우 중복 작동을 방지하기 위해
        {
            return;
        }

        currentHP -= _damage;
        currentHP = Mathf.Max(currentHP, 0); // currentHP 값이 음수로 내려가지 않게 하기 위함

        if (IsDead) // 데미지를 받은 뒤 죽었을 경우 Die를 호출
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        agent.isStopped = true;

        if (GameManager.Instance != null)
        {
            // 11/09 - 처치 방식 배율 + 경험치 연출을 위해 죽은 위치(몸통 높이)를 함께 넘김
            GameManager.Instance.AddKillEXP(exp, transform.position + Vector3.up * 1.5f);
            GameManager.Instance.AddKill(); // 11/15 - 보스 등장 조건용 처치 수 집계
        }

        // 11/15 - Bomber처럼 애니메이터가 없으면 사망 애니메이션 이벤트(DestroyObject)가 오지 않으므로 바로 제거
        if (animator == null)
        {
            Destroy(gameObject);
            return;
        }

        animator.SetBool("IsDead", IsDead);
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }

    // 11/09 - 무한 웨이브 강화: 소환 직후(Start 전)에 호출해야 currentHP에도 반영됨
    public void ApplyStatMultiplier(float hpMultiplier, float damageMultiplier)
    {
        maxHP *= hpMultiplier;
        damage *= damageMultiplier;
    }

    // 시간 계산 -----------------------------------------

    private bool Timer(ref float timer, float time)
    {
        timer += Time.deltaTime;

        if (timer >= time)
        {
            timer = 0;

            return true;
        }

        return false;
    }
}
