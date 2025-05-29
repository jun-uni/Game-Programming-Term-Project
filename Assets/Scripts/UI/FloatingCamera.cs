using UnityEngine;

public class FloatingCamera : MonoBehaviour
{
    [Header("회전 속도")] public float rotationSpeed = 5f;

    [Header("방향 변화 속도")] public float directionChangeSpeed = 0.1f;

    private Vector3 currentRotationVelocity;

    private void Start()
    {
        // 초기 회전 속도 랜덤 설정
        currentRotationVelocity = GetRandomRotationVector();
    }

    private void Update()
    {
        // 회전 방향을 서서히 랜덤하게 변경
        Vector3 randomChange = GetRandomRotationVector() * (directionChangeSpeed * Time.deltaTime);
        currentRotationVelocity += randomChange;

        // 전체 크기 제한 (너무 빠르게 회전하지 않도록)
        currentRotationVelocity = Vector3.ClampMagnitude(currentRotationVelocity, 1f);

        // 회전 적용
        transform.Rotate(currentRotationVelocity * (rotationSpeed * Time.deltaTime), Space.Self);
    }

    // 랜덤 방향 벡터 생성 (약한 랜덤성)
    private Vector3 GetRandomRotationVector()
    {
        return new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0f // 롤(roll)은 생략해서 화면이 기울어지지 않도록
        );
    }
}
