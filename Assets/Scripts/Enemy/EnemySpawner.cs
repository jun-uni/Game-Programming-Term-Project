using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 설정")] [SerializeField] private GameObject meleeEnemyPrefab; // 근거리 적 프리팹
    [SerializeField] private GameObject rangedEnemyPrefab; // 원거리 적 프리팹
    [SerializeField] private float meleeSpawnRadius = 13.5f; // 근거리 적 스폰 반경
    [SerializeField] private float rangedSpawnRadius = 10f; // 원거리 적 스폰 반경 (더 작게)
    [SerializeField] private float minDistanceFromPlayer = 5f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform player;
    [SerializeField] private bool showGizmoAlways = true;

    [Header("적 타입 확률 설정")] [SerializeField] [Range(0f, 1f)]
    private float meleeEnemyChance = 0.7f; // 근거리 적 스폰 확률 (70%)

    [SerializeField] [Range(0f, 1f)] private float rangedEnemyChance = 0.3f; // 원거리 적 스폰 확률 (30%)

    [Header("오브젝트 풀 설정")] [SerializeField] private List<GameObject> meleeEnemyPool = new(); // 근거리 적 풀
    [SerializeField] private List<GameObject> rangedEnemyPool = new(); // 원거리 적 풀
    [SerializeField] private int maxMeleeEnemyCount = 30;
    [SerializeField] private int maxRangedEnemyCount = 10;
    [SerializeField] private int maxActiveEnemies = 10; // 동시에 활성화될 수 있는 최대 적 수

    [Header("디버그")] [SerializeField] private bool showDebugInfo = true;

    private int currentActiveEnemies = 0;

    private void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
        CreateEnemyPools();
        InvokeRepeating(nameof(SpawnEnemy), 2f, spawnInterval);
    }

    /// <summary>
    /// 근거리와 원거리 적 오브젝트 풀 생성
    /// </summary>
    private void CreateEnemyPools()
    {
        // 근거리 적 풀 생성
        if (meleeEnemyPrefab != null)
            for (int i = 0; i < maxMeleeEnemyCount; i++)
            {
                GameObject enemy = Instantiate(meleeEnemyPrefab, transform);
                enemy.name = $"MeleeEnemy_{i:00}";
                enemy.SetActive(false);
                meleeEnemyPool.Add(enemy);

                if (showDebugInfo)
                    Debug.Log($"근거리 적 풀 생성: {enemy.name}");
            }
        else
            Debug.LogError("근거리 적 프리팹이 할당되지 않았습니다!");

        // 원거리 적 풀 생성
        if (rangedEnemyPrefab != null)
            for (int i = 0; i < maxRangedEnemyCount; i++)
            {
                GameObject enemy = Instantiate(rangedEnemyPrefab, transform);
                enemy.name = $"RangedEnemy_{i:00}";
                enemy.SetActive(false);
                rangedEnemyPool.Add(enemy);

                if (showDebugInfo)
                    Debug.Log($"원거리 적 풀 생성: {enemy.name}");
            }
        else
            Debug.LogError("원거리 적 프리팹이 할당되지 않았습니다!");

        if (showDebugInfo)
            Debug.Log($"적 오브젝트 풀 생성 완료: 근거리 {maxMeleeEnemyCount}개, 원거리 {maxRangedEnemyCount}개");
    }

    /// <summary>
    /// 풀에서 비활성화된 근거리 적 가져오기
    /// </summary>
    public GameObject GetMeleeEnemyFromPool()
    {
        foreach (GameObject enemy in meleeEnemyPool)
            if (!enemy.activeSelf)
                return enemy;

        if (showDebugInfo)
            Debug.LogWarning("사용 가능한 근거리 적이 풀에 없습니다!");

        return null;
    }

    /// <summary>
    /// 풀에서 비활성화된 원거리 적 가져오기
    /// </summary>
    public GameObject GetRangedEnemyFromPool()
    {
        foreach (GameObject enemy in rangedEnemyPool)
            if (!enemy.activeSelf)
                return enemy;

        if (showDebugInfo)
            Debug.LogWarning("사용 가능한 원거리 적이 풀에 없습니다!");

        return null;
    }

    /// <summary>
    /// 적 스폰 (확률에 따라 근거리/원거리 적 선택)
    /// </summary>
    private void SpawnEnemy()
    {
        // 최대 활성 적 수 제한
        if (currentActiveEnemies >= maxActiveEnemies)
        {
            if (showDebugInfo)
                Debug.Log($"최대 활성 적 수({maxActiveEnemies}) 도달. 스폰 건너뜀.");
            return;
        }

        // 확률에 따라 적 타입 결정
        EnemyType enemyTypeToSpawn = DetermineEnemyType();

        GameObject enemy = null;
        string enemyTypeName = "";
        float spawnRadius = 0f;

        switch (enemyTypeToSpawn)
        {
            case EnemyType.Melee:
                enemy = GetMeleeEnemyFromPool();
                enemyTypeName = "근거리";
                spawnRadius = meleeSpawnRadius;
                break;
            case EnemyType.Ranged:
                enemy = GetRangedEnemyFromPool();
                enemyTypeName = "원거리";
                spawnRadius = rangedSpawnRadius;
                break;
        }

        if (enemy == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"풀에 사용 가능한 {enemyTypeName} 적이 없어 스폰할 수 없습니다.");
            return;
        }

        // 스폰 위치 계산 (스포너 기준으로 변경)
        Vector3 spawnPos = GetRandomPositionFromSpawner(spawnRadius, minDistanceFromPlayer);

        // 적 배치 및 활성화
        enemy.transform.position = spawnPos;
        enemy.transform.rotation = Quaternion.identity;

        // 적 타입에 따른 초기화
        InitializeEnemy(enemy, enemyTypeToSpawn);

        enemy.SetActive(true);
        currentActiveEnemies++;

        if (showDebugInfo)
            Debug.Log(
                $"{enemyTypeName} 적 스폰됨: {enemy.name} at {spawnPos} (활성 적: {currentActiveEnemies}/{maxActiveEnemies})");
    }

    /// <summary>
    /// 확률에 따라 스폰할 적 타입 결정
    /// </summary>
    private EnemyType DetermineEnemyType()
    {
        // 확률 정규화 (합이 1이 되도록)
        float totalChance = meleeEnemyChance + rangedEnemyChance;
        if (totalChance <= 0f)
        {
            Debug.LogWarning("모든 적 스폰 확률이 0입니다. 근거리 적을 기본값으로 사용합니다.");
            return EnemyType.Melee;
        }

        float normalizedMeleeChance = meleeEnemyChance / totalChance;
        float random = Random.Range(0f, 1f);

        if (random <= normalizedMeleeChance)
            return EnemyType.Melee;
        else
            return EnemyType.Ranged;
    }

    /// <summary>
    /// 적 타입에 따른 초기화
    /// </summary>
    private void InitializeEnemy(GameObject enemy, EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Melee:
                EnemyController meleeController = enemy.GetComponent<EnemyController>();
                if (meleeController != null)
                {
                    meleeController.hitPoint = meleeController.maxHitPoint; // 체력 리셋
                    meleeController.isDie = false;
                    meleeController.state = EnemyState.TRACE;
                }

                break;

            case EnemyType.Ranged:
                EnemyRangedController rangedController = enemy.GetComponent<EnemyRangedController>();
                if (rangedController != null)
                {
                    rangedController.hitPoint = rangedController.maxHitPoint; // 체력 리셋
                    rangedController.isDie = false;
                    rangedController.state = EnemyRangedState.IDLE;
                }

                break;
        }
    }

    /// <summary>
    /// 적이 죽었을 때 풀로 반환 (EnemyController 또는 EnemyRangedController에서 호출)
    /// </summary>
    public void ReturnEnemyToPool(GameObject enemy)
    {
        if (enemy.activeSelf)
        {
            enemy.SetActive(false);
            currentActiveEnemies = Mathf.Max(0, currentActiveEnemies - 1);

            string enemyType = "";
            if (enemy.GetComponent<EnemyController>() != null)
                enemyType = "근거리";
            else if (enemy.GetComponent<EnemyRangedController>() != null)
                enemyType = "원거리";

            if (showDebugInfo)
                Debug.Log($"{enemyType} 적 풀로 반환: {enemy.name} (활성 적: {currentActiveEnemies}/{maxActiveEnemies})");
        }
    }

    /// <summary>
    /// 스포너로부터 일정 거리 떨어진 랜덤 위치 생성 (플레이어 최소 거리 고려)
    /// </summary>
    private Vector3 GetRandomPositionFromSpawner(float radius, float minDistanceFromPlayer)
    {
        Vector3 randomPos;
        int attempts = 0;

        do
        {
            // 스포너 중심으로 랜덤 위치 생성
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(0f, radius);
            float x = transform.position.x + Mathf.Cos(angle) * distance;
            float z = transform.position.z + Mathf.Sin(angle) * distance;
            randomPos = new Vector3(x, transform.position.y, z);

            attempts++;
            if (attempts > 30) break; // 무한 루프 방지
        } while (player != null && Vector3.Distance(randomPos, player.position) < minDistanceFromPlayer);

        return randomPos;
    }

    /// <summary>
    /// 현재 활성화된 적 수 반환
    /// </summary>
    public int GetActiveEnemyCount()
    {
        return currentActiveEnemies;
    }

    /// <summary>
    /// 모든 적을 풀로 반환 (게임 리셋 등에 사용)
    /// </summary>
    public void ReturnAllEnemiesToPool()
    {
        // 근거리 적 풀 반환
        foreach (GameObject enemy in meleeEnemyPool)
            if (enemy.activeSelf)
                enemy.SetActive(false);

        // 원거리 적 풀 반환
        foreach (GameObject enemy in rangedEnemyPool)
            if (enemy.activeSelf)
                enemy.SetActive(false);

        currentActiveEnemies = 0;

        if (showDebugInfo)
            Debug.Log("모든 적이 풀로 반환됨");
    }

    /// <summary>
    /// 현재 스폰 확률 설정 가져오기 (디버그용)
    /// </summary>
    public (float melee, float ranged) GetSpawnChances()
    {
        float total = meleeEnemyChance + rangedEnemyChance;
        if (total <= 0f) return (1f, 0f);

        return (meleeEnemyChance / total, rangedEnemyChance / total);
    }

    #region 기즈모 그리기

    private void OnDrawGizmos()
    {
        if (showGizmoAlways)
            DrawGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmoAlways)
            DrawGizmo();
    }

    private void DrawGizmo()
    {
        Vector3 spawnerCenter = transform.position;
        Vector3 playerCenter = player != null ? player.position : transform.position;

        // 근거리 적 스폰 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnerCenter, meleeSpawnRadius);

        // 원거리 적 스폰 범위 (파란색)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnerCenter, rangedSpawnRadius);

        // 플레이어 최소 거리 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerCenter, minDistanceFromPlayer);

        // 현재 활성 적 정보 표시
        if (Application.isPlaying) Gizmos.color = Color.white;
        // 유니티 에디터에서만 텍스트 표시됨
    }

    #endregion

    #region 디버그 메서드

    /// <summary>
    /// 현재 풀 상태 출력 (개발/테스트용)
    /// </summary>
    [ContextMenu("Show Pool Status")]
    public void ShowPoolStatus()
    {
        (float meleeChance, float rangedChance) = GetSpawnChances();

        Debug.Log("=== Enemy Spawner 상태 ===");
        Debug.Log($"스폰 확률 - 근거리: {meleeChance:P0}, 원거리: {rangedChance:P0}");
        Debug.Log($"스폰 반경 - 근거리: {meleeSpawnRadius}m, 원거리: {rangedSpawnRadius}m");
        Debug.Log($"활성 적: {currentActiveEnemies}/{maxActiveEnemies}");

        int activeMelee = 0, activeRanged = 0;
        foreach (GameObject enemy in meleeEnemyPool)
            if (enemy.activeSelf)
                activeMelee++;
        foreach (GameObject enemy in rangedEnemyPool)
            if (enemy.activeSelf)
                activeRanged++;

        Debug.Log($"근거리 적: {activeMelee}/{maxMeleeEnemyCount} 활성");
        Debug.Log($"원거리 적: {activeRanged}/{maxRangedEnemyCount} 활성");
    }

    /// <summary>
    /// 특정 타입 적 강제 스폰 (테스트용)
    /// </summary>
    [ContextMenu("Spawn Melee Enemy")]
    public void ForceSpawnMeleeEnemy()
    {
        if (currentActiveEnemies >= maxActiveEnemies) return;

        GameObject enemy = GetMeleeEnemyFromPool();
        if (enemy != null)
        {
            Vector3 spawnPos = GetRandomPositionFromSpawner(meleeSpawnRadius, minDistanceFromPlayer);
            enemy.transform.position = spawnPos;
            InitializeEnemy(enemy, EnemyType.Melee);
            enemy.SetActive(true);
            currentActiveEnemies++;
            Debug.Log($"근거리 적 강제 스폰: {enemy.name}");
        }
    }

    /// <summary>
    /// 특정 타입 적 강제 스폰 (테스트용)
    /// </summary>
    [ContextMenu("Spawn Ranged Enemy")]
    public void ForceSpawnRangedEnemy()
    {
        if (currentActiveEnemies >= maxActiveEnemies) return;

        GameObject enemy = GetRangedEnemyFromPool();
        if (enemy != null)
        {
            Vector3 spawnPos = GetRandomPositionFromSpawner(rangedSpawnRadius, minDistanceFromPlayer);
            enemy.transform.position = spawnPos;
            InitializeEnemy(enemy, EnemyType.Ranged);
            enemy.SetActive(true);
            currentActiveEnemies++;
            Debug.Log($"원거리 적 강제 스폰: {enemy.name}");
        }
    }

    #endregion
}

/// <summary>
/// 적 타입 열거형
/// </summary>
public enum EnemyType
{
    Melee, // 근거리 적
    Ranged // 원거리 적
}
