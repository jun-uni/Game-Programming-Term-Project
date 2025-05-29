using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI 요소에 추가할 수 있는 컴포넌트
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    private Text textComponent;
    private TextMeshProUGUI tmpComponent;

    private void Awake()
    {
        // UI Text와 TextMeshPro 모두 지원
        textComponent = GetComponent<Text>();
        tmpComponent = GetComponent<TextMeshProUGUI>();

        // LocalizationManager 언어 변경 이벤트에 구독
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;

        // 초기 텍스트 설정
        if (LocalizationManager.IsInitialized)
            UpdateText();
        else
            // 초기화 대기
            LocalizationManager.OnLanguageInitialized += OnLocalizationManagerInitialized;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    // 언어 변경 시 호출될 메소드
    private void OnLanguageChanged(SystemLanguage newLanguage)
    {
        UpdateText();
    }

    // 텍스트 업데이트
    private void UpdateText()
    {
        string localizedText = localizationKey.Localize();

        if (textComponent != null)
            textComponent.text = localizedText;

        if (tmpComponent != null)
            tmpComponent.text = localizedText;
    }

    // 에디터에서 키 변경 시 업데이트를 위한 메소드
    public void SetKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }

    private void OnLocalizationManagerInitialized()
    {
        // 이벤트 한 번만 사용하고 구독 해제
        LocalizationManager.OnLanguageInitialized -= OnLocalizationManagerInitialized;
        UpdateText();
    }
}
