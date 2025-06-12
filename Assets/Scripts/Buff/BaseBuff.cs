using UnityEngine;

public abstract class BaseBuff : IBuff
{
    public BuffType BuffType { get; protected set; }
    public float Duration { get; protected set; }
    public bool IsActive { get; protected set; }

    protected float remainingTime;
    protected BuffData data;
    protected GameObject target;

    public BaseBuff(BuffData buffData, GameObject targetObject)
    {
        data = buffData;
        target = targetObject;
        BuffType = buffData.buffType;
        Duration = buffData.duration;
        remainingTime = Duration;
        IsActive = true;
    }

    public virtual void Apply(GameObject target)
    {
        OnApply();
    }

    public virtual void Remove(GameObject target)
    {
        OnRemove();
        IsActive = false;
    }

    public virtual void Update(float deltaTime)
    {
        if (!IsActive) return;

        if (Duration > 0) // 지속시간이 있는 버프
        {
            remainingTime -= deltaTime;
            if (remainingTime <= 0) Remove(target);
        }

        OnUpdate(deltaTime);
    }

    protected abstract void OnApply();
    protected abstract void OnRemove();

    protected virtual void OnUpdate(float deltaTime)
    {
    }
}
