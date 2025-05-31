using UnityEngine;

public class WordTarget : MonoBehaviour
{
    [Header("단어 설정")] public string Word { get; private set; }

    [Header("컴포넌트")] private WordDisplay wordDisplay;
    private EnemyController enemyController;

    [Header("상태")] public bool IsCompleted { get; private set; } = false;

    [Header("타이핑 진행 상태")] private int currentProgress = 0; // 현재 몇 글자까지 입력되었는지

    private void Start()
    {
        // 컴포넌트 참조
        wordDisplay = GetComponentInChildren<WordDisplay>();
        enemyController = GetComponent<EnemyController>();

        // 랜덤 단어 할당
        AssignRandomWord();

        // 타이핑 매니저에 등록
        if (TypingManager.Instance != null) TypingManager.Instance.RegisterTarget(this);
    }

    private void OnEnable()
    {
        if (TypingManager.Instance != null && !string.IsNullOrEmpty(Word))
        {
            ResetTypingProgress();
            TypingManager.Instance.RegisterTarget(this);
        }
    }

    private void OnDisable()
    {
        if (TypingManager.Instance != null) TypingManager.Instance.UnregisterTarget(this);
    }

    private void AssignRandomWord()
    {
        if (WordDatabase.Instance != null)
        {
            Word = WordDatabase.Instance.GetRandomWord();
            if (wordDisplay != null) wordDisplay.SetWord(Word);
            ResetTypingProgress();
        }
    }

    /// <summary>
    /// 현재 위치에서 다음 글자로 입력된 문자를 받을 수 있는지 확인
    /// </summary>
    public bool CanAcceptNextChar(char inputChar)
    {
        if (string.IsNullOrEmpty(Word) || IsCompleted || currentProgress >= Word.Length) return false;

        char expectedChar = char.ToLower(Word[currentProgress]);
        char lowerInputChar = char.ToLower(inputChar);

        return expectedChar == lowerInputChar;
    }

    /// <summary>
    /// 글자를 수용하고 진행도 증가
    /// </summary>
    public void AcceptCharacter(char inputChar)
    {
        if (CanAcceptNextChar(inputChar))
        {
            currentProgress++;
            UpdateDisplay();
        }
    }

    /// <summary>
    /// 개별 오타 발생 시 처리
    /// </summary>
    public void TriggerIndividualTypo()
    {
        if (TypingManager.Instance.showDebugInfo)
            Debug.Log($"개별 오타: {Word} 리셋됨");

        ResetTypingProgress();
    }

    /// <summary>
    /// 백스페이스 처리
    /// </summary>
    public void HandleBackspace()
    {
        if (currentProgress > 0)
        {
            currentProgress--;
            UpdateDisplay();
        }
    }

    /// <summary>
    /// 현재 진행도 반환
    /// </summary>
    public int GetCurrentProgress()
    {
        return currentProgress;
    }

    /// <summary>
    /// 단어가 완성되었는지 확인
    /// </summary>
    public bool IsWordCompleted()
    {
        return !IsCompleted && currentProgress >= Word.Length;
    }

    /// <summary>
    /// 현재 타겟이 첫 글자를 기다리고 있는지 확인
    /// </summary>
    public bool IsWaitingForFirstChar()
    {
        return currentProgress == 0;
    }

    public void SetTypingProgress(int typedCharacters)
    {
        currentProgress = Mathf.Clamp(typedCharacters, 0, Word?.Length ?? 0);
        UpdateDisplay();
    }

    public void ResetTypingProgress()
    {
        currentProgress = 0;
        UpdateDisplay();

        if (wordDisplay != null) wordDisplay.ResetToDefaultColor();
    }

    private void UpdateDisplay()
    {
        if (wordDisplay != null) wordDisplay.UpdateTypingProgress(currentProgress);
    }

    public void OnWordCompleted()
    {
        IsCompleted = true;

        // 시각적 효과
        if (wordDisplay != null) wordDisplay.ShowCompletionEffect();

        Debug.Log($"단어 완성됨: {Word}");

        // 단어 완성 이벤트 발생
        WordCompletionEvents.TriggerWordCompleted(transform);

        // 타이핑 매니저에서 제거
        if (TypingManager.Instance != null) TypingManager.Instance.UnregisterTarget(this);

        // 완성 효과 후 새로운 단어 할당 (0.5초 후)
        StartCoroutine(AssignNewWordAfterDelay(0.5f));
    }

    private System.Collections.IEnumerator AssignNewWordAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 적이 아직 살아있고 게임오브젝트가 활성화되어 있으면 새 단어 할당
        if (gameObject.activeInHierarchy && (enemyController == null || !enemyController.isDie))
        {
            IsCompleted = false;
            AssignRandomWord();

            // 타이핑 매니저에 다시 등록
            if (TypingManager.Instance != null) TypingManager.Instance.RegisterTarget(this);
        }
    }
}
