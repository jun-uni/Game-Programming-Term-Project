using UnityEngine;

public class InvincibleBuff : BaseBuff
{
    private PlayerController playerController;

    public InvincibleBuff(BuffData data, GameObject target) : base(data, target)
    {
        playerController = target.GetComponent<PlayerController>();
    }

    protected override void OnApply()
    {
        if (playerController != null)
            // PlayerController에 무적 상태를 강제로 설정하는 메서드가 필요
            // 현재는 디버그 로그만 출력
            Debug.Log($"무적 버프 적용: {Duration}초간 무적");
    }

    protected override void OnRemove()
    {
        Debug.Log("무적 버프 해제");
    }

    protected override void OnUpdate(float deltaTime)
    {
        // 무적 상태 유지 로직 (PlayerController의 isInvincible을 계속 true로 유지)
        // 실제 구현을 위해서는 PlayerController에 공개 메서드가 필요
    }
}
