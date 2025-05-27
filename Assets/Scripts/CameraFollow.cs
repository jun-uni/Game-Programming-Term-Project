using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;

    [Header("Position Settings")] public Vector3 offset = new Vector3(0, 6, -5);  // 카메라의 기본 오프셋
    public float height = 10f;        // 카메라 높이
    public float distance = 8f;     // 플레이어로부터의 거리
    public float angle = 45f;        // 카메라 각도 (쿼터뷰는 보통 30-60도)
    
    [Header("Follow Settings")]
    public float followSpeed = 5f;   // 카메라 따라오는 속도
    public bool smoothFollow = true; // 부드러운 따라오기 여부
    
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // 초기 카메라 위치 설정
        SetupQuarterViewPosition();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;

        // 카메라 이동
        if (smoothFollow)
        {
            // 부드러운 이동
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / followSpeed);
        }
        else
        {
            // 즉시 이동
            transform.position = desiredPosition;
        }

        // 카메라가 항상 타겟을 바라보도록 설정
        Vector3 lookDirection = target.position - transform.position;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    // 쿼터뷰 위치를 설정하는 함수
    void SetupQuarterViewPosition()
    {
        if (target == null) return;

        // 각도를 라디안으로 변환
        float angleRad = angle * Mathf.Deg2Rad;
        
        // 쿼터뷰를 위한 대각선 오프셋 계산
        float x = distance * Mathf.Sin(angleRad * Mathf.Deg2Rad);
        float z = -distance * Mathf.Cos(angleRad * Mathf.Deg2Rad);
        
        offset = new Vector3(x, height, z);
        
        // 초기 위치 설정
        transform.position = target.position + offset;
        
        // 타겟을 바라보도록 회전
        Vector3 lookDirection = target.position - transform.position;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    // 런타임에서 카메라 설정을 조정할 수 있는 함수들
    public void SetCameraAngle(float newAngle)
    {
        angle = newAngle;
        SetupQuarterViewPosition();
    }

    public void SetCameraHeight(float newHeight)
    {
        height = newHeight;
        SetupQuarterViewPosition();
    }

    public void SetCameraDistance(float newDistance)
    {
        distance = newDistance;
        SetupQuarterViewPosition();
    }

    // 디버그용: Scene 뷰에서 카메라 위치 시각화
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position + offset, 0.5f);
        }
    }
}