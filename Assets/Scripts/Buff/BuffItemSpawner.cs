using System.Collections.Generic;
using UnityEngine;

public class BuffItemSpawner : MonoBehaviour
{
    [Header("스폰 설정")] [SerializeField] private BuffData[] availableBuffs;
    [SerializeField] private GameObject defaultBuffItemPrefab; // 기본 버프 아이템 프리팹 (BuffData에 itemPrefab이 없을 때 사용)
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private float spawnRadius = 13.5f;
    [SerializeField] private float spawnHeight = 0.8f;

    [Header("스폰 제한")] [SerializeField] private int maxBuffItems = 3; // 맵에 동시 존재할 수 있는 최대 버프 아이템 수

    [Header("디버그")] [SerializeField] private bool showDebugInfo = true;

    private float nextSpawnTime;
    private List<GameObject> spawnedItems = new();

    private void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGameActive()) return;

        // 파괴된 아이템들을 리스트에서 제거
        spawnedItems.RemoveAll(item => item == null);

        if (Time.time >= nextSpawnTime && spawnedItems.Count < maxBuffItems)
        {
            SpawnRandomBuff();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnRandomBuff()
    {
        if (availableBuffs.Length == 0) return;

        // 랜덤 버프 선택
        BuffData selectedBuff = availableBuffs[Random.Range(0, availableBuffs.Length)];

        // 랜덤 위치 생성
        Vector3 spawnPosition = GetRandomSpawnPosition();

        // 아이템 생성 - BuffData에서 전용 프리팹 사용 또는 기본 프리팹 사용
        GameObject item = CreateBuffItem(selectedBuff, spawnPosition);

        if (item != null)
        {
            // 스폰된 아이템 리스트에 추가
            spawnedItems.Add(item);

            if (showDebugInfo)
                Debug.Log($"버프 아이템 스폰: {selectedBuff.buffName} at {spawnPosition}");
        }
    }

    /// <summary>
    /// 버프 데이터에 따라 적절한 아이템 생성
    /// </summary>
    private GameObject CreateBuffItem(BuffData buffData, Vector3 position)
    {
        GameObject itemPrefab = null;

        // 1. BuffData에 전용 프리팹이 있으면 사용
        if (buffData.itemPrefab != null)
        {
            itemPrefab = buffData.itemPrefab;
            if (showDebugInfo)
                Debug.Log($"전용 프리팹 사용: {buffData.buffName} - {itemPrefab.name}");
        }
        // 2. 없으면 기본 프리팹 사용
        else if (defaultBuffItemPrefab != null)
        {
            itemPrefab = defaultBuffItemPrefab;
            if (showDebugInfo)
                Debug.Log($"기본 프리팹 사용: {buffData.buffName}");
        }
        else
        {
            Debug.LogError($"버프 아이템 프리팹이 없습니다! BuffData: {buffData.buffName}");
            return null;
        }

        // 아이템 생성
        GameObject item = Instantiate(itemPrefab, position, Quaternion.identity);

        // BuffItem 컴포넌트 확인 및 데이터 설정
        BuffItem buffItem = item.GetComponent<BuffItem>();
        if (buffItem == null)
        {
            // BuffItem 컴포넌트가 없으면 추가
            buffItem = item.AddComponent<BuffItem>();
            if (showDebugInfo)
                Debug.Log($"BuffItem 컴포넌트 자동 추가: {item.name}");
        }

        // 버프 데이터 설정
        buffItem.SetBuffData(buffData);

        return item;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float distance = Random.Range(2f, spawnRadius);

        Vector3 randomPosition = new(
            Mathf.Cos(angle) * distance,
            spawnHeight, // 바닥 위 고정 높이 (필요에 따라 조정)
            Mathf.Sin(angle) * distance
        );

        return randomPosition;
    }

    private void OnDrawGizmos()
    {
        // 스폰 범위 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
