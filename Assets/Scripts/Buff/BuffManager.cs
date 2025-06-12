using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    private List<IBuff> activeBuffs = new();
    private Dictionary<BuffType, IBuff> buffsByType = new();

    [Header("디버그")] [SerializeField] private bool showDebugInfo = true;

    public void ApplyBuff(BuffData buffData, GameObject target)
    {
        // 같은 타입의 버프가 이미 있으면 제거 (또는 시간 연장)
        if (buffsByType.ContainsKey(buffData.buffType)) RemoveBuff(buffData.buffType);

        IBuff newBuff = CreateBuff(buffData, target);
        if (newBuff != null)
        {
            activeBuffs.Add(newBuff);
            buffsByType[buffData.buffType] = newBuff;
            newBuff.Apply(target);

            if (showDebugInfo)
                Debug.Log($"버프 적용: {buffData.buffName} ({buffData.buffType})");
        }
    }

    private IBuff CreateBuff(BuffData data, GameObject target)
    {
        switch (data.buffType)
        {
            case BuffType.SpeedUp:
                return new SpeedUpBuff(data, target);
            case BuffType.AttackPowerUp:
                return new AttackPowerUpBuff(data, target);
            case BuffType.HealthRestore:
                return new HealthRestoreBuff(data, target);
            case BuffType.SlowEnemies:
                return new SlowEnemiesBuff(data, target);
            case BuffType.SlowEnemySpawn:
                return new SlowEnemySpawnBuff(data, target);
            default:
                Debug.LogWarning($"구현되지 않은 버프 타입: {data.buffType}");
                return null;
        }
    }

    public void RemoveBuff(BuffType type)
    {
        if (buffsByType.ContainsKey(type))
        {
            IBuff buff = buffsByType[type];
            buff.Remove(gameObject);
            activeBuffs.Remove(buff);
            buffsByType.Remove(type);

            if (showDebugInfo)
                Debug.Log($"버프 제거: {type}");
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGameActive()) return;

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            IBuff buff = activeBuffs[i];
            buff.Update(Time.deltaTime);

            if (!buff.IsActive)
            {
                activeBuffs.RemoveAt(i);
                buffsByType.Remove(buff.BuffType);

                if (showDebugInfo)
                    Debug.Log($"버프 자동 제거: {buff.BuffType}");
            }
        }
    }

    /// <summary>
    /// 현재 활성 버프 리스트 반환
    /// </summary>
    public List<IBuff> GetActiveBuffs()
    {
        return new List<IBuff>(activeBuffs);
    }

    /// <summary>
    /// 특정 타입 버프가 활성화되어 있는지 확인
    /// </summary>
    public bool HasBuff(BuffType type)
    {
        return buffsByType.ContainsKey(type);
    }
}
