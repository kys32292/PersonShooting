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
    private GameObject playerPrefab;
    [SerializeField]
    protected Transform targetPlayer;
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

        switch (state)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                break;
            case EnemyState.Track:
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

    // 행동 상태 전환 ------------------------------------------------

    protected virtual void SetState()
    {
        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        if(distance >= attackRange)
        {
            state = EnemyState.Track;
        }
        else if(distance < attackRange)
        {
            state = EnemyState.Attack;
        }
        else
        {
            state = EnemyState.Idle;
        }
    }

    // 이동 ----------------------------------------------------------

    private float moveSpeed = 5f;

    private void SetSpeed(float _speed)
    {
        agent.speed = _speed;
    }

    // 공격 ------------------------------------------------------------

    [SerializeField]
    protected float damage = 10f;
    [SerializeField]
    protected float attackRange = 2f;

    [SerializeField]
    private float attackRate = 1f;
    private float attackRateTime = 0f;

    protected virtual void Attack()
    {
        print("Attack!");

        if (playerPrefab.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage);
        }
    }

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

    protected void Die()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.AddEXP(exp);
        }

        Destroy(gameObject);
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
