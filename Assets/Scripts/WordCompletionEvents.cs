using UnityEngine;
using System;

/// <summary>
/// 단어 완성과 관련된 이벤트들을 관리하는 정적 클래스
/// </summary>
public static class WordCompletionEvents
{
    /// <summary>
    /// 단어 완성 시 발생하는 이벤트
    /// Transform: 완성된 단어의 타겟 Transform
    /// </summary>
    public static event Action<Transform> OnWordCompleted;

    /// <summary>
    /// 단어 완성 이벤트 발생
    /// </summary>
    /// <param name="target">완성된 단어의 타겟</param>
    public static void TriggerWordCompleted(Transform target)
    {
        OnWordCompleted?.Invoke(target);
    }

    /// <summary>
    /// 모든 이벤트 구독 해제 (씬 전환 시 사용)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnWordCompleted = null;
    }
}
