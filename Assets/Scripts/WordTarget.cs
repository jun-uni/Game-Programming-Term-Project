using UnityEngine;

public class WordTarget : MonoBehaviour
{
    [Header("단어 설정")] public string Word { get; private set; }

    [Header("컴포넌트")] private WordDisplay wordDisplay;
    private EnemyController enemyController;

    [Header("상태")] public bool IsCompleted { get; private set; } = false;
    public bool IsTypoError { get; private set; } = false; // 오타 상태

    [Header("타이핑 진행 상태")] public int LastMatchedLength { get; private set; } = 0;
    public string LastMatchedInput { get; private set; } = "";

    private void Start()
    {
        // 컴포넌트 참조
        wordDisplay = GetComponentInChildren<WordDisplay>();
        enemyController = GetComponent<EnemyController>();

        // 랜덤 단어 할당
        AssignRandomWord();

        // 타이핑 매니저에 등록
        TypingManager.Instance.RegisterTarget(this);
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
        Word = WordDatabase.Instance.GetRandomWord();
        if (wordDisplay != null) wordDisplay.SetWord(Word);
        ResetTypingProgress();
    }

    public void SetTypingProgress(int typedCharacters)
    {
        LastMatchedLength = typedCharacters;
        if (wordDisplay != null) wordDisplay.UpdateTypingProgress(typedCharacters);
    }

    public void ResetTypingProgress()
    {
        LastMatchedLength = 0;
        LastMatchedInput = "";
        if (wordDisplay != null)
        {
            wordDisplay.UpdateTypingProgress(0);
            wordDisplay.ResetToDefaultColor();
        }
    }


    // 현재 입력이 이 단어와 매칭되는지 확인 (오타 체크 포함)
    public bool CheckMatching(string currentInput, out int matchLength, out int startPosition)
    {
        matchLength = 0;
        startPosition = -1;

        string targetWord = Word.ToLower();

        // 입력 문자열의 모든 위치에서 이 단어 매칭 시도
        for (int startPos = 0; startPos <= currentInput.Length - 1; startPos++)
        {
            int currentMatchLength = 0;
            bool hasTypoInThisPosition = false;

            // 해당 위치에서 단어 매칭 확인
            for (int i = 0; i < targetWord.Length && startPos + i < currentInput.Length; i++)
                if (currentInput[startPos + i] == targetWord[i])
                {
                    currentMatchLength++;
                }
                else
                {
                    // 이미 매칭이 시작된 상태에서 틀렸으면 오타
                    if (currentMatchLength > 0)
                    {
                        hasTypoInThisPosition = true;
                        ShowTypoEffect();
                    }

                    break;
                }

            // 이 위치에서 오타가 났지만, 다른 위치에서는 매칭될 수 있으므로 계속 확인
            if (!hasTypoInThisPosition && currentMatchLength > 0)
                // 유효한 매칭이 있으면 저장
                if (currentMatchLength > matchLength)
                {
                    matchLength = currentMatchLength;
                    startPosition = startPos;
                }
        }

        return matchLength > 0;
    }

    public void ShowTypoEffect()
    {
        // 오타 효과 없이 바로 리셋만 수행
        LastMatchedLength = 0;
        LastMatchedInput = "";

        if (wordDisplay != null)
        {
            wordDisplay.UpdateTypingProgress(0);
            wordDisplay.ResetToDefaultColor();
        }

        Debug.Log($"오타 발생: {Word} - 진행상황 리셋됨");
    }

    public void OnWordCompleted()
    {
        IsCompleted = true;

        // 시각적 효과
        if (wordDisplay != null) wordDisplay.ShowCompletionEffect();

        // 적 처리는 외부에서 처리하도록 이벤트 발생
        Debug.Log($"단어 완성됨: {Word} - 외부에서 적 처리 필요");

        // 타이핑 매니저에서 제거
        TypingManager.Instance.UnregisterTarget(this);

        // 완성 효과 후 새로운 단어 할당 (1초 후)
        StartCoroutine(AssignNewWordAfterDelay(1f));
    }

    private System.Collections.IEnumerator AssignNewWordAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 적이 아직 살아있고 게임오브젝트가 활성화되어 있으면 새 단어 할당
        if (gameObject.activeInHierarchy && !enemyController.isDie)
        {
            IsCompleted = false;
            AssignRandomWord();

            // 타이핑 매니저에 다시 등록
            TypingManager.Instance.RegisterTarget(this);
        }
    }
}
