using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

/// <summary>
/// 몬스터의 행동 상태를 관리하고, 애니메이션 이벤트를 통해 정확한 공격 타이밍을 제어하는 클래스.
/// IEnemy 인터페이스를 구현하여 WordTarget과의 통합성을 제공.
/// </summary>
public class EnemyController : MonoBehaviour, IEnemy
{
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int IsAttack = Animator.StringToHash("IsAttack");
    private static readonly int IsTrace = Animator.StringToHash("IsTrace");
    private static readonly int Hit = Animator.StringToHash("Hit"); // 피격 트리거 추가

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
    /// 단어 관련 컴포넌트들 (Inspector에서 할당)
    /// </summary>
    [Header("Word Display Components")] [SerializeField]
    private WordTarget wordTarget;

    [SerializeField] private WordDisplay wordDisplay;

    /// <summary>
    /// 현재 몬스터의 상태.
    /// </summary>
    public EnemyState state;

    /// <summary>
    /// 이전 상태 (피격 후 복구용)
    /// </summary>
    private EnemyState previousState;

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
    public int hitPoint = 50;

    public int maxHitPoint = 50;

    /// <summary>
    /// EnemySpawner 참조 (풀 반환용)
    /// </summary>
    private EnemySpawner enemySpawner;

    /// <summary>
    /// 실제 공격 판정이 활성화되었는지 여부 (애니메이션 이벤트로 제어)
    /// </summary>
    public bool isAttackActive = false;

    /// <summary>
    /// 공격 쿨다운 시간
    /// </summary>
    public float attackCooldown = 2f;

    /// <summary>
    /// 피격 상태인지 여부
    /// </summary>
    public bool isHit = false;

    /// <summary>
    /// 피격 지속 시간
    /// </summary>
    [SerializeField] private float hitDuration = 0.5f;

    private float lastAttackTime = 0f;

    #region IEnemy 인터페이스 구현

    /// <summary>
    /// 적이 죽었는지 여부 (IEnemy 인터페이스 구현)
    /// </summary>
    public bool IsDead => isDie;

    /// <summary>
    /// 적의 현재 체력 (IEnemy 인터페이스 구현)
    /// </summary>
    public int HitPoint
    {
        get => hitPoint;
        set => hitPoint = value;
    }

    #endregion

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // EnemySpawner 참조 찾기 (풀 반환용)
        enemySpawner = FindObjectOfType<EnemySpawner>();

        // WordTarget과 WordDisplay 자동 찾기 (Inspector에서 할당 안 된 경우)
        if (wordTarget == null)
            wordTarget = GetComponent<WordTarget>();

        if (wordDisplay == null)
            wordDisplay = GetComponentInChildren<WordDisplay>(true); // includeInactive: true

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
        // 피격 중이거나 사망 중이면 이동하지 않음
        if (!isDie && !isHit)
            agent.SetDestination(playerTransform.position);
    }

    private void OnEnable()
    {
        state = EnemyState.TRACE;
        previousState = EnemyState.TRACE;
        isAttackActive = false;
        isHit = false;

        DisableAttackCollider();

        // 활성화 시 단어 표시 확인
        StartCoroutine(EnsureWordDisplayOnEnable());

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

            // 사망 상태이거나 피격 중이면 상태 변경하지 않음
            if (state == EnemyState.DIE || isHit)
                continue;

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
            case EnemyState.HIT:
                Gizmos.color = Color.yellow;
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

                case EnemyState.HIT:
                    // 피격 중에는 모든 행동 정지
                    DisableAttackCollider();
                    agent.isStopped = true;
                    animator.SetBool(IsTrace, false);
                    animator.SetBool(IsAttack, false);
                    // 피격 애니메이션은 OnHit() 메서드에서 처리
                    break;

                case EnemyState.DIE:
                    isDie = true;
                    agent.isStopped = true;
                    animator.SetTrigger(Die);

                    Debug.Log("사망");

                    // GameManager에 적 처치 점수 추가 알림
                    if (GameManager.Instance != null && GameManager.Instance.IsGameActive())
                        GameManager.Instance.AddEnemyKillScore();

                    // 죽으면 즉시 단어 비활성화
                    DisableWordDisplay();

                    GetComponent<CapsuleCollider>().enabled = false;

                    yield return new WaitForSeconds(3.0f);

                    hitPoint = 100;
                    isDie = false;
                    state = EnemyState.TRACE;
                    previousState = EnemyState.TRACE;
                    isHit = false;

                    // 부활 시 단어 다시 활성화
                    EnableWordDisplay();

                    GetComponent<CapsuleCollider>().enabled = true;

                    // 오브젝트 풀로 반환
                    if (enemySpawner != null)
                        enemySpawner.ReturnEnemyToPool(gameObject);
                    else
                        gameObject.SetActive(false);
                    break;

                default:
                    break;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// 피격 처리 메서드 - MagicProjectile에서 호출 (IEnemy 인터페이스 구현)
    /// </summary>
    /// <param name="damage">받을 데미지</param>
    public void OnHit(int damage)
    {
        // 이미 피격 중이거나 사망 상태면 무시
        if (isHit || state == EnemyState.DIE || isDie)
            return;

        // 체력 감소
        hitPoint -= damage;

        Debug.Log($"{gameObject.name}이(가) {damage} 데미지를 받음. 남은 체력: {hitPoint}");

        // 체력이 0 이하가 되면 사망 처리
        if (hitPoint <= 0)
        {
            state = EnemyState.DIE;
            return;
        }

        // 피격 상태 시작
        StartCoroutine(HandleHitState());
    }

    /// <summary>
    /// 피격 상태 처리 코루틴
    /// </summary>
    private IEnumerator HandleHitState()
    {
        // 현재 상태 저장 (복구용)
        previousState = state;

        // 피격 상태로 변경
        isHit = true;
        state = EnemyState.HIT;

        // 피격 애니메이션 재생
        animator.SetTrigger(Hit);

        Debug.Log($"{gameObject.name} 피격 애니메이션 재생");

        // 피격 지속 시간 대기
        yield return new WaitForSeconds(hitDuration);

        // 피격 상태 해제
        isHit = false;

        // 이전 상태로 복구 (단, 사망하지 않은 경우에만)
        if (hitPoint > 0 && state != EnemyState.DIE)
        {
            state = previousState;
            Debug.Log($"{gameObject.name} 피격 상태 해제, {previousState} 상태로 복구");
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

    /// <summary>
    /// 애니메이션 이벤트: 피격 애니메이션 완료 (선택사항)
    /// </summary>
    public void OnHitComplete()
    {
        // 피격 애니메이션이 끝났을 때 호출 (필요시 사용)
        Debug.Log($"{gameObject.name} 피격 애니메이션 완료");
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

    /// <summary>
    /// 단어 표시 비활성화 (죽을 때)
    /// </summary>
    private void DisableWordDisplay()
    {
        // WordTarget 컴포넌트 비활성화
        if (wordTarget != null)
        {
            wordTarget.enabled = false;

            // 타이핑 매니저에서 제거
            if (TypingManager.Instance != null)
                TypingManager.Instance.UnregisterTarget(wordTarget);

            Debug.Log($"{gameObject.name} WordTarget 비활성화됨");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} WordTarget이 null입니다!");
        }

        // WordDisplay 비활성화
        if (wordDisplay != null)
        {
            wordDisplay.gameObject.SetActive(false);
            Debug.Log($"{gameObject.name} WordDisplay 비활성화됨");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} WordDisplay가 null입니다!");
        }
    }

    /// <summary>
    /// OnEnable 시 단어 표시 확인 코루틴
    /// </summary>
    private IEnumerator EnsureWordDisplayOnEnable()
    {
        // 한 프레임 기다린 후 체크
        yield return null;

        // WordTarget과 WordDisplay가 제대로 활성화되어 있는지 확인
        if (wordTarget != null && !wordTarget.enabled)
        {
            wordTarget.enabled = true;
            Debug.Log($"{gameObject.name} OnEnable - WordTarget 활성화");
        }

        if (wordDisplay != null && !wordDisplay.gameObject.activeSelf)
        {
            wordDisplay.gameObject.SetActive(true);
            Debug.Log($"{gameObject.name} OnEnable - WordDisplay 활성화");
        }
    }

    /// <summary>
    /// 단어 표시 활성화 (부활 시)
    /// </summary>
    private void EnableWordDisplay()
    {
        // WordTarget 컴포넌트 활성화
        if (wordTarget != null)
        {
            wordTarget.enabled = true;
            Debug.Log($"{gameObject.name} WordTarget 활성화됨");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} WordTarget이 null입니다!");
        }

        // WordDisplay 활성화
        if (wordDisplay != null)
        {
            wordDisplay.gameObject.SetActive(true);
            Debug.Log($"{gameObject.name} WordDisplay 활성화됨");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} WordDisplay가 null입니다!");
        }
    }
}
