using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float minDistanceFromPlayer = 3f;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Transform player;
    [SerializeField] private bool showGizmoAlways = true;

    [SerializeField] private List<GameObject> enemyPool = new();
    [SerializeField] private int maxEnemyCount = 40;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Start()
    {
        CreateEnemyPool();
        InvokeRepeating(nameof(SpawnEnemy), 2f, spawnInterval);
    }

    private void CreateEnemyPool()
    {
        for (int i = 0; i < maxEnemyCount; i++)
        {
            // 몬스터 생성
            GameObject monster = Instantiate<GameObject>(enemyPrefab);
            monster.name = $"Monster_{i:00}";
            monster.SetActive(false);
            enemyPool.Add(monster);
        }
    }

    public GameObject GetEnemyInPool()
    {
        foreach (GameObject enemy in enemyPool)
            if (enemy.activeSelf == false)
                return enemy;

        return null;
    }

    private void SpawnEnemy()
    {
        Vector3 spawnPos = GetRandomPosition(spawnRadius, minDistanceFromPlayer, player.position);
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    private Vector3 GetRandomPosition(float radius, float minDistanceFromPlayer, Vector3 playerPosition)
    {
        Vector3 randomPos;
        int attempts = 0;
        do
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minDistanceFromPlayer, radius);
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;
            randomPos = new Vector3(x, 0f, z);
            attempts++;
            if (attempts > 10) break;
        } while (Vector3.Distance(randomPos, playerPosition) < minDistanceFromPlayer);

        return randomPos;
    }

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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
