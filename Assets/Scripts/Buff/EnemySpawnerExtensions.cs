public static class EnemySpawnerExtensions
{
    /// <summary>
    /// 버프를 고려한 스폰 간격 계산
    /// EnemySpawner의 InvokeRepeating 간격에 사용
    /// </summary>
    public static float GetBuffedSpawnInterval(float baseInterval)
    {
        if (SlowEnemySpawnBuff.IsSlowSpawnBuffActive)
        {
            float buffedInterval = baseInterval * SlowEnemySpawnBuff.SpawnRateMultiplier;
            return buffedInterval;
        }

        return baseInterval;
    }
}
