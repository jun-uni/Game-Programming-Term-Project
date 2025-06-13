using UnityEngine;

public class AttackPowerUpBuff : BaseBuff
{
    public static bool IsAttackPowerBuffActive { get; private set; } = false;
    public static float AttackPowerMultiplier { get; private set; } = 1f;

    public AttackPowerUpBuff(BuffData data, GameObject target) : base(data, target)
    {
    }

    protected override void OnApply()
    {
        IsAttackPowerBuffActive = true;
        AttackPowerMultiplier = data.value;

        Debug.Log($"공격력 버프 적용: 배율 {AttackPowerMultiplier}x");
    }

    protected override void OnRemove()
    {
        IsAttackPowerBuffActive = false;
        AttackPowerMultiplier = 1f;

        Debug.Log("공격력 버프 해제");
    }

    /// <summary>
    /// 게임 종료/재시작 시 static 변수 초기화
    /// </summary>
    public static void ResetStaticValues()
    {
        IsAttackPowerBuffActive = false;
        AttackPowerMultiplier = 1f;
        Debug.Log("AttackPowerUpBuff static 값 초기화");
    }
}
