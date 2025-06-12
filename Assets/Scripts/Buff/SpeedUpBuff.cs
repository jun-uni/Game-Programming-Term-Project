using UnityEngine;

public class SpeedUpBuff : BaseBuff
{
    private float originalWalkSpeed;
    private float originalRunSpeed;
    private PlayerController playerController;

    public SpeedUpBuff(BuffData data, GameObject target) : base(data, target)
    {
        playerController = target.GetComponent<PlayerController>();
    }

    protected override void OnApply()
    {
        if (playerController != null)
        {
            originalWalkSpeed = playerController.moveSpeed;
            originalRunSpeed = playerController.runSpeed;

            playerController.moveSpeed *= data.value;
            playerController.runSpeed *= data.value;

            Debug.Log(
                $"속도 버프 적용: 걷기 {originalWalkSpeed} → {playerController.moveSpeed}, 달리기 {originalRunSpeed} → {playerController.runSpeed}");
        }
    }

    protected override void OnRemove()
    {
        if (playerController != null)
        {
            playerController.moveSpeed = originalWalkSpeed;
            playerController.runSpeed = originalRunSpeed;

            Debug.Log($"속도 버프 해제: 걷기 {playerController.moveSpeed}, 달리기 {playerController.runSpeed}");
        }
    }
}
