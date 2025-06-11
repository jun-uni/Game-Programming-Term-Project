using UnityEngine;

public class WordTarget : MonoBehaviour
{
    [Header("단어 설정")] public string Word;

    [Header("한국어 지원")] private string[] koreanJamoArray; // 한국어일 때 자모 배열
    private bool isKoreanWord = false;

    [Header("컴포넌트")] private WordDisplay wordDisplay;

    // IEnemy 인터페이스로 깔끔하게 통합!
    private IEnemy enemy;

    [Header("상태")] public bool IsCompleted = false;

    [Header("타이핑 진행 상태")] [SerializeField] private int currentProgress = 0; // 현재 몇 글자/자모까지 입력되었는지

    private void Start()
    {
        // 컴포넌트 참조
        wordDisplay = GetComponentInChildren<WordDisplay>();

        // IEnemy 인터페이스를 구현한 컴포넌트 찾기 (깔끔!)
        enemy = GetComponent<IEnemy>();

        if (enemy != null)
        {
            string enemyType = enemy.GetType().Name;
            Debug.Log($"{gameObject.name}: {enemyType}로 인식됨");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: IEnemy를 구현한 컴포넌트를 찾을 수 없음!");
        }

        // 랜덤 단어 할당
        AssignRandomWord();

        // 타이핑 매니저에 등록
        if (TypingManager.Instance != null)
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
        if (TypingManager.Instance != null)
            TypingManager.Instance.UnregisterTarget(this);
    }

    private void AssignRandomWord()
    {
        if (WordDatabase.Instance != null)
        {
            // 타이핑 매니저의 한국어 모드에 따라 단어 선택
            bool useKoreanMode = TypingManager.Instance != null && TypingManager.Instance.IsKoreanMode();

            if (useKoreanMode)
            {
                Word = WordDatabase.Instance.GetRandomKoreanWord();
                SetupKoreanWord();
            }
            else
            {
                Word = WordDatabase.Instance.GetRandomWord();
                SetupEnglishWord();
            }

            if (wordDisplay != null)
                wordDisplay.SetWord(Word);

            ResetTypingProgress();
        }
    }

    private void SetupKoreanWord()
    {
        isKoreanWord = true;
        koreanJamoArray = KoreanTool.SplitKoreanCharacters(Word);

        if (TypingManager.Instance != null && TypingManager.Instance.showDebugInfo)
            Debug.Log($"한국어 단어 설정: {Word} → 자모: [{string.Join(", ", koreanJamoArray)}]");
    }

    private void SetupEnglishWord()
    {
        isKoreanWord = false;
        koreanJamoArray = null;
    }

    /// <summary>
    /// 현재 위치에서 다음 글자로 입력된 문자를 받을 수 있는지 확인 (영어용)
    /// </summary>
    public bool CanAcceptNextChar(char inputChar)
    {
        if (isKoreanWord || string.IsNullOrEmpty(Word) || IsCompleted || currentProgress >= Word.Length)
            return false;

        char expectedChar = char.ToLower(Word[currentProgress]);
        char lowerInputChar = char.ToLower(inputChar);

        return expectedChar == lowerInputChar;
    }

    /// <summary>
    /// 현재 위치에서 다음 자모를 받을 수 있는지 확인 (한국어용)
    /// </summary>
    public bool CanAcceptNextJamo(string inputJamo)
    {
        if (!isKoreanWord || koreanJamoArray == null || IsCompleted || currentProgress >= koreanJamoArray.Length)
            return false;

        string expectedJamo = koreanJamoArray[currentProgress];
        return expectedJamo == inputJamo;
    }

    /// <summary>
    /// 글자를 수용하고 진행도 증가 (영어용)
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
    /// 자모를 수용하고 진행도 증가 (한국어용)
    /// </summary>
    public void AcceptJamo(string inputJamo)
    {
        if (CanAcceptNextJamo(inputJamo))
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
        if (TypingManager.Instance != null && TypingManager.Instance.showDebugInfo)
            Debug.Log($"개별 오타: {GetDisplayWord()} 리셋됨");

        ResetTypingProgress();
    }

    /// <summary>
    /// 오타 시각적 효과 표시
    /// </summary>
    public void ShowTypoEffect()
    {
        if (wordDisplay != null)
            wordDisplay.ShowTypoEffect();
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
        if (isKoreanWord)
            return !IsCompleted && koreanJamoArray != null && currentProgress >= koreanJamoArray.Length;
        else
            return !IsCompleted && currentProgress >= Word.Length;
    }

    /// <summary>
    /// 현재 타겟이 첫 글자를 기다리고 있는지 확인
    /// </summary>
    public bool IsWaitingForFirstChar()
    {
        return currentProgress == 0;
    }

    /// <summary>
    /// 화면에 표시할 단어 반환 (디버그용)
    /// </summary>
    public string GetDisplayWord()
    {
        return Word;
    }

    /// <summary>
    /// 현재 입력된 부분의 실제 한글 문자열 반환 (한국어용)
    /// </summary>
    public string GetCurrentKoreanText()
    {
        if (!isKoreanWord || koreanJamoArray == null || currentProgress <= 0)
            return "";

        // 현재까지 입력된 자모들로 한글 조합
        string[] inputJamos = new string[currentProgress];
        System.Array.Copy(koreanJamoArray, 0, inputJamos, 0, currentProgress);

        return CombineJamosToKorean(inputJamos);
    }

    /// <summary>
    /// 자모 배열을 한글로 조합
    /// </summary>
    private string CombineJamosToKorean(string[] jamos)
    {
        if (jamos == null || jamos.Length == 0) return "";

        // 간단한 구현: 각 글자별로 자모를 조합
        // 실제로는 더 복잡한 로직이 필요할 수 있음
        string result = "";
        int i = 0;

        while (i < jamos.Length)
            // 초성이 있는지 확인
            if (i < jamos.Length && IsChosung(jamos[i]))
            {
                string chosung = jamos[i];
                i++;

                // 중성이 있는지 확인
                if (i < jamos.Length && IsJungsung(jamos[i]))
                {
                    string jungsung = jamos[i];
                    i++;

                    // 종성이 있는지 확인
                    string jongsung = "";
                    if (i < jamos.Length && IsJongsung(jamos[i]))
                    {
                        jongsung = jamos[i];
                        i++;
                    }

                    // 한글 조합
                    char combinedChar = CombineJamos(chosung, jungsung, jongsung);
                    result += combinedChar;
                }
                else
                {
                    // 중성이 없다면 자모 그대로 표시
                    result += chosung;
                    // i는 이미 증가됨
                }
            }
            else
            {
                // 초성이 아니라면 그대로 표시
                result += jamos[i];
                i++;
            }

        return result;
    }

    /// <summary>
    /// 개별 자모를 한글로 조합
    /// </summary>
    private char CombineJamos(string chosung, string jungsung, string jongsung = "")
    {
        // KoreanTool의 자모 리스트를 사용하여 인덱스 찾기
        int chosungIndex = GetChosungIndex(chosung);
        int jungsungIndex = GetJungsungIndex(jungsung);
        int jongsungIndex = GetJongsungIndex(jongsung);

        if (chosungIndex >= 0 && jungsungIndex >= 0)
            return (char)(chosungIndex * 21 * 28 + jungsungIndex * 28 + jongsungIndex + 0xAC00);

        return chosung[0]; // 조합 실패시 초성만 반환
    }

    // 자모 타입 확인 헬퍼 메서드들
    private bool IsChosung(string jamo)
    {
        char[] chosungList =
        {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };
        return jamo.Length == 1 && System.Array.IndexOf(chosungList, jamo[0]) >= 0;
    }

    private bool IsJungsung(string jamo)
    {
        char[] jungsungList =
        {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ', 'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
        };
        return jamo.Length == 1 && System.Array.IndexOf(jungsungList, jamo[0]) >= 0;
    }

    private bool IsJongsung(string jamo)
    {
        char[] jongsungList =
        {
            'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ', 'ㅇ',
            'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };
        return jamo.Length == 1 && System.Array.IndexOf(jongsungList, jamo[0]) >= 0;
    }

    private int GetChosungIndex(string chosung)
    {
        char[] chosungList =
        {
            'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ', 'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };
        return chosung.Length == 1 ? System.Array.IndexOf(chosungList, chosung[0]) : -1;
    }

    private int GetJungsungIndex(string jungsung)
    {
        char[] jungsungList =
        {
            'ㅏ', 'ㅐ', 'ㅑ', 'ㅒ', 'ㅓ', 'ㅔ', 'ㅕ', 'ㅖ', 'ㅗ', 'ㅘ', 'ㅙ', 'ㅚ', 'ㅛ', 'ㅜ', 'ㅝ', 'ㅞ', 'ㅟ', 'ㅠ', 'ㅡ', 'ㅢ', 'ㅣ'
        };
        return jungsung.Length == 1 ? System.Array.IndexOf(jungsungList, jungsung[0]) : -1;
    }

    private int GetJongsungIndex(string jongsung)
    {
        if (string.IsNullOrEmpty(jongsung)) return 0; // 종성 없음

        char[] jongsungList =
        {
            ' ', 'ㄱ', 'ㄲ', 'ㄳ', 'ㄴ', 'ㄵ', 'ㄶ', 'ㄷ', 'ㄹ', 'ㄺ', 'ㄻ', 'ㄼ', 'ㄽ', 'ㄾ', 'ㄿ', 'ㅀ', 'ㅁ', 'ㅂ', 'ㅄ', 'ㅅ', 'ㅆ',
            'ㅇ', 'ㅈ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
        };
        return jongsung.Length == 1 ? System.Array.IndexOf(jongsungList, jongsung[0]) : 0;
    }

    public void SetTypingProgress(int typedCharacters)
    {
        int maxProgress = isKoreanWord ? koreanJamoArray?.Length ?? 0 : Word?.Length ?? 0;

        currentProgress = Mathf.Clamp(typedCharacters, 0, maxProgress);
        UpdateDisplay();
    }

    public void ResetTypingProgress()
    {
        currentProgress = 0;
        IsCompleted = false;
        UpdateDisplay();

        if (wordDisplay != null)
            wordDisplay.ResetToDefaultColor();
    }

    private void UpdateDisplay()
    {
        if (wordDisplay != null)
        {
            if (isKoreanWord)
                wordDisplay.UpdateKoreanTypingProgress(currentProgress, koreanJamoArray);
            else
                wordDisplay.UpdateTypingProgress(currentProgress);
        }
    }

    public void OnWordCompleted()
    {
        IsCompleted = true;

        // 시각적 효과
        if (wordDisplay != null)
            wordDisplay.ShowCompletionEffect();

        Debug.Log($"단어 완성됨: {Word}");

        // 단어 완성 이벤트 발생
        WordCompletionEvents.TriggerWordCompleted(transform);

        // 타이핑 매니저에서 제거
        if (TypingManager.Instance != null)
            TypingManager.Instance.UnregisterTarget(this);

        // 완성 효과 후 새로운 단어 할당 (0.5초 후)
        StartCoroutine(AssignNewWordAfterDelay(0.5f));
    }

    private System.Collections.IEnumerator AssignNewWordAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // IEnemy 인터페이스로 깔끔하게 생존 상태 확인!
        bool isEnemyAlive = enemy != null && !enemy.IsDead;

        if (gameObject.activeInHierarchy && isEnemyAlive)
        {
            IsCompleted = false;
            AssignRandomWord();

            // 타이핑 매니저에 다시 등록
            if (TypingManager.Instance != null)
                TypingManager.Instance.RegisterTarget(this);
        }
    }
}
