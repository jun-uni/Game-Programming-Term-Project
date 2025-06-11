using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")] public float moveSpeed = 4f; // 걷기 속도
    public float runSpeed = 8f; // 뛰기 속도

    [Header("Stamina Settings")] [SerializeField]
    private float maxStamina = 100f; // 최대 스태미너

    [SerializeField] private float currentStamina = 100f; // 현재 스태미너
    [SerializeField] private float staminaDrainRate = 20f; // 초당 스태미너 소모량 (달릴 때)
    [SerializeField] private float staminaRecoveryAmount = 25f; // 단어 완성시 회복량
    [SerializeField] private float minStaminaToRun = 5f; // 달리기 위한 최소 스태미너

    // 스태미너 관련 이벤트
    public static event Action<float, float> OnPlayerStaminaChanged; // (currentStamina, maxStamina)

    [Header("Animation Smooth Settings")] [Range(0.1f, 10f)]
    public float moveBlendSpeed = 8f; // 이동 블렌딩 속도

    [Range(0.1f, 10f)] public float rotationBlendSpeed = 10f; // 회전 블렌딩 속도
    [Range(0.1f, 5f)] public float animationSmoothTime = 0.15f; // 애니메이션 부드러움 정도

    [Header("Camera Settings")] public Transform camTransform;

    [Header("Hit Settings")] [Range(0.5f, 5f)]
    public float invincibleTime = 1.5f; // 무적 시간 (초)

    [Range(0.1f, 1f)] public float blinkInterval = 0.1f; // 무적 시 깜빡임 간격

    [Header("Combat Settings")] [SerializeField]
    private Transform magicFirePosition; // 마법 발사 위치

    [SerializeField] private GameObject magicProjectilePrefab; // VFX 프리팹
    [SerializeField] private string castAnimationTrigger = "Cast"; // 발사 애니메이션 트리거
    [SerializeField] private float combatRotationSpeed = 720f; // 전투 시 회전 속도
    [SerializeField] private bool smoothCombatRotation = true; // 전투 시 부드러운 회전
    [SerializeField] private float projectileSpeed = 10f; // 투사체 속도
    [SerializeField] private bool restrictMovementDuringCast = false; // 전투 중 이동 제한

    [SerializeField] private Vector3 modelRotationOffset = new(0, -90, 0); // 모델 방향 보정 (Y축 각도)
    [SerializeField] private Vector3 targetPositionYOffest = new(0, 1.5f, 0);

    [Header("Fire Sound Settings")] [SerializeField]
    private AudioClip fireSoundClip; // 마법 발사 시 재생할 사운드

    [SerializeField] private AudioSource audioSource; // 오디오 소스 (없으면 자동 생성)
    [SerializeField] private float fireSoundVolume = 1f; // 발사 소리 볼륨 (0~1)

    [Header("Hit Sound Settings")] [SerializeField]
    private AudioClip[] hitSoundClips; // 피격 시 재생할 사운드들 (랜덤 재생)

    [SerializeField] private float hitSoundVolume = 1f; // 피격 소리 볼륨 (0~1)

    [Header("Attack Queue Settings")] [SerializeField]
    private int maxAttackQueue = 3; // 최대 큐 개수

    [SerializeField] private bool allowAttackQueue = true; // 큐 시스템 사용 여부

    // 공격 큐 시스템
    private Queue<Transform> attackQueue = new();
    private bool isProcessingQueue = false;

    [Header("Death Settings")] [SerializeField]
    private float respawnDelay = 3f;

    [SerializeField] private Vector3 respawnPosition = Vector3.zero;
    [SerializeField] private bool autoRespawn = true;
    private static readonly int DeathTrigger = Animator.StringToHash("Death");
    private static readonly int IsAliveBool = Animator.StringToHash("IsAlive");

    // 체력 시스템을 int로 변경 (3칸 시스템)
    private int maxHitPoint = 3;
    private bool isDead = false;
    public static event Action OnPlayerDeath;
    public static event Action<int, int> OnPlayerHealthChanged; // (currentHealth, maxHealth)

    [Header("Death Sound Settings")] [SerializeField]
    private AudioClip deathSoundClip;

    [SerializeField] private float deathSoundVolume = 1f;

    [Header("Debug")] [SerializeField] private bool showDebugInfo = true;

    [Header("Components")] [SerializeField]
    private Animator animator;

    private Renderer[] renderers; // 깜빡임 효과를 위한 렌더러들

    private int currentHitPoint = 3; // int로 변경
    private bool isInvincible = false; // 무적 상태
    private bool isCasting = false; // 전투 중인지
    private Transform currentCastTarget; // 현재 공격 중인 타겟

    // 부드러운 애니메이션을 위한 변수들
    private Vector2 currentAnimParams = Vector2.zero;
    private Vector2 targetAnimParams = Vector2.zero;
    private Vector2 animVelocity = Vector2.zero;

    // Input dampening을 위한 변수들
    private Vector2 currentInput = Vector2.zero;
    private Vector2 inputVelocity = Vector2.zero;

    private void Start()
    {
        // 스태미너 초기화
        currentStamina = maxStamina;

        // 모든 렌더러 컴포넌트 가져오기 (깜빡임 효과용)
        renderers = GetComponentsInChildren<Renderer>();

        // 카메라 참조가 없으면 메인 카메라 사용
        if (camTransform == null)
            camTransform = Camera.main.transform;

        // AudioSource 자동 찾기 또는 생성
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false; // 자동 재생 방지
            }
        }

        // 단어 완성 이벤트 구독
        WordCompletionEvents.OnWordCompleted += HandleWordCompleted;

        // 초기 체력 UI 업데이트
        OnPlayerHealthChanged?.Invoke(currentHitPoint, maxHitPoint);

        // 초기 스태미너 UI 업데이트
        OnPlayerStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        WordCompletionEvents.OnWordCompleted -= HandleWordCompleted;
    }

    private void Update()
    {
        if (isDead) return; // 죽은 상태에서는 입력 무시

        HandleInput();
        HandleMovement();
        HandleAnimation();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 무적 상태이거나 죽은 상태일 때는 피격 처리하지 않음
        if (isInvincible || isDead) return;

        // Enemy Hand 태그 확인
        if (other.CompareTag("Enemy Hand"))
        {
            // 부모 오브젝트에서 EnemyController 찾기 (기존 방식 유지 - 근거리 적만 직접 공격)
            EnemyController enemy = other.GetComponentInParent<EnemyController>();

            if (enemy != null)
                // 실제 공격 판정이 활성화되어 있을 때만 피격 처리
                if (enemy.isAttackActive && currentHitPoint > 0)
                {
                    currentHitPoint -= 1; // 1씩 감소

                    // 체력 UI 업데이트
                    OnPlayerHealthChanged?.Invoke(currentHitPoint, maxHitPoint);

                    // 체력이 0 이하가 되면 죽음 처리
                    if (currentHitPoint <= 0)
                        OnDeath();
                    else
                        // 피격 처리
                        OnHit(enemy.transform.position);
                }
        }
    }

    #region Movement & Input

    /// <summary>
    /// 입력 처리 - 부드러운 입력 변화를 위해 SmoothDamp 사용
    /// </summary>
    private void HandleInput()
    {
        // 전투 중이고 이동 제한이 활성화되어 있으면 입력 무시
        if (restrictMovementDuringCast && isCasting)
        {
            Vector2 targetInput = Vector2.zero;
            currentInput = Vector2.SmoothDamp(currentInput, targetInput, ref inputVelocity, 0.1f);
            return;
        }

        // Raw 입력값 획득
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 targetInput2 = new(h, v);

        // 부드러운 입력 보간
        currentInput = Vector2.SmoothDamp(currentInput, targetInput2, ref inputVelocity, 0.1f);
    }

    /// <summary>
    /// 캐릭터 이동 처리 - 완전 월드축 기준 이동
    /// </summary>
    private void HandleMovement()
    {
        // 입력이 있는지 확인
        if (currentInput.magnitude > 0.1f)
        {
            // 달리기 여부 확인 (스태미너 체크 포함)
            bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
            bool canRun = currentStamina >= minStaminaToRun;
            bool isRun = wantsToRun && canRun;

            // 스태미너 소모 처리
            if (isRun)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
                currentStamina = Mathf.Max(0f, currentStamina);

                // 스태미너 UI 업데이트
                OnPlayerStaminaChanged?.Invoke(currentStamina, maxStamina);
            }

            float speed = isRun ? runSpeed : moveSpeed;

            // 완전 월드축 기준 이동 방향 (Z = 앞/뒤, X = 좌/우)
            Vector3 worldMoveDirection = new Vector3(currentInput.x, 0, currentInput.y).normalized;

            // 실제 이동 (월드 기준)
            transform.Translate(worldMoveDirection * speed * Time.deltaTime, Space.World);

            // 애니메이션용 로컬 입력 계산 (캐릭터의 현재 방향 기준)
            Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);

            // 애니메이션 파라미터 목표값 설정 (로컬 기준)
            float factor = isRun ? 2f : 1f;
            targetAnimParams = new Vector2(localMoveDirection.x * factor, localMoveDirection.z * factor);
        }
        else
        {
            // 정지 상태일 때 애니메이션 파라미터를 0으로
            targetAnimParams = Vector2.zero;
        }
    }

    /// <summary>
    /// 애니메이션 파라미터 처리 - 부드러운 블렌딩
    /// </summary>
    private void HandleAnimation()
    {
        // 현재 애니메이션 파라미터를 목표값으로 부드럽게 보간
        currentAnimParams = Vector2.SmoothDamp(
            currentAnimParams,
            targetAnimParams,
            ref animVelocity,
            animationSmoothTime
        );

        // 애니메이터에 부드러운 값 전달
        animator.SetFloat("MoveX", currentAnimParams.x);
        animator.SetFloat("MoveY", currentAnimParams.y);
    }

    #endregion

    #region Stamina System

    /// <summary>
    /// 스태미너 회복 (단어 완성 시 호출)
    /// </summary>
    public void RecoverStamina()
    {
        currentStamina += staminaRecoveryAmount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);

        // 스태미너 UI 업데이트
        OnPlayerStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (showDebugInfo)
            Debug.Log($"스태미너 회복! 현재: {currentStamina:F1}/{maxStamina}");
    }

    /// <summary>
    /// 현재 스태미너 반환
    /// </summary>
    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    /// <summary>
    /// 최대 스태미너 반환
    /// </summary>
    public float GetMaxStamina()
    {
        return maxStamina;
    }

    /// <summary>
    /// 스태미너 설정 (외부에서 호출 가능)
    /// </summary>
    public void SetStamina(float newStamina)
    {
        currentStamina = Mathf.Clamp(newStamina, 0f, maxStamina);
        OnPlayerStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    #endregion

    #region Combat System

    /// <summary>
    /// 단어 완성 이벤트 핸들러 - 큐 시스템 적용 + 스태미너 회복
    /// </summary>
    private void HandleWordCompleted(Transform target)
    {
        if (target == null) return;

        // 단어 완성 시 스태미너 회복
        RecoverStamina();

        Vector3 originalPosition = target.position; // 원래 위치 저장

        target.position += targetPositionYOffest; // 임시로 위치 변경

        if (!allowAttackQueue)
        {
            // 큐 시스템 사용 안 함 - 캐스팅 중이면 무시
            if (isCasting)
            {
                if (showDebugInfo)
                    Debug.Log($"캐스팅 중이므로 공격 무시: {target.name}");
                return;
            }

            StartCoroutine(CastSpellAtTarget(target));

            target.position = originalPosition;
        }
        else
        {
            // 큐 시스템 사용
            if (attackQueue.Count < maxAttackQueue)
            {
                attackQueue.Enqueue(target);
                if (showDebugInfo)
                    Debug.Log($"공격 큐에 추가: {target.name} (큐 크기: {attackQueue.Count})");

                // 현재 처리 중이 아니면 큐 처리 시작
                if (!isProcessingQueue) StartCoroutine(ProcessAttackQueue());
                target.position = originalPosition;
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log($"공격 큐가 가득함. 공격 무시: {target.name}");
            }
        }

        target.position = originalPosition;
    }

    /// <summary>
    /// 공격 큐 처리 코루틴 - 죽은 적 검증 추가 (IEnemy 사용)
    /// </summary>
    private System.Collections.IEnumerator ProcessAttackQueue()
    {
        isProcessingQueue = true;

        while (attackQueue.Count > 0)
        {
            Transform target = attackQueue.Dequeue();

            // 타겟이 여전히 공격 가능한지 확인 (IEnemy 인터페이스 사용!)
            if (IsValidTarget(target))
            {
                if (showDebugInfo)
                    Debug.Log($"큐에서 공격 처리: {target.name} (남은 큐: {attackQueue.Count})");

                // 완전히 끝날 때까지 기다림
                yield return StartCoroutine(CastSpellAtTargetComplete(target));
            }
            else
            {
                // 무효한 타겟이면 건너뛰기
                if (showDebugInfo)
                    Debug.Log($"무효한 타겟 건너뛰기: {(target != null ? target.name : "null")} (남은 큐: {attackQueue.Count})");
            }
        }

        isProcessingQueue = false;
        if (showDebugInfo)
            Debug.Log("공격 큐 처리 완료");
    }

    /// <summary>
    /// 완전한 캐스팅 처리 - 중간에 타겟 죽음 체크 추가 (IEnemy 사용)
    /// </summary>
    private System.Collections.IEnumerator CastSpellAtTargetComplete(Transform target)
    {
        isCasting = true;
        currentCastTarget = target;

        if (showDebugInfo)
            Debug.Log($"마법 발사 시작: {target.name}");

        // 1. 타겟 방향으로 회전 전에 다시 한 번 확인 (IEnemy 사용!)
        if (!IsValidTarget(target))
        {
            if (showDebugInfo)
                Debug.Log($"회전 중 타겟이 무효화됨: {target.name}");

            isCasting = false;
            currentCastTarget = null;
            yield break;
        }

        // 2. 타겟 방향으로 회전
        yield return StartCoroutine(RotateToTarget(target));

        // 3. 애니메이션 재생 전에 다시 한 번 확인 (IEnemy 사용!)
        if (!IsValidTarget(target))
        {
            if (showDebugInfo)
                Debug.Log($"애니메이션 재생 전 타겟이 무효화됨: {target.name}");

            isCasting = false;
            currentCastTarget = null;
            yield break;
        }

        // 4. 발사 애니메이션 재생
        if (animator != null)
            animator.SetTrigger(castAnimationTrigger);

        // 5. 애니메이션 완료까지 대기
        yield return StartCoroutine(WaitForCastAnimation());

        if (showDebugInfo)
            Debug.Log($"마법 발사 완료: {target.name}");
    }

    /// <summary>
    /// 캐스팅 애니메이션 완료 대기 - 타임아웃 추가
    /// </summary>
    private System.Collections.IEnumerator WaitForCastAnimation()
    {
        float timeout = 3f; // 3초 타임아웃
        float elapsedTime = 0f;

        // isCasting이 false가 될 때까지 대기 (타임아웃 포함)
        while (isCasting && elapsedTime < timeout)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 타임아웃이 발생했으면 강제 완료
        if (isCasting && elapsedTime >= timeout)
        {
            if (showDebugInfo)
                Debug.LogWarning("캐스팅 애니메이션 타임아웃 - 강제 완료");

            ForceCastComplete();
        }
    }

    private System.Collections.IEnumerator CastSpellAtTarget(Transform target)
    {
        isCasting = true;
        currentCastTarget = target; // 타겟 저장

        if (showDebugInfo)
            Debug.Log($"마법 발사 시작: {target.name}");

        // 1. 타겟 방향으로 회전
        yield return StartCoroutine(RotateToTarget(target));

        // 2. 발사 애니메이션 재생 (애니메이션 이벤트가 발사 처리)
        if (animator != null) animator.SetTrigger(castAnimationTrigger);

        // 애니메이션 이벤트에서 발사하므로 여기서는 대기만
        // OnCastComplete() 애니메이션 이벤트에서 isCasting = false 처리
    }

    /// <summary>
    /// 애니메이션 이벤트: 발사 타이밍 - 재검증 추가 (IEnemy 사용)
    /// </summary>
    public void OnCastFire()
    {
        // 발사 직전에 타겟이 여전히 유효한지 확인 (IEnemy 사용!)
        if (IsValidTarget(currentCastTarget))
        {
            FireProjectile(currentCastTarget);
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"발사 시점에 타겟이 무효화됨: {(currentCastTarget != null ? currentCastTarget.name : "null")}");

            // 무효한 타겟이면 발사하지 않음
        }
    }

    /// <summary>
    /// 애니메이션 이벤트: 발사 애니메이션 완료
    /// </summary>
    public void OnCastComplete()
    {
        isCasting = false;
        currentCastTarget = null;

        if (showDebugInfo)
            Debug.Log("발사 애니메이션 완료");
    }

    /// <summary>
    /// 타겟이 공격 가능한 상태인지 확인 - IEnemy 인터페이스로 깔끔하게 처리!
    /// </summary>
    private bool IsValidTarget(Transform target)
    {
        // 1. Transform이 null인지 확인
        if (target == null)
        {
            if (showDebugInfo)
                Debug.Log("타겟 검증 실패: Transform이 null");
            return false;
        }

        // 2. GameObject가 활성화되어 있는지 확인
        if (!target.gameObject.activeInHierarchy)
        {
            if (showDebugInfo)
                Debug.Log($"타겟 검증 실패: {target.name} - GameObject가 비활성화됨");
            return false;
        }

        // 3. IEnemy 인터페이스를 구현한 컴포넌트 찾기 (깔끔!)
        IEnemy enemy = target.GetComponent<IEnemy>();
        if (enemy == null)
        {
            if (showDebugInfo)
                Debug.Log($"타겟 검증 실패: {target.name} - IEnemy 구현 컴포넌트가 없음");
            return false;
        }

        // 4. 적이 죽지 않았는지 확인 (IEnemy 인터페이스 사용!)
        if (enemy.IsDead)
        {
            if (showDebugInfo)
                Debug.Log($"타겟 검증 실패: {target.name} - 적이 죽음");
            return false;
        }

        // 5. 체력이 0 이하인지 확인 (IEnemy 인터페이스 사용!)
        if (enemy.HitPoint <= 0)
        {
            if (showDebugInfo)
                Debug.Log($"타겟 검증 실패: {target.name} - 체력이 0 이하 ({enemy.HitPoint})");
            return false;
        }

        if (showDebugInfo)
            Debug.Log($"타겟 검증 성공: {target.name} - {enemy.GetType().Name} (체력: {enemy.HitPoint})");

        return true;
    }

    private System.Collections.IEnumerator RotateToTarget(Transform target)
    {
        // 타겟 방향 계산 (높이 차이 무시)
        Vector3 targetDirection = (target.position - transform.position).normalized;
        targetDirection.y = 0; // 수평면에서만 회전

        // 모델 오프셋 적용
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection) * Quaternion.Euler(modelRotationOffset);

        if (!smoothCombatRotation)
        {
            // 즉시 회전
            transform.rotation = targetRotation;
            yield break;
        }

        // 부드러운 회전
        Quaternion startRotation = transform.rotation;

        float rotationTime = 0f;
        float totalRotationTime = Quaternion.Angle(startRotation, targetRotation) / combatRotationSpeed;

        while (rotationTime < totalRotationTime)
        {
            rotationTime += Time.deltaTime;
            float progress = rotationTime / totalRotationTime;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private void FireProjectile(Transform target)
    {
        if (magicProjectilePrefab == null || magicFirePosition == null)
        {
            Debug.LogError("투사체 프리팹 또는 발사 위치가 설정되지 않았습니다!");
            return;
        }

        // Fire Sound 재생
        PlayFireSound();

        // VFX 생성
        GameObject projectile =
            Instantiate(magicProjectilePrefab, magicFirePosition.position, magicFirePosition.rotation);

        // Projectile 컴포넌트 추가 및 설정
        MagicProjectile projectileScript = projectile.GetComponent<MagicProjectile>();
        if (projectileScript == null) projectileScript = projectile.AddComponent<MagicProjectile>();

        // 발사 방향 계산 (타겟 방향)
        Vector3 fireDirection = (target.position - magicFirePosition.position).normalized;

        // 투사체 설정
        projectileScript.Initialize(target, fireDirection, projectileSpeed);

        if (showDebugInfo)
            Debug.Log($"투사체 발사: {target.name} 방향");
    }

    /// <summary>
    /// 마법 발사 소리 재생
    /// </summary>
    private void PlayFireSound()
    {
        if (fireSoundClip != null && audioSource != null)
        {
            // 볼륨을 적용해서 재생
            audioSource.PlayOneShot(fireSoundClip, fireSoundVolume);

            if (showDebugInfo)
                Debug.Log($"Fire Sound 재생: {fireSoundClip.name} (Volume: {fireSoundVolume})");
        }
        else if (showDebugInfo)
        {
            if (fireSoundClip == null)
                Debug.LogWarning("Fire Sound Clip이 설정되지 않았습니다.");
            if (audioSource == null)
                Debug.LogWarning("AudioSource가 없습니다.");
        }
    }

    /// <summary>
    /// 피격 소리 재생 (랜덤)
    /// </summary>
    private void PlayHitSound()
    {
        if (hitSoundClips != null && hitSoundClips.Length > 0 && audioSource != null)
        {
            // 배열에서 랜덤하게 선택
            int randomIndex = Random.Range(0, hitSoundClips.Length);
            AudioClip selectedClip = hitSoundClips[randomIndex];

            if (selectedClip != null)
            {
                // 볼륨을 적용해서 재생
                audioSource.PlayOneShot(selectedClip, hitSoundVolume);

                if (showDebugInfo)
                    Debug.Log($"Hit Sound 재생: {selectedClip.name} (Index: {randomIndex})");
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"Hit Sound Clip이 null입니다. (Index: {randomIndex})");
            }
        }
        else if (showDebugInfo)
        {
            if (hitSoundClips == null || hitSoundClips.Length == 0)
                Debug.LogWarning("Hit Sound Clips가 설정되지 않았습니다.");
            if (audioSource == null)
                Debug.LogWarning("AudioSource가 없습니다.");
        }
    }

    #endregion

    #region Hit System

    /// <summary>
    /// 피격 처리 - 무적 시간 적용 + 캐스팅 중단 처리
    /// </summary>
    public void OnHit(Vector3 hitSourcePos)
    {
        Vector3 dir = (hitSourcePos - transform.position).normalized;
        float angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);

        int hitDir;
        if (angle > -45 && angle <= 45) hitDir = 0; // 앞
        else if (angle > 45 && angle <= 135) hitDir = 1; // 오른쪽
        else if (angle <= -45 && angle > -135) hitDir = 3; // 왼쪽
        else hitDir = 2; // 뒤

        // 피격 시 현재 캐스팅 중이면 강제 완료 처리
        if (isCasting)
        {
            if (showDebugInfo)
                Debug.Log("피격으로 인한 캐스팅 중단");

            ForceCastComplete();
        }

        // Hit Sound 재생
        PlayHitSound();

        animator.SetInteger("HitDir", hitDir);
        animator.SetTrigger("Hit");

        // 무적 상태 시작
        StartCoroutine(InvincibleCoroutine());

        // 피격 시 이동 파라미터를 잠시 0으로 만들어 더 자연스럽게
        StartCoroutine(ResetAnimationAfterHit());
    }

    /// <summary>
    /// 캐스팅 강제 완료 처리
    /// </summary>
    private void ForceCastComplete()
    {
        isCasting = false;
        currentCastTarget = null;

        // Cast 트리거 리셋 (중요!)
        if (animator != null) animator.ResetTrigger(castAnimationTrigger);

        if (showDebugInfo)
            Debug.Log("캐스팅 강제 완료됨");
    }

    /// <summary>
    /// 무적 시간 처리 코루틴
    /// </summary>
    private System.Collections.IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;

        // 깜빡임 효과 시작
        StartCoroutine(BlinkEffect());

        // 무적 시간 대기
        yield return new WaitForSeconds(invincibleTime);

        // 무적 상태 해제
        isInvincible = false;

        // 렌더러를 다시 보이게 설정 (깜빡임 효과 종료)
        SetRenderersEnabled(true);
    }

    /// <summary>
    /// 무적 시간 동안 깜빡임 효과
    /// </summary>
    private System.Collections.IEnumerator BlinkEffect()
    {
        while (isInvincible)
        {
            // 렌더러 끄기
            SetRenderersEnabled(false);
            yield return new WaitForSeconds(blinkInterval);

            // 렌더러 켜기
            SetRenderersEnabled(true);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    /// <summary>
    /// 모든 렌더러의 활성화 상태 설정
    /// </summary>
    private void SetRenderersEnabled(bool enabled)
    {
        foreach (Renderer renderer in renderers)
            if (renderer != null)
                renderer.enabled = enabled;
    }

    /// <summary>
    /// 피격 후 애니메이션 리셋 처리
    /// </summary>
    private System.Collections.IEnumerator ResetAnimationAfterHit()
    {
        // 잠시 이동 애니메이션 정지
        float originalSmoothTime = animationSmoothTime;
        animationSmoothTime = 0.05f; // 빠르게 0으로

        yield return new WaitForSeconds(0.1f);

        // 원래 설정으로 복구
        animationSmoothTime = originalSmoothTime;
    }

    #endregion

    #region Death System

    /// <summary>
    /// 플레이어 죽음 처리
    /// </summary>
    private void OnDeath()
    {
        if (isDead) return; // 이미 죽은 상태면 무시

        isDead = true;
        currentHitPoint = 0;

        if (showDebugInfo)
            Debug.Log("플레이어 사망!");

        // 현재 진행 중인 모든 행동 중단
        StopAllActions();

        // 죽음 사운드 재생
        PlayDeathSound();

        // 죽음 애니메이션 재생
        animator.SetBool(IsAliveBool, false);
        animator.SetTrigger(DeathTrigger);

        // 체력 UI 업데이트 (0으로)
        OnPlayerHealthChanged?.Invoke(currentHitPoint, maxHitPoint);

        // 부활 처리 시작
        if (autoRespawn) StartCoroutine(RespawnCoroutine());

        // 죽음 이벤트 발생 (다른 시스템에서 사용 가능)
        OnPlayerDeath?.Invoke();
    }

    /// <summary>
    /// 모든 행동 중단
    /// </summary>
    private void StopAllActions()
    {
        // 캐스팅 중단
        if (isCasting) ForceCastComplete();

        // 공격 큐 초기화
        attackQueue.Clear();
        isProcessingQueue = false;

        // 무적 상태 해제
        if (isInvincible)
        {
            StopCoroutine(InvincibleCoroutine());
            StopCoroutine(BlinkEffect());
            isInvincible = false;
            SetRenderersEnabled(true);
        }

        // 이동 파라미터 초기화
        currentInput = Vector2.zero;
        targetAnimParams = Vector2.zero;
        currentAnimParams = Vector2.zero;
    }

    /// <summary>
    /// 부활 코루틴
    /// </summary>
    private System.Collections.IEnumerator RespawnCoroutine()
    {
        if (showDebugInfo)
            Debug.Log($"부활까지 {respawnDelay}초 대기...");

        // 부활 대기
        yield return new WaitForSeconds(respawnDelay);

        // 부활 처리
        Respawn();
    }

    /// <summary>
    /// 플레이어 부활
    /// </summary>
    public void Respawn()
    {
        if (showDebugInfo)
            Debug.Log("플레이어 부활!");

        // 상태 초기화
        isDead = false;
        currentHitPoint = maxHitPoint;
        isInvincible = false;

        // 스태미너 완전 회복
        currentStamina = maxStamina;

        // 위치 초기화
        transform.position = respawnPosition;
        transform.rotation = Quaternion.identity;

        // 애니메이터 상태 초기화
        animator.SetBool(IsAliveBool, true);

        // 렌더러 활성화
        SetRenderersEnabled(true);

        // 체력 UI 업데이트
        OnPlayerHealthChanged?.Invoke(currentHitPoint, maxHitPoint);

        // 스태미너 UI 업데이트
        OnPlayerStaminaChanged?.Invoke(currentStamina, maxStamina);

        // 잠시 무적 시간 부여 (부활 직후 보호)
        StartCoroutine(RespawnInvincibility());
    }

    /// <summary>
    /// 부활 직후 무적 시간
    /// </summary>
    private System.Collections.IEnumerator RespawnInvincibility()
    {
        isInvincible = true;

        if (showDebugInfo)
            Debug.Log("부활 보호 무적 시작");

        // 깜빡임 효과
        StartCoroutine(BlinkEffect());

        // 부활 보호 시간 (조금 더 길게)
        yield return new WaitForSeconds(invincibleTime * 2f);

        // 무적 해제
        isInvincible = false;
        SetRenderersEnabled(true);

        if (showDebugInfo)
            Debug.Log("부활 보호 무적 종료");
    }

    /// <summary>
    /// 죽음 소리 재생
    /// </summary>
    private void PlayDeathSound()
    {
        if (deathSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSoundClip, deathSoundVolume);

            if (showDebugInfo)
                Debug.Log($"Death Sound 재생: {deathSoundClip.name}");
        }
        else if (showDebugInfo)
        {
            if (deathSoundClip == null)
                Debug.LogWarning("Death Sound Clip이 설정되지 않았습니다.");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 무적 상태 확인 (외부에서 참조 가능)
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
    }

    /// <summary>
    /// 전투 중인지 확인
    /// </summary>
    public bool IsCasting()
    {
        return isCasting;
    }

    /// <summary>
    /// 현재 체력 반환
    /// </summary>
    public int GetCurrentHitPoint()
    {
        return currentHitPoint;
    }

    /// <summary>
    /// 체력 설정 (외부에서 호출 가능)
    /// </summary>
    public void SetHitPoint(int newHitPoint)
    {
        currentHitPoint = Mathf.Max(0, newHitPoint);
        OnPlayerHealthChanged?.Invoke(currentHitPoint, maxHitPoint);
    }

    #endregion

    #region Debug

    /// <summary>
    /// 에디터에서 기즈모를 통한 디버깅
    /// </summary>
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // 현재 입력 방향 시각화
            Gizmos.color = Color.blue;
            Vector3 inputDir = new(currentInput.x, 0, currentInput.y);
            Gizmos.DrawRay(transform.position + Vector3.up, inputDir * 2f);

            // 애니메이션 파라미터 시각화
            Gizmos.color = Color.red;
            Vector3 animDir = new(currentAnimParams.x, 0, currentAnimParams.y);
            Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, animDir);

            // 무적 상태 시각화
            if (isInvincible)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 1f);
            }

            // 전투 중 상태 시각화
            if (isCasting)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.8f);
            }

            // 마법 발사 위치 시각화
            if (magicFirePosition != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(magicFirePosition.position, 0.2f);
            }
        }
    }

    #endregion
}
