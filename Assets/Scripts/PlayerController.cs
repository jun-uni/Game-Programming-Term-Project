using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f; // 걷기 속도
    public float runSpeed = 8f; // 뛰기 속도

    [Header("Animation Smooth Settings")]
    [Range(0.1f, 10f)]
    public float moveBlendSpeed = 8f; // 이동 블렌딩 속도

    [Range(0.1f, 10f)]
    public float rotationBlendSpeed = 10f; // 회전 블렌딩 속도

    [Range(0.1f, 5f)]
    public float animationSmoothTime = 0.15f; // 애니메이션 부드러움 정도

    [Header("Camera Settings")]
    public Transform camTransform;

    [Header("Hit Settings")]
    [Range(0.5f, 5f)]
    public float invincibleTime = 1.5f; // 무적 시간 (초)

    [Range(0.1f, 1f)]
    public float blinkInterval = 0.1f; // 무적 시 깜빡임 간격

    [Header("Components")]
    private Animator animator;
    private Renderer[] renderers; // 깜빡임 효과를 위한 렌더러들

    private float currentHitPoint = 100;
    private bool isInvincible = false; // 무적 상태

    // 부드러운 애니메이션을 위한 변수들
    private Vector2 currentAnimParams = Vector2.zero;
    private Vector2 targetAnimParams = Vector2.zero;
    private Vector2 animVelocity = Vector2.zero;

    // Input dampening을 위한 변수들
    private Vector2 currentInput = Vector2.zero;
    private Vector2 inputVelocity = Vector2.zero;

    private void Start()
    {
        animator = GetComponent<Animator>();

        // 모든 렌더러 컴포넌트 가져오기 (깜빡임 효과용)
        renderers = GetComponentsInChildren<Renderer>();

        // 카메라 참조가 없으면 메인 카메라 사용
        if (camTransform == null)
            camTransform = Camera.main.transform;
    }

    private void Update()
    {
        HandleInput();
        HandleMovement();
        HandleAnimation();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("콜라이더 닿음!");

        // 무적 상태일 때는 피격 처리하지 않음
        if (isInvincible)
        {
            Debug.Log("무적 상태라 피격 무시됨");
            return;
        }

        // Enemy Hand 태그 확인
        if (other.CompareTag("Enemy Hand"))
        {
            // 부모 오브젝트에서 EnemyController 찾기
            EnemyController enemy = other.GetComponentInParent<EnemyController>();

            if (enemy != null)
            {
                // 실제 공격 판정이 활성화되어 있을 때만 피격 처리
                if (enemy.isAttackActive && currentHitPoint > 0.0f)
                {
                    currentHitPoint -= 10.0f;
                    Debug.Log($"피격! 현재 체력: {currentHitPoint}");

                    // 피격 처리
                    OnHit(enemy.transform.position);
                }
            }
        }
    }

    /// <summary>
    /// 입력 처리 - 부드러운 입력 변화를 위해 SmoothDamp 사용
    /// </summary>
    private void HandleInput()
    {
        // Raw 입력값 획득
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 targetInput = new(h, v);

        // 부드러운 입력 보간
        currentInput = Vector2.SmoothDamp(currentInput, targetInput, ref inputVelocity, 0.1f);
    }

    /// <summary>
    /// 캐릭터 이동 처리
    /// </summary>
    private void HandleMovement()
    {
        // 입력이 있는지 확인
        if (currentInput.magnitude > 0.1f)
        {
            // 달리기 여부 확인
            bool isRun = Input.GetKey(KeyCode.LeftShift);
            float speed = isRun ? runSpeed : moveSpeed;

            // 캐릭터의 로컬 방향 기준으로 이동
            Vector3 moveDirection = (transform.forward * currentInput.y + transform.right * currentInput.x).normalized;

            // 실제 이동
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);

            // 애니메이션 파라미터 목표값 설정
            float factor = isRun ? 2f : 1f;
            targetAnimParams = new Vector2(currentInput.x * factor, currentInput.y * factor);
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

    /// <summary>
    /// 피격 처리 - 무적 시간 적용
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

        animator.SetInteger("HitDir", hitDir);
        animator.SetTrigger("Hit");

        // 무적 상태 시작
        StartCoroutine(InvincibleCoroutine());

        // 피격 시 이동 파라미터를 잠시 0으로 만들어 더 자연스럽게
        StartCoroutine(ResetAnimationAfterHit());
    }

    /// <summary>
    /// 무적 시간 처리 코루틴
    /// </summary>
    private System.Collections.IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;
        Debug.Log("무적 상태 시작");

        // 깜빡임 효과 시작
        StartCoroutine(BlinkEffect());

        // 무적 시간 대기
        yield return new WaitForSeconds(invincibleTime);

        // 무적 상태 해제
        isInvincible = false;

        // 렌더러를 다시 보이게 설정 (깜빡임 효과 종료)
        SetRenderersEnabled(true);

        Debug.Log("무적 상태 종료");
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
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = enabled;
        }
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

    /// <summary>
    /// 무적 상태 확인 (외부에서 참조 가능)
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
    }

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
        }
    }
}
