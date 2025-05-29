using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordDisplay : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Canvas worldCanvas;
    public TextMeshProUGUI wordText;

    [Header("색상 설정")]
    public Color defaultColor = Color.white;
    public Color typingColor = Color.yellow;
    public Color completedColor = Color.green;

    [Header("애니메이션")]
    public float bounceScale = 1.2f;
    public float animationDuration = 0.2f;

    private string currentWord;
    private int typedCharacters = 0;

    private void Awake()
    {
        // World Space Canvas 설정
        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();

        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = Camera.main;

        // Canvas 크기와 스케일 설정
        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200f, 50f); // Canvas 내부 크기
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // 실제 월드 크기를 작게

        // 적군 머리 위 적절한 높이에 위치 (부모 오브젝트 기준)
        canvasRect.localPosition = new Vector3(0f, 2.2f, 0f); // Y값 조정으로 높이 설정

        // 텍스트 설정
        if (wordText != null)
        {
            wordText.fontSize = 36f; // Canvas 스케일이 0.01이므로 큰 폰트 사이즈 필요
            wordText.alignment = TextAlignmentOptions.Center;
        }
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
        UpdateDisplay();
    }

    public void UpdateTypingProgress(int typed)
    {
        typedCharacters = typed;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (wordText == null || string.IsNullOrEmpty(currentWord))
            return;

        string displayText = "";

        // 입력된 글자가 없으면 전체를 기본 색상으로
        if (typedCharacters <= 0)
        {
            displayText = $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{currentWord}</color>";
        }
        else
        {
            for (int i = 0; i < currentWord.Length; i++)
            {
                if (i < typedCharacters)
                {
                    // 입력된 글자는 노란색
                    displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(typingColor)}>{currentWord[i]}</color>";
                }
                else
                {
                    // 아직 입력되지 않은 글자는 기본 색상
                    displayText += $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{currentWord[i]}</color>";
                }
            }
        }

        wordText.text = displayText;
    }

    public void ShowCompletionEffect()
    {
        // 원래 스케일 저장
        Vector3 originalScale = transform.localScale;
        Vector3 bounceTargetScale = originalScale * bounceScale;

        // 완성 애니메이션 - 원래 스케일 기준으로 상대적 크기 조정
        LeanTween.scale(gameObject, bounceTargetScale, animationDuration / 2)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() => {
                LeanTween.scale(gameObject, originalScale, animationDuration / 2)
                    .setEase(LeanTweenType.easeInBack)
                    .setOnComplete(() => {
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
            // 전체 단어를 기본 색상으로 강제 설정
            string displayText = $"<color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{currentWord}</color>";
            wordText.text = displayText;
            typedCharacters = 0; // 진행상황도 초기화
        }
    }

}
