using UnityEngine;

[CreateAssetMenu(fileName = "New Buff Data", menuName = "Game/Buff Data")]
public class BuffData : ScriptableObject
{
    [Header("기본 정보")] public BuffType buffType;
    public string buffName;
    public string description;
    public Color particleColor = Color.white;

    [Header("효과")] public float duration = 10f; // 지속시간 (즉시 효과면 0)
    public float value = 1.5f; // 효과 수치 (배율, 회복량 등)

    [Header("아이템 외형")] public GameObject itemPrefab;
}
