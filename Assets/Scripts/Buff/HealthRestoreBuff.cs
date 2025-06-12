using UnityEngine;

public class HealthRestoreBuff : BaseBuff
{
    public HealthRestoreBuff(BuffData data, GameObject target) : base(data, target)
    {
    }

    protected override void OnApply()
    {
        PlayerController playerController = target.GetComponent<PlayerController>();
        if (playerController != null)
        {
            int currentHealth = playerController.GetCurrentHitPoint();
            int healAmount = (int)data.value;

            playerController.SetHitPoint(currentHealth + healAmount);

            Debug.Log($"체력 회복: {healAmount}만큼 회복 (현재: {playerController.GetCurrentHitPoint()})");
        }

        // 즉시 효과이므로 바로 제거
        Remove(target);
    }

    protected override void OnRemove()
    {
    }
}
