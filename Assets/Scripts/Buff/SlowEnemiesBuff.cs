using System.Collections.Generic;
using UnityEngine;

public class SlowEnemiesBuff : BaseBuff
{
    public static bool IsSlowEnemiesBuffActive { get; private set; } = false;
    public static float EnemySpeedMultiplier { get; private set; } = 1f;

    // 원래 속도를 static 변수로 저장 (모든 근거리 적이 같은 속도)
    private static float originalEnemySpeed = -1f; // -1은 아직 저장 안됨을 의미

    public SlowEnemiesBuff(BuffData data, GameObject target) : base(data, target)
    {
    }

    protected override void OnApply()
    {
        IsSlowEnemiesBuffActive = true;
        EnemySpeedMultiplier = data.value;

        // 현재 활성화된 모든 적들에게 속도 감소 적용
        ApplySlowToAllEnemies();

        Debug.Log($"적 속도 감소 버프 적용: 배율 {EnemySpeedMultiplier}x");
    }

    protected override void OnRemove()
    {
        IsSlowEnemiesBuffActive = false;
        EnemySpeedMultiplier = 1f;

        // 현재 활성화된 모든 적들의 속도 원복
        RestoreSpeedToAllEnemies();

        Debug.Log("적 속도 감소 버프 해제");
    }

    private void ApplySlowToAllEnemies()
    {
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
            ApplySlowToEnemiesInPool(spawner.meleeEnemyPool);
    }

    private void RestoreSpeedToAllEnemies()
    {
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
            RestoreSpeedToEnemiesInPool(spawner.meleeEnemyPool);
    }

    private void ApplySlowToEnemiesInPool(List<GameObject> enemyPool)
    {
        foreach (GameObject enemyObj in enemyPool)
            if (enemyObj != null && enemyObj.activeSelf)
            {
                EnemyController enemy = enemyObj.GetComponent<EnemyController>();
                if (enemy != null && !enemy.isDie)
                {
                    UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                    {
                        // 첫 번째 적에서 원래 속도 저장
                        if (originalEnemySpeed < 0) originalEnemySpeed = agent.speed;

                        // 속도 감소 적용
                        agent.speed = originalEnemySpeed * EnemySpeedMultiplier;
                    }
                }
            }
    }

    private void RestoreSpeedToEnemiesInPool(List<GameObject> enemyPool)
    {
        foreach (GameObject enemyObj in enemyPool)
            if (enemyObj != null && enemyObj.activeSelf)
            {
                EnemyController enemy = enemyObj.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null)
                        // 원래 속도로 복원
                        agent.speed = originalEnemySpeed;
                }
            }
    }

    /// <summary>
    /// 새로 소환된 적에게 슬로우 효과 적용
    /// </summary>
    public static void ApplySlowToNewEnemy(UnityEngine.AI.NavMeshAgent agent)
    {
        if (IsSlowEnemiesBuffActive && agent != null)
        {
            // 원래 속도가 아직 저장 안됐으면 저장
            if (originalEnemySpeed < 0) originalEnemySpeed = agent.speed;

            // 슬로우 효과 적용
            agent.speed = originalEnemySpeed * EnemySpeedMultiplier;
        }
    }
}
