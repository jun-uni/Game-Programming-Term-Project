/// <summary>
/// 몬스터의 현재 상태를 나타내는 열거형.
/// </summary>
public enum EnemyState
{
    /// <summary>
    /// 플레이어를 추적하는 상태.
    /// </summary>
    TRACE,

    /// <summary>
    /// 공격 상태.
    /// </summary>
    ATTACK,

    /// <summary>
    /// 피격 상태.
    /// </summary>
    HIT,

    /// <summary>
    /// 사망 상태.
    /// </summary>
    DIE
}
