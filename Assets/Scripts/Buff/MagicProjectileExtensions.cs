using UnityEngine;

public static class MagicProjectileExtensions
{
    /// <summary>
    /// MagicProjectile의 데미지를 버프 적용해서 계산
    /// 기존 MagicProjectile 클래스의 ApplyDamageToTarget 메서드에서 사용
    /// </summary>
    public static int GetBuffedDamage(int baseDamage)
    {
        if (AttackPowerUpBuff.IsAttackPowerBuffActive)
        {
            int buffedDamage = Mathf.RoundToInt(baseDamage * AttackPowerUpBuff.AttackPowerMultiplier);
            Debug.Log($"공격력 버프 적용: {baseDamage} → {buffedDamage}");
            return buffedDamage;
        }

        return baseDamage;
    }
}
