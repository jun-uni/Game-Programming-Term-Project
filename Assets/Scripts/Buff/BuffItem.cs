using UnityEngine;

public class BuffItem : MonoBehaviour
{
    [Header("버프 설정")] [SerializeField] private BuffData buffData;

    [Header("애니메이션 설정")] [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;

    [Header("이펙트")] [SerializeField] private ParticleSystem particles;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float soundVolume = 0.5f;

    private Vector3 startPosition;
    private BuffManager buffManager;

    private void Start()
    {
        startPosition = transform.position;

        // 파티클 색상 설정
        if (particles != null && buffData != null)
        {
            ParticleSystem.MainModule main = particles.main;
            main.startColor = buffData.particleColor;
        }
    }

    private void Update()
    {
        // 회전 효과
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

        // 위아래 움직임 효과
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 체력 회복 아이템의 경우 체력이 가득하면 먹지 않음
        if (buffData.buffType == BuffType.HealthRestore)
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null && playerController.GetCurrentHitPoint() >= 3) // maxHitPoint가 3
                return;
        }

        // BuffManager 찾기
        buffManager = other.GetComponent<BuffManager>();
        if (buffManager == null)
        {
            Debug.LogError("플레이어에 BuffManager 컴포넌트가 없습니다!");
            return;
        }

        // 버프 적용
        buffManager.ApplyBuff(buffData, other.gameObject);

        // UI에 버프 설명 표시
        if (UIManager.Instance != null && buffData != null) UIManager.Instance.ShowBuffDescription(buffData);

        // 사운드 재생
        if (pickupSound != null)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // PlayerController의 AudioSource 사용 (이미 볼륨 설정됨)
                AudioSource playerAudio = player.GetComponent<AudioSource>();
                if (playerAudio != null) playerAudio.PlayOneShot(pickupSound, soundVolume);
            }
        }

        // 아이템 제거
        Destroy(gameObject);
    }

    /// <summary>
    /// 버프 데이터 설정 (스포너에서 동적으로 설정할 때 사용)
    /// </summary>
    public void SetBuffData(BuffData data)
    {
        buffData = data;

        // 파티클 색상 업데이트
        if (particles != null && buffData != null)
        {
            ParticleSystem.MainModule main = particles.main;
            main.startColor = buffData.particleColor;
        }
    }
}
