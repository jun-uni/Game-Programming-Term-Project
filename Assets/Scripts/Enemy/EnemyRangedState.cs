/// <summary>
/// 원거리 적의 상태 열거형
/// </summary>
public enum EnemyRangedState
{
    IDLE, // 대기 상태
    RANGED_ATTACK, // 원거리 공격 상태
    HIT, // 피격 상태
    DIE // 사망 상태
}
