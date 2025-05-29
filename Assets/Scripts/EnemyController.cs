using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// 몬스터의 행동 상태를 관리하고, 애니메이션 이벤트를 통해 정확한 공격 타이밍을 제어하는 클래스.
/// </summary>
public class EnemyController : MonoBehaviour
{
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int IsAttack = Animator.StringToHash("IsAttack");
    private static readonly int IsTrace = Animator.StringToHash("IsTrace");

    /// <summary>
    /// 몬스터 자신의 Transform.
    /// </summary>
    [SerializeField] private Transform monsterTransform;

    /// <summary>
    /// 플레이어의 Transform.
    /// </summary>
    private Transform playerTransform;

    /// <summary>
    /// 경로 탐색을 위한 NavMesh 에이전트.
    /// </summary>
    [SerializeField] private NavMeshAgent agent;

    /// <summary>
    /// 애니메이션 컨트롤러.
    /// </summary>
    [SerializeField] private Animator animator;

    /// <summary>
    /// 공격 판정용 콜라이더 (Hand 태그가 달린 오브젝트)
    /// </summary>
    [SerializeField] private Collider leftHandCollider;

    [SerializeField] private Collider rightHandCollider;

    /// <summary>
    /// 현재 몬스터의 상태.
    /// </summary>
    public EnemyState state;

    /// <summary>
    /// 공격을 시작하는 거리.
    /// </summary>
    public float attackRange = 1.0f;

    /// <summary>
    /// 몬스터가 사망했는지 여부.
    /// </summary>
    public bool isDie = false;

    /// <summary>
    /// 몬스터의 체력.
    /// </summary>
    public int hitPoint = 100;

    /// <summary>
    /// 실제 공격 판정이 활성화되었는지 여부 (애니메이션 이벤트로 제어)
    /// </summary>
    public bool isAttackActive = false;

    /// <summary>
    /// 공격 쿨다운 시간
    /// </summary>
    public float attackCooldown = 2f;

    private float lastAttackTime = 0f;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // NavMeshAgent 회전 속도 설정
        agent.angularSpeed = 360f;
        agent.acceleration = 15f;


        DisableAttackCollider();
    }

    /// <summary>
    /// 매 프레임마다 플레이어를 향해 이동 지점을 설정.
    /// </summary>
    private void Update()
    {
        if (!isDie) agent.SetDestination(playerTransform.position);
    }

    private void OnEnable()
    {
        state = EnemyState.TRACE;
        isAttackActive = false;

        DisableAttackCollider();

        StartCoroutine(CheckMonsterState());
        StartCoroutine(MonsterAction());
    }

    private void OnDisable()
    {
        DisableAttackCollider();
    }

    /// <summary>
    /// 일정 시간마다 몬스터의 상태를 체크 (공격 거리 및 쿨다운 확인).
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator CheckMonsterState()
    {
        while (!isDie)
        {
            yield return new WaitForSeconds(0.3f);

            if (state == EnemyState.DIE) yield break;

            float distance = Vector3.Distance(monsterTransform.position, playerTransform.position);

            // 공격 거리 내에 있고 쿨다운이 끝났으면 공격, 아니면 추적
            if (distance <= attackRange)
                state = EnemyState.ATTACK;
            else
                state = EnemyState.TRACE;
        }
    }

    /// <summary>
    /// Gizmo를 사용해 공격 범위를 시각적으로 표시.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (monsterTransform == null)
            monsterTransform = GetComponent<Transform>();

        switch (state)
        {
            case EnemyState.TRACE:
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(monsterTransform.position, attackRange);
                break;
            case EnemyState.ATTACK:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(monsterTransform.position, attackRange);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 몬스터의 현재 상태에 따라 행동 수행.
    /// </summary>
    /// <returns>코루틴</returns>
    private IEnumerator MonsterAction()
    {
        while (!isDie)
        {
            switch (state)
            {
                case EnemyState.TRACE:
                    DisableAttackCollider();
                    agent.isStopped = false;
                    agent.SetDestination(playerTransform.position);
                    animator.SetBool(IsTrace, true);
                    animator.SetBool(IsAttack, false);
                    break;

                case EnemyState.ATTACK:
                    agent.isStopped = true; // 공격 시에는 이동 정지
                    animator.SetBool(IsTrace, false);
                    animator.SetBool(IsAttack, true);
                    break;

                case EnemyState.DIE:
                    isDie = true;
                    agent.isStopped = true;
                    animator.SetTrigger(Die);

                    Debug.Log("사망");

                    GetComponent<CapsuleCollider>().enabled = false;

                    yield return new WaitForSeconds(3.0f);

                    hitPoint = 100;
                    isDie = false;
                    state = EnemyState.TRACE;

                    GetComponent<CapsuleCollider>().enabled = true;

                    gameObject.SetActive(false);
                    break;

                default:
                    break;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// 애니메이션 이벤트: 공격 판정 시작 (팔을 휘두르기 시작할 때)
    /// </summary>
    public void OnAttackStart()
    {
        EnableAttackCollider();
    }

    /// <summary>
    /// 애니메이션 이벤트: 공격 판정 끝 (팔을 다 휘둘렀을 때)
    /// </summary>
    public void OnAttackEnd()
    {
        DisableAttackCollider();
    }

    /// <summary>
    /// 애니메이션 이벤트: 공격 애니메이션 완전 종료
    /// </summary>
    public void OnAttackComplete()
    {
        // 공격이 완전히 끝나면 다시 추적 상태로
        state = EnemyState.TRACE;

        DisableAttackCollider();
    }

    private void EnableAttackCollider()
    {
        isAttackActive = true;
        if (leftHandCollider != null)
            leftHandCollider.enabled = true;

        if (rightHandCollider != null)
            rightHandCollider.enabled = true;
    }

    private void DisableAttackCollider()
    {
        isAttackActive = false;
        if (leftHandCollider != null)
            leftHandCollider.enabled = false;

        if (rightHandCollider != null)
            rightHandCollider.enabled = false;
    }
}
