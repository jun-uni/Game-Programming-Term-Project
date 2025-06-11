/// <summary>
/// 모든 적 타입이 구현해야 하는 공통 인터페이스
/// </summary>
public interface IEnemy
{
    /// <summary>
    /// 적이 죽었는지 여부
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// 적의 현재 체력
    /// </summary>
    int HitPoint { get; set; }

    /// <summary>
    /// 피격 처리
    /// </summary>
    /// <param name="damage">받을 데미지</param>
    void OnHit(int damage);
}
