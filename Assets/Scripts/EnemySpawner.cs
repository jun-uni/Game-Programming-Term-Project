using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 설정")] [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform player;
    [SerializeField] private bool showGizmoAlways = true;

    [Header("오브젝트 풀 설정")] [SerializeField] private List<GameObject> enemyPool = new();
    [SerializeField] private int maxEnemyCount = 40;
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
        CreateEnemyPool();
        InvokeRepeating(nameof(SpawnEnemy), 2f, spawnInterval);
    }

    /// <summary>
    /// 적 오브젝트 풀 생성
    /// </summary>
    private void CreateEnemyPool()
    {
        for (int i = 0; i < maxEnemyCount; i++)
        {
            // 몬스터 생성
            GameObject enemy = Instantiate(enemyPrefab, transform);
            enemy.name = $"Enemy_{i:00}";
            enemy.SetActive(false);
            enemyPool.Add(enemy);

            if (showDebugInfo)
                Debug.Log($"적 풀 생성: {enemy.name}");
        }

        if (showDebugInfo)
            Debug.Log($"적 오브젝트 풀 생성 완료: {maxEnemyCount}개");
    }

    /// <summary>
    /// 풀에서 비활성화된 적 가져오기
    /// </summary>
    public GameObject GetEnemyFromPool()
    {
        foreach (GameObject enemy in enemyPool)
            if (!enemy.activeSelf)
                return enemy;

        if (showDebugInfo)
            Debug.LogWarning("사용 가능한 적이 풀에 없습니다!");

        return null;
    }

    /// <summary>
    /// 적 스폰 (오브젝트 풀링 사용)
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

        // 풀에서 비활성화된 적 가져오기
        GameObject enemy = GetEnemyFromPool();
        if (enemy == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("풀에 사용 가능한 적이 없어 스폰할 수 없습니다.");
            return;
        }

        // 스폰 위치 계산
        Vector3 spawnPos = GetRandomPosition(spawnRadius, minDistanceFromPlayer, player.position);

        // 적 배치 및 활성화
        enemy.transform.position = spawnPos;
        enemy.transform.rotation = Quaternion.identity;

        // EnemyController 초기화 (체력 등 리셋)
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.hitPoint = 100; // 체력 리셋
            enemyController.isDie = false;
            enemyController.state = EnemyState.TRACE;
        }

        enemy.SetActive(true);
        currentActiveEnemies++;

        if (showDebugInfo)
            Debug.Log($"적 스폰됨: {enemy.name} at {spawnPos} (활성 적: {currentActiveEnemies}/{maxActiveEnemies})");
    }

    /// <summary>
    /// 적이 죽었을 때 풀로 반환 (EnemyController에서 호출)
    /// </summary>
    public void ReturnEnemyToPool(GameObject enemy)
    {
        if (enemy.activeSelf)
        {
            enemy.SetActive(false);
            currentActiveEnemies = Mathf.Max(0, currentActiveEnemies - 1);

            if (showDebugInfo)
                Debug.Log($"적 풀로 반환: {enemy.name} (활성 적: {currentActiveEnemies}/{maxActiveEnemies})");
        }
    }

    /// <summary>
    /// 플레이어로부터 일정 거리 떨어진 랜덤 위치 생성
    /// </summary>
    private Vector3 GetRandomPosition(float radius, float minDistanceFromPlayer, Vector3 playerPosition)
    {
        Vector3 randomPos;
        int attempts = 0;

        do
        {
            // 플레이어 중심으로 랜덤 위치 생성
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minDistanceFromPlayer, radius);
            float x = playerPosition.x + Mathf.Cos(angle) * distance;
            float z = playerPosition.z + Mathf.Sin(angle) * distance;
            randomPos = new Vector3(x, playerPosition.y, z);

            attempts++;
            if (attempts > 20) break; // 무한 루프 방지
        } while (Vector3.Distance(randomPos, playerPosition) < minDistanceFromPlayer);

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
        foreach (GameObject enemy in enemyPool)
            if (enemy.activeSelf)
                enemy.SetActive(false);

        currentActiveEnemies = 0;

        if (showDebugInfo)
            Debug.Log("모든 적이 풀로 반환됨");
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
        Vector3 center = player != null ? player.position : transform.position;

        // 스폰 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, spawnRadius);

        // 플레이어 최소 거리 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, minDistanceFromPlayer);

        // 현재 활성 적 정보 표시
        if (Application.isPlaying) Gizmos.color = Color.white;
        // 유니티 에디터에서만 텍스트 표시됨
    }

    #endregion
}
