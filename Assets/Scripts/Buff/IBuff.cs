using UnityEngine;

public interface IBuff
{
    BuffType BuffType { get; }
    float Duration { get; }
    bool IsActive { get; }
    void Apply(GameObject target);
    void Remove(GameObject target);
    void Update(float deltaTime);
}
