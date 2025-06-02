using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordDisplay : MonoBehaviour
{
    [Header("UI 컴포넌트")] public Canvas worldCanvas;
    public TextMeshProUGUI wordText;

    [Header("색상 설정")] public Color defaultColor = Color.white;
    public Color typingColor = Color.yellow;
    public Color completedColor = Color.green;

    [Header("애니메이션")] public float bounceScale = 1.2f;
    public float animationDuration = 0.2f;

    [Header("한국어 지원")] private bool isKoreanMode = false;
    private string[] koreanJamoArray;

    private string currentWord;
    private int typedCharacters = 0;

    private void Awake()
    {
        // World Space Canvas 설정
        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();

        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = Camera.main;
    }

    private void Update()
    {
        // 항상 카메라를 향하도록
        Vector3 directionToCamera = Camera.main.transform.position - transform.position;
        directionToCamera.y = 0; // Y축 회전만
        transform.rotation = Quaternion.LookRotation(-directionToCamera);
    }

    public void SetWord(string word)
    {
        currentWord = word;

        // 한국어인지 확인
        isKoreanMode = IsKoreanWord(word);

        if (isKoreanMode)
            koreanJamoArray = KoreanTool.SplitKoreanCharacters(word);
        else
            koreanJamoArray = null;

        UpdateDisplay();
    }

    public void UpdateTypingProgress(int typed)
    {
        typedCharacters = typed;
        UpdateDisplay();
    }

    public void UpdateKoreanTypingProgress(int typedJamos, string[] jamoArray)
    {
        typedCharacters = typedJamos;
        koreanJamoArray = jamoArray;
        isKoreanMode = true;
        UpdateKoreanDisplay();
    }

    private bool IsKoreanWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;

        foreach (char c in word)
            if (c >= 0xAC00 && c <= 0xD7AF) // 한글 음절 범위
                return true;
        return false;
    }

    private void UpdateDisplay()
    {
        if (wordText == null || string.IsNullOrEmpty(currentWord))
            return;

        if (isKoreanMode)
            UpdateKoreanDisplay();
        else
            UpdateEnglishDisplay();
    }

    private void UpdateEnglishDisplay()
    {
        string displayText = "";

        // 입력된 글자가 없으면 전체를 기본 색상으로
        if (typedCharacters <= 0)
            displayText = $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{currentWord}</color>";
        else
            for (int i = 0; i < currentWord.Length; i++)
                if (i < typedCharacters)
                    // 입력된 글자는 노란색
                    displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(typingColor)}>{currentWord[i]}</color>";
                else
                    // 아직 입력되지 않은 글자는 기본 색상
                    displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{currentWord[i]}</color>";

        wordText.text = displayText;
    }

    private void UpdateKoreanDisplay()
    {
        if (koreanJamoArray == null || koreanJamoArray.Length == 0)
        {
            wordText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{currentWord}</color>";
            return;
        }

        // 현재 자모 진행도를 바탕으로 완성된 글자 수 계산
        int completedCharacters = CalculateCompletedKoreanCharacters();

        string displayText = "";

        // 원본 단어를 글자 단위로 분할
        char[] originalChars = currentWord.ToCharArray();

        for (int i = 0; i < originalChars.Length; i++)
            if (i < completedCharacters)
                // 완성된 글자는 노란색
                displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(typingColor)}>{originalChars[i]}</color>";
            else
                // 아직 완성되지 않은 글자는 기본색
                displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{originalChars[i]}</color>";

        wordText.text = displayText;
    }

    /// <summary>
    /// 현재 자모 진행도를 바탕으로 완성된 한글 글자 수 계산
    /// </summary>
    private int CalculateCompletedKoreanCharacters()
    {
        if (koreanJamoArray == null || koreanJamoArray.Length == 0 || typedCharacters <= 0)
            return 0;

        // 원본 한글 단어의 각 글자가 몇 개의 자모로 구성되는지 계산
        char[] originalChars = currentWord.ToCharArray();
        int completedChars = 0;
        int currentJamoIndex = 0;

        foreach (char koreanChar in originalChars)
        {
            // 이 글자가 몇 개의 자모로 구성되는지 계산
            string[] charJamos = KoreanTool.SplitKoreanCharacters(koreanChar.ToString());
            int jamosInThisChar = charJamos.Length;

            // 이 글자를 완성하기 위해 필요한 자모가 모두 입력되었는지 확인
            if (currentJamoIndex + jamosInThisChar <= typedCharacters)
            {
                completedChars++;
                currentJamoIndex += jamosInThisChar;
            }
            else
            {
                // 이 글자는 아직 완성되지 않음
                break;
            }
        }

        return completedChars;
    }

    private string CombineJamosToKorean(string[] jamos)
    {
        if (jamos == null || jamos.Length == 0) return "";

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

    private char CombineJamos(string chosung, string jungsung, string jongsung = "")
    {
        int chosungIndex = GetChosungIndex(chosung);
        int jungsungIndex = GetJungsungIndex(jungsung);
        int jongsungIndex = GetJongsungIndex(jongsung);

        if (chosungIndex >= 0 && jungsungIndex >= 0)
            return (char)(chosungIndex * 21 * 28 + jungsungIndex * 28 + jongsungIndex + 0xAC00);

        return chosung.Length > 0 ? chosung[0] : '?';
    }

    // 자모 타입 확인 및 인덱스 반환 헬퍼 메서드들
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

    public void ShowCompletionEffect()
    {
        // 원래 스케일 저장
        Vector3 originalScale = transform.localScale;
        Vector3 bounceTargetScale = originalScale * bounceScale;

        // 완성 애니메이션 - 원래 스케일 기준으로 상대적 크기 조정
        LeanTween.scale(gameObject, bounceTargetScale, animationDuration / 2)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, originalScale, animationDuration / 2)
                    .setEase(LeanTweenType.easeInBack)
                    .setOnComplete(() =>
                    {
                        // 애니메이션 완료 후 기본 색상으로 복원
                        ResetToDefaultColor();
                    });
            });

        // 색상 변경
        wordText.color = completedColor;
    }

    public void ResetToDefaultColor()
    {
        if (wordText != null && !string.IsNullOrEmpty(currentWord))
        {
            typedCharacters = 0; // 진행상황도 초기화
            UpdateDisplay(); // 전체적으로 다시 업데이트
        }
    }
}
