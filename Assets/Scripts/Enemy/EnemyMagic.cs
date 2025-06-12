using System.Collections;
using UnityEngine;

public class EnemyMagic : MonoBehaviour
{
    [Header("타이밍 설정")] public float damageStartTime = 3.2f; // 데미지 시작 시점
    public float damageEndTime = 5.8f; // 데미지 종료 시점

    [Header("콜라이더 참조")] public Collider damageCollider; // Inspector에서 미리 설정!

    private bool isDamageActive = false;

    private void Start()
    {
        // 처음에는 콜라이더 비활성화
        if (damageCollider != null) damageCollider.enabled = false;

        // 타이밍 제어
        StartCoroutine(MeteorSequence());
    }

    private IEnumerator MeteorSequence()
    {
        // 1단계: 경고 단계
        yield return new WaitForSeconds(damageStartTime);

        // 2단계: 데미지 콜라이더 ON
        damageCollider.enabled = true;
        isDamageActive = true;

        // 데미지 지속 시간
        float damageActiveDuration = damageEndTime - damageStartTime;
        yield return new WaitForSeconds(damageActiveDuration);

        // 3단계: 데미지 콜라이더 OFF
        damageCollider.enabled = false;
        isDamageActive = false;

        // 잔여 시간 후 정리
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
}
