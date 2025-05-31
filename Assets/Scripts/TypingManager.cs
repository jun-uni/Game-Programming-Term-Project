using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TypingManager : MonoBehaviour
{
    public static TypingManager Instance;

    [Header("활성 타겟들")] private List<WordTarget> activeTargets = new();

    [Header("입력 설정")] public bool allowBackspace = true;

    [Header("전역 오타 설정")] public float typoEffectDuration = 0.5f;

    [Header("오타 효과")] public bool isGlobalTypo = false;
    private float typoTimer = 0f;

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
        HandleTypoEffect();
    }

    private void HandleInput()
    {
        // 게임이 활성 상태가 아니면 입력 무시
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            return;

        // 키보드 입력 감지
        foreach (char c in Input.inputString)
            if (c == '\b') // 백스페이스
            {
                if (allowBackspace) HandleBackspace();
            }
            else if (char.IsLetter(c)) // 영문자만 허용
            {
                ProcessSingleCharacter(char.ToLower(c));
            }
    }

    private void HandleBackspace()
    {
        // 모든 타겟에서 마지막 글자 제거
        foreach (WordTarget target in activeTargets) target.HandleBackspace();

        if (showDebugInfo)
            Debug.Log("백스페이스 처리됨");
    }

    private void ProcessSingleCharacter(char inputChar)
    {
        if (showDebugInfo)
            Debug.Log($"새 글자 입력: '{inputChar}'");

        // 모든 타겟의 이전 진행도 저장
        Dictionary<WordTarget, int> previousProgress = new();
        foreach (WordTarget target in activeTargets) previousProgress[target] = target.GetCurrentProgress();

        // 각 타겟이 이 글자를 처리할 수 있는지 확인
        Dictionary<WordTarget, int> individualTypoTargets = new();
        List<WordTarget> acceptingTargets = new();

        foreach (WordTarget target in activeTargets)
        {
            bool canAccept = target.CanAcceptNextChar(inputChar);

            if (canAccept)
            {
                // 이 타겟은 글자를 받을 수 있음
                target.AcceptCharacter(inputChar);
                acceptingTargets.Add(target);

                if (showDebugInfo)
                    Debug.Log(
                        $"{target.Word}: '{inputChar}' 수용, 진행도 {previousProgress[target]} → {target.GetCurrentProgress()}");
            }
            else
            {
                // 이 타겟은 글자를 받을 수 없음 - 개별 오타
                int prevProgress = previousProgress[target];
                if (prevProgress > 0) // 진행 중이었다면 개별 오타
                {
                    individualTypoTargets[target] = prevProgress;
                    target.TriggerIndividualTypo();

                    if (showDebugInfo)
                        Debug.Log($"{target.Word}: '{inputChar}' 개별 오타, 진행도 {prevProgress} → 0");
                }
            }
        }

        // 전역 오타 체크 (6번 규칙)
        if (CheckGlobalTypo(individualTypoTargets, acceptingTargets))
        {
            TriggerGlobalTypo();
            return;
        }

        // 완성된 단어 체크
        CheckCompletedWords();
    }

    private bool CheckGlobalTypo(Dictionary<WordTarget, int> individualTypoTargets, List<WordTarget> acceptingTargets)
    {
        // 개별 오타가 없으면 전역 오타 아님
        if (individualTypoTargets.Count == 0)
            return false;

        // 6번 규칙: 개별 오타가 발생한 타겟들의 이전 진행도 중 최대값
        int maxTypoProgress = individualTypoTargets.Values.Max();

        // 개별 오타가 발생하지 않은 타겟들 중에서 더 진행된 것이 있는지 확인
        foreach (WordTarget target in acceptingTargets)
            if (target.GetCurrentProgress() > maxTypoProgress)
            {
                if (showDebugInfo)
                    Debug.Log(
                        $"전역 오타 회피: {target.Word}가 더 진행됨 (진행도: {target.GetCurrentProgress()}, 오타 최대 진행도: {maxTypoProgress})");
                return false;
            }

        if (showDebugInfo)
            Debug.Log($"전역 오타 발생: 6번 규칙 적용 (개별 오타 최대 진행도: {maxTypoProgress})");
        return true; // 전역 오타
    }

    private void TriggerGlobalTypo()
    {
        Debug.LogError("전역 오타 발생!");

        isGlobalTypo = true;
        typoTimer = typoEffectDuration;

        // GameManager에 전역 오타 발생 알림
        if (GameManager.Instance != null) GameManager.Instance.AddGlobalTypo();

        // 개별 오타가 발생한 타겟들은 이미 TriggerIndividualTypo()로 리셋됨
        // 정상 진행 중인 타겟들은 그대로 유지

        // 여기에 전역 오타 시각/음향 효과 추가 가능
    }

    private void HandleTypoEffect()
    {
        if (isGlobalTypo)
        {
            typoTimer -= Time.deltaTime;
            if (typoTimer <= 0f)
            {
                isGlobalTypo = false;
                if (showDebugInfo)
                    Debug.Log("전역 오타 효과 종료");
            }
        }
    }

    private void CheckCompletedWords()
    {
        List<WordTarget> completedTargets = new();

        foreach (WordTarget target in activeTargets)
            if (target.IsWordCompleted())
                completedTargets.Add(target);

        foreach (WordTarget completedTarget in completedTargets) CompleteWord(completedTarget);
    }

    private void CompleteWord(WordTarget target)
    {
        if (showDebugInfo)
            Debug.Log($"단어 완성: {target.Word}");

        target.OnWordCompleted();
    }

    public void RegisterTarget(WordTarget target)
    {
        if (!activeTargets.Contains(target)) activeTargets.Add(target);
    }

    public void UnregisterTarget(WordTarget target)
    {
        if (activeTargets.Contains(target)) activeTargets.Remove(target);
    }

    public bool IsGlobalTypo()
    {
        return isGlobalTypo;
    }
}
