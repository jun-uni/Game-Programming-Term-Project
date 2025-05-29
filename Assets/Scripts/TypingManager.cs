using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TypingManager : MonoBehaviour
{
    public static TypingManager Instance;

    [Header("현재 입력 상태")] public string currentInput = "";

    [Header("활성 타겟들")] private List<WordTarget> activeTargets = new();
    private WordTarget currentTarget = null;

    [Header("입력 설정")] public bool allowBackspace = true;

    [Header("디버그")] public bool showDebugInfo = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        HandleInput();

        // 디버그 정보 표시
        if (showDebugInfo) Debug.Log($"Current Input: '{currentInput}' | Active Targets: {activeTargets.Count}");
    }

    private void HandleInput()
    {
        // 키보드 입력 감지
        foreach (char c in Input.inputString)
            if (c == '\b') // 백스페이스
            {
                if (allowBackspace && currentInput.Length > 0)
                {
                    currentInput = currentInput.Substring(0, currentInput.Length - 1);
                    ClearCurrentTarget();
                    UpdateTargetMatching();
                }
            }
            else if (char.IsLetter(c)) // 영문자만 허용
            {
                currentInput += char.ToLower(c);
                UpdateTargetMatching();
            }
    }

    private void UpdateTargetMatching()
    {
        if (string.IsNullOrEmpty(currentInput))
        {
            // 입력이 없으면 모든 타겟 초기화 (오타 상태는 유지)
            ClearCurrentTarget();
            return;
        }

        // 각 타겟별로 개별 매칭 확인
        WordTarget bestTarget = null;
        int bestMatchLength = 0;
        int bestStartPos = -1;

        foreach (WordTarget target in activeTargets)
        {
            int matchLength;
            int startPos;

            // 각 타겟의 개별 매칭 확인 (오타 체크 포함)
            bool hasMatch = target.CheckMatching(currentInput, out matchLength, out startPos);

            if (hasMatch && matchLength > 0)
            {
                // 완전 매칭된 단어가 있으면 즉시 완성 처리
                if (matchLength == target.Word.Length)
                {
                    CompleteWord(target, startPos, target.Word.Length);
                    return;
                }

                // 가장 긴 매칭을 찾기 (같은 길이면 더 가까운 적 우선)
                if (matchLength > bestMatchLength ||
                    (matchLength == bestMatchLength && bestTarget != null &&
                     Vector3.Distance(Camera.main.transform.position, target.transform.position) <
                     Vector3.Distance(Camera.main.transform.position, bestTarget.transform.position)))
                {
                    bestTarget = target;
                    bestMatchLength = matchLength;
                    bestStartPos = startPos;
                }
            }
        }

        // 가장 좋은 매칭 설정
        if (bestTarget != null && bestMatchLength > 0)
        {
            if (currentTarget != bestTarget)
            {
                ClearCurrentTarget();
                SetCurrentTarget(bestTarget);
            }

            bestTarget.SetTypingProgress(bestMatchLength);
        }
        else
        {
            ClearCurrentTarget();
        }
    }

    private void SetCurrentTarget(WordTarget target)
    {
        currentTarget = target;
    }

    private void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.SetTypingProgress(0);
            currentTarget = null;
        }
    }

    private void ClearAllTargets()
    {
        foreach (WordTarget target in activeTargets) target.ResetTypingProgress();
        currentTarget = null;
    }

    private void ResetAllTargetsProgress()
    {
        foreach (WordTarget target in activeTargets) target.ResetTypingProgress();
        currentTarget = null;
    }

    private void CompleteWord(WordTarget target, int startPos, int wordLength)
    {
        Debug.Log($"단어 완성: {target.Word} (위치: {startPos})");

        // 타겟 처리 (투사체 발사 등은 나중에 구현)
        target.OnWordCompleted();

        // 완성된 단어 부분만 입력에서 제거
        string before = currentInput.Substring(0, startPos);
        string after = currentInput.Substring(startPos + wordLength);
        currentInput = before + after;

        Debug.Log($"남은 입력: '{currentInput}'");

        // 타겟 초기화
        ClearCurrentTarget();

        // 남은 입력이 있으면 다시 매칭 시도
        if (!string.IsNullOrEmpty(currentInput)) UpdateTargetMatching();
    }

    public void RegisterTarget(WordTarget target)
    {
        if (!activeTargets.Contains(target)) activeTargets.Add(target);
    }

    public void UnregisterTarget(WordTarget target)
    {
        if (activeTargets.Contains(target))
        {
            activeTargets.Remove(target);

            if (currentTarget == target) currentTarget = null;
        }
    }
}
