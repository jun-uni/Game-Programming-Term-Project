using UnityEngine;
using UnityEngine.VFX;

public class MagicProjectile : MonoBehaviour
{
    [Header("투사체 설정")] [SerializeField] private float speed = 10f;
    [SerializeField] private float maxDistance = 50f; // 최대 이동 거리
    [SerializeField] private float targetRadius = 1f; // 타겟 충돌 반경

    [Header("VFX 설정")] [SerializeField] private VisualEffect vfx; // Visual Effect Graph 컴포넌트
    [SerializeField] private string velocityDirectionProperty = "velocityDirection"; // VFX 속성 이름

    [Header("Hit Effect 설정")] [SerializeField]
    private GameObject hitEffectPrefab; // 명중 시 생성할 이펙트 프리팹

    [SerializeField] private float hitEffectDuration = 2f; // Hit Effect 지속 시간 (0이면 자동 파괴 안함)

    [Header("Hit Sound 설정")] [SerializeField]
    private AudioClip hitSoundClip; // 명중 시 재생할 사운드

    [SerializeField] private AudioSource audioSource; // 오디오 소스 (없으면 자동 생성)
    [SerializeField] private float hitSoundVolume = 0.5f;

    [Header("충돌 설정")] [SerializeField] private float damage = 30f; // 데미지
    [SerializeField] private float destroyDelay = 0f; // 충돌 후 파괴 딜레이

    [Header("디버그")] [SerializeField] private bool showDebugInfo = true;

    private Transform target;
    private Vector3 moveDirection;
    private float traveledDistance = 0f;
    private bool hasHitTarget = false;
    private Vector3 startPosition;

    private void Awake()
    {
        // VFX 컴포넌트 자동 찾기
        if (vfx == null)
            vfx = GetComponent<VisualEffect>();

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
    }

    /// <summary>
    /// 투사체 초기화
    /// </summary>
    public void Initialize(Transform targetTransform, Vector3 direction, float projectileSpeed)
    {
        target = targetTransform;
        moveDirection = direction.normalized;
        speed = projectileSpeed;
        startPosition = transform.position;

        // VFX 방향 설정 (이동 방향의 반대로 설정)
        SetVFXVelocityDirection(-moveDirection);

        if (showDebugInfo)
            Debug.Log($"투사체 초기화: 타겟={target.name}, 방향={moveDirection}, 속도={speed}");
    }

    private void Update()
    {
        if (hasHitTarget) return;

        MovementUpdate();
        CheckTargetHit();
        CheckMaxDistance();
    }

    private void MovementUpdate()
    {
        // 타겟이 살아있으면 방향 업데이트 (유도 미사일 효과)
        if (target != null)
        {
            Vector3 targetDirection = (target.position - transform.position).normalized;

            // 부드러운 방향 전환 (선택사항)
            moveDirection = Vector3.Slerp(moveDirection, targetDirection, Time.deltaTime * 2f);

            // VFX 방향도 업데이트
            SetVFXVelocityDirection(-moveDirection);
        }

        // 이동
        Vector3 movement = moveDirection * speed * Time.deltaTime;
        transform.position += movement;
        traveledDistance += movement.magnitude;

        // 회전 (이동 방향으로)
        if (moveDirection != Vector3.zero) transform.rotation = Quaternion.LookRotation(moveDirection);
    }

    private void CheckTargetHit()
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= targetRadius) HitTarget();
    }

    private void CheckMaxDistance()
    {
        if (traveledDistance >= maxDistance)
        {
            if (showDebugInfo)
                Debug.Log("투사체가 최대 거리에 도달하여 파괴됩니다.");

            DestroyProjectile();
        }
    }

    private void HitTarget()
    {
        if (hasHitTarget) return;

        hasHitTarget = true;

        if (showDebugInfo)
            Debug.Log($"타겟 명중: {target.name}");

        // Hit Effect 생성
        SpawnHitEffect();

        // Hit Sound 재생
        PlayHitSound();

        // 타겟에 데미지 적용
        ApplyDamageToTarget();

        // 충돌 효과 (VFX 정지 또는 변경)
        StopVFX();

        // 일정 시간 후 파괴
        Destroy(gameObject, destroyDelay);
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            // 타겟 위치에 Hit Effect 생성
            Vector3 hitPosition = target != null ? target.position : transform.position;
            GameObject hitEffect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);

            if (showDebugInfo)
                Debug.Log($"Hit Effect 생성: {hitEffect.name} at {hitPosition}");

            // Hit Effect 자동 파괴 (duration이 0보다 크면)
            if (hitEffectDuration > 0f) Destroy(hitEffect, hitEffectDuration);
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("Hit Effect Prefab이 설정되지 않았습니다.");
        }
    }

    private void PlayHitSound()
    {
        if (hitSoundClip != null && audioSource != null)
        {
            // 볼륨을 적용해서 재생
            audioSource.PlayOneShot(hitSoundClip, hitSoundVolume);

            if (showDebugInfo)
                Debug.Log($"Hit Sound 재생: {hitSoundClip.name} (Volume: {hitSoundVolume})");
        }
        else if (showDebugInfo)
        {
            if (hitSoundClip == null)
                Debug.LogWarning("Hit Sound Clip이 설정되지 않았습니다.");
            if (audioSource == null)
                Debug.LogWarning("AudioSource가 없습니다.");
        }
    }

    private void ApplyDamageToTarget()
    {
        if (target == null) return;

        // EnemyController에 데미지 적용
        EnemyController enemyController = target.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.hitPoint -= (int)damage;

            if (showDebugInfo)
                Debug.Log($"{target.name}에게 {damage} 데미지 적용. 남은 체력: {enemyController.hitPoint}");

            // 체력이 0 이하가 되면 사망 처리
            if (enemyController.hitPoint <= 0) enemyController.state = EnemyState.DIE;
        }
    }

    private void SetVFXVelocityDirection(Vector3 direction)
    {
        if (vfx != null && vfx.HasVector3(velocityDirectionProperty))
        {
            vfx.SetVector3(velocityDirectionProperty, direction);

            if (showDebugInfo)
                Debug.Log($"VFX 방향 설정: {direction}");
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning($"VFX 또는 '{velocityDirectionProperty}' 속성을 찾을 수 없습니다.");
        }
    }

    private void StopVFX()
    {
        if (vfx != null)
        {
            // VFX 정지 또는 충돌 효과로 전환
            vfx.Stop();

            if (showDebugInfo)
                Debug.Log("VFX 정지됨");
        }
    }

    private void DestroyProjectile()
    {
        StopVFX();
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // 타겟 충돌 반경 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetRadius);

        // 이동 방향 표시
        if (moveDirection != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }

        // VFX 방향 표시 (반대 방향)
        if (moveDirection != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, -moveDirection * 2f);
        }
    }
}
