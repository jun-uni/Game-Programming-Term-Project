using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 제자리에서 원거리 마법 공격을 하는 적의 행동을 관리하는 클래스.
/// IEnemy 인터페이스를 구현하여 WordTarget과의 통합성을 제공.
/// </summary>
public class EnemyRangedController : MonoBehaviour, IEnemy
{
    private static readonly int Die = Animator.StringToHash("Die");
    private static readonly int IsRangedAttack = Animator.StringToHash("IsRangedAttack");
    private static readonly int Hit = Animator.StringToHash("Hit");

    /// <summary>
    /// 몬스터 자신의 Transform.
    /// </summary>
    [SerializeField] private Transform monsterTransform;

    /// <summary>
    /// 플레이어의 Transform.
    /// </summary>
    private Transform playerTransform;

    /// <summary>
    /// 애니메이션 컨트롤러.
    /// </summary>
    [SerializeField] private Animator animator;

    /// <summary>
    /// 마법 공격 프리팹 (메테오, 원형 이펙트 등)
    /// </summary>
    [SerializeField] private GameObject magicAttackPrefab;

    /// <summary>
    /// 마법 공격 소환 위치 오프셋 범위 (플레이어 주변 랜덤 위치)
    /// </summary>
    [SerializeField] private float attackPositionOffset = 2f;

    /// <summary>
    /// 단어 관련 컴포넌트들 (Inspector에서 할당)
    /// </summary>
    [Header("Word Display Components")] [SerializeField]
    private WordTarget wordTarget;

    [SerializeField] private WordDisplay wordDisplay;

    /// <summary>
    /// 현재 몬스터의 상태.
    /// </summary>
    public EnemyRangedState state;

    /// <summary>
    /// 이전 상태 (피격 후 복구용)
    /// </summary>
    private EnemyRangedState previousState;

    /// <summary>
    /// 원거리 공격 사거리.
    /// </summary>
    public float attackRange = 50.0f;

    /// <summary>
    /// 몬스터가 사망했는지 여부.
    /// </summary>
    public bool isDie = false;

    /// <summary>
    /// 몬스터의 체력.
    /// </summary>
    public int hitPoint = 30;

    public int maxHitPoint = 30;

    /// <summary>
    /// EnemySpawner 참조 (풀 반환용)
    /// </summary>
    private EnemySpawner enemySpawner;

    /// <summary>
    /// 공격 쿨다운 시간
    /// </summary>
    public float attackCooldown = 3f;

    /// <summary>
    /// 피격 상태인지 여부
    /// </summary>
    public bool isHit = false;

    /// <summary>
    /// 피격 지속 시간
    /// </summary>
    [SerializeField] private float hitDuration = 0.5f;

    /// <summary>
    /// 마법 공격이 정확히 플레이어 위치에 떨어질지 여부 (false면 주변 랜덤 위치)
    /// </summary>
    [SerializeField] private bool exactPlayerPosition = false;

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
            wordDisplay = GetComponentInChildren<WordDisplay>(true);
    }

    private void OnEnable()
    {
        state = EnemyRangedState.IDLE;
        previousState = EnemyRangedState.IDLE;
        isHit = false;
        lastAttackTime = 0f;

        // 활성화 시 단어 표시 확인
        StartCoroutine(EnsureWordDisplayOnEnable());

        StartCoroutine(CheckMonsterState());
        StartCoroutine(MonsterAction());
    }

    /// <summary>
    /// 일정 시간마다 몬스터의 상태를 체크 (공격 거리 및 쿨다운 확인).
    /// </summary>
    private IEnumerator CheckMonsterState()
    {
        while (!isDie)
        {
            yield return new WaitForSeconds(0.3f);

            // 사망 상태이거나 피격 중이면 상태 변경하지 않음
            if (state == EnemyRangedState.DIE || isHit)
                continue;

            float distance = Vector3.Distance(monsterTransform.position, playerTransform.position);
            bool canAttack = Time.time - lastAttackTime >= attackCooldown;

            // 사거리 내에 있고 쿨다운이 끝났으면 공격, 아니면 대기
            if (distance <= attackRange && canAttack)
            {
                state = EnemyRangedState.RANGED_ATTACK;
                lastAttackTime = Time.time;
            }
            else if (state != EnemyRangedState.RANGED_ATTACK)
            {
                state = EnemyRangedState.IDLE;
            }
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
            case EnemyRangedState.IDLE:
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(monsterTransform.position, attackRange);
                break;
            case EnemyRangedState.RANGED_ATTACK:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(monsterTransform.position, attackRange);
                break;
            case EnemyRangedState.HIT:
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
    private IEnumerator MonsterAction()
    {
        while (!isDie)
        {
            switch (state)
            {
                case EnemyRangedState.IDLE:
                    // Idle은 기본 상태이므로 별도 파라미터 설정 불필요
                    // 플레이어 방향으로 회전
                    LookAtPlayer();
                    break;

                case EnemyRangedState.RANGED_ATTACK:
                    animator.SetBool(IsRangedAttack, true);

                    // 플레이어 방향으로 회전
                    LookAtPlayer();
                    break;

                case EnemyRangedState.HIT:
                    // 피격 중에는 공격 애니메이션 중단
                    animator.SetBool(IsRangedAttack, false);
                    break;

                case EnemyRangedState.DIE:
                    isDie = true;
                    animator.SetTrigger(Die);

                    Debug.Log("원거리 적 사망");

                    // GameManager에 적 처치 점수 추가 알림
                    if (GameManager.Instance != null && GameManager.Instance.IsGameActive())
                        GameManager.Instance.AddEnemyKillScore();

                    // 죽으면 즉시 단어 비활성화
                    DisableWordDisplay();

                    GetComponent<CapsuleCollider>().enabled = false;

                    yield return new WaitForSeconds(3.0f);

                    // 리셋
                    hitPoint = 100;
                    isDie = false;
                    state = EnemyRangedState.IDLE;
                    previousState = EnemyRangedState.IDLE;
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
    /// 플레이어 방향으로 회전 (Y축만)
    /// </summary>
    private void LookAtPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0; // Y축 회전만 적용

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// 피격 처리 메서드 (IEnemy 인터페이스 구현)
    /// </summary>
    public void OnHit(int damage)
    {
        // 이미 피격 중이거나 사망 상태면 무시
        if (isHit || state == EnemyRangedState.DIE || isDie)
            return;

        // 체력 감소
        hitPoint -= damage;

        Debug.Log($"{gameObject.name}이(가) {damage} 데미지를 받음. 남은 체력: {hitPoint}");

        // 체력이 0 이하가 되면 사망 처리
        if (hitPoint <= 0)
        {
            state = EnemyRangedState.DIE;
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
        state = EnemyRangedState.HIT;

        // 피격 애니메이션 재생
        animator.SetTrigger(Hit);

        Debug.Log($"{gameObject.name} 피격 애니메이션 재생");

        // 피격 지속 시간 대기
        yield return new WaitForSeconds(hitDuration);

        // 피격 상태 해제
        isHit = false;

        // 이전 상태로 복구 (단, 사망하지 않은 경우에만)
        if (hitPoint > 0 && state != EnemyRangedState.DIE)
        {
            state = previousState;

            // 이전 상태가 공격 상태였다면 애니메이션 파라미터도 복구
            if (previousState == EnemyRangedState.RANGED_ATTACK) animator.SetBool(IsRangedAttack, true);

            Debug.Log($"{gameObject.name} 피격 상태 해제, {previousState} 상태로 복구");
        }
    }

    /// <summary>
    /// 애니메이션 이벤트: 원거리 마법 공격 소환 (애니메이션의 특정 프레임에서 호출)
    /// </summary>
    public void OnRangedAttackFire()
    {
        if (magicAttackPrefab != null && playerTransform != null)
        {
            Vector3 spawnPosition;

            if (exactPlayerPosition)
            {
                // 정확히 플레이어 발 밑에 소환
                spawnPosition = playerTransform.position;
            }
            else
            {
                // 플레이어 주변 랜덤 위치에 소환
                Vector3 randomOffset = new(
                    Random.Range(-attackPositionOffset, attackPositionOffset),
                    0,
                    Random.Range(-attackPositionOffset, attackPositionOffset)
                );
                spawnPosition = playerTransform.position + randomOffset;
            }

            // 마법 공격 소환 (메테오, 원형 이펙트 등)
            GameObject magicAttack = Instantiate(magicAttackPrefab, spawnPosition, Quaternion.identity);

            Debug.Log($"마법 공격 소환: {spawnPosition}");
        }
    }

    /// <summary>
    /// 애니메이션 이벤트: 원거리 공격 애니메이션 완료
    /// </summary>
    public void OnRangedAttackComplete()
    {
        // 공격 애니메이션 종료 - Bool 파라미터를 false로 설정하여 Idle로 전환
        animator.SetBool(IsRangedAttack, false);

        // 상태도 다시 대기 상태로
        state = EnemyRangedState.IDLE;
    }

    /// <summary>
    /// 애니메이션 이벤트: 피격 애니메이션 완료
    /// </summary>
    public void OnHitComplete()
    {
        Debug.Log($"{gameObject.name} 피격 애니메이션 완료");
    }

    /// <summary>
    /// 단어 표시 비활성화 (죽을 때)
    /// </summary>
    private void DisableWordDisplay()
    {
        if (wordTarget != null)
        {
            wordTarget.enabled = false;
            if (TypingManager.Instance != null)
                TypingManager.Instance.UnregisterTarget(wordTarget);
        }

        if (wordDisplay != null) wordDisplay.gameObject.SetActive(false);
    }

    /// <summary>
    /// OnEnable 시 단어 표시 확인 코루틴
    /// </summary>
    private IEnumerator EnsureWordDisplayOnEnable()
    {
        yield return null;

        if (wordTarget != null && !wordTarget.enabled) wordTarget.enabled = true;

        if (wordDisplay != null && !wordDisplay.gameObject.activeSelf) wordDisplay.gameObject.SetActive(true);
    }

    /// <summary>
    /// 단어 표시 활성화 (부활 시)
    /// </summary>
    private void EnableWordDisplay()
    {
        if (wordTarget != null) wordTarget.enabled = true;

        if (wordDisplay != null) wordDisplay.gameObject.SetActive(true);
    }
}
