using UnityEngine;

public class SlowEnemySpawnBuff : BaseBuff
{
    public static bool IsSlowSpawnBuffActive { get; private set; } = false;
    public static float SpawnRateMultiplier { get; private set; } = 1f;

    public SlowEnemySpawnBuff(BuffData data, GameObject target) : base(data, target)
    {
    }

    protected override void OnApply()
    {
        IsSlowSpawnBuffActive = true;
        SpawnRateMultiplier = data.value; // 예: 2.0f (스폰 간격 2배 증가 = 스폰 속도 절반)

        Debug.Log($"적 스폰 속도 감소 버프 적용: 스폰 간격 {SpawnRateMultiplier}배 증가");
    }

    protected override void OnRemove()
    {
        IsSlowSpawnBuffActive = false;
        SpawnRateMultiplier = 1f;

        Debug.Log("적 스폰 속도 감소 버프 해제");
    }


    /// <summary>
    /// 게임 종료/재시작 시 static 변수 초기화
    /// </summary>
    public static void ResetStaticValues()
    {
        IsSlowSpawnBuffActive = false;
        SpawnRateMultiplier = 1f;
        Debug.Log("SlowEnemySpawnBuff static 값 초기화");
    }
}
