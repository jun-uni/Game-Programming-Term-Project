using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SettingsPanelController : MonoBehaviour
{
    [Header("소리 설정")] [SerializeField] private Slider volumeSlider;

    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private int volumeSteps = 10;

    [Header("버튼들")] [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button systemLanguageEnglishButton;
    [SerializeField] private Button systemLanguageKoreanButton;
    [SerializeField] private Button systemLanguageSpanishButton;
    [SerializeField] private Button systemLanguageFrenchButton;
    [SerializeField] private Button gameLanguageEnglishButton;
    [SerializeField] private Button gameLanguageKoreanButton;

    [Header("버튼 색깔")] [SerializeField] private Color selectedButtonColor = Color.cyan;

    [SerializeField] private Color normalButtonColor = Color.white;


    // 현재 설정값 (임시 저장)
    private float currentVolume;
    private SystemLanguage currentSystemLanguage;
    private SystemLanguage currentGameLanguage;

    // 원래 설정값 (취소 시 복원용)
    private float originalVolume;
    private SystemLanguage originalSystemLanguage;
    private SystemLanguage originalGameLanguage;


    private void Awake()
    {
    }

    private void Start()
    {
    }

    private void OnEnable()
    {
        originalSystemLanguage = LocalizationManager.CurrentSystemLanguage;
        originalGameLanguage = LocalizationManager.GameLanguage;
        currentSystemLanguage = originalSystemLanguage;
        currentGameLanguage = originalGameLanguage;

        // 볼륨 설정 초기화
        InitializeVolumeSettings();
        InitializeLanguageButtons();
    }


    #region Volume Settings

    // 볼륨 설정 초기화
    private void InitializeVolumeSettings()
    {
        // AudioManager에서 현재 볼륨 값 가져오기
        if (GameManager.Instance != null)
            originalVolume = GameManager.Instance.GetVolumes();

        // 단계에 맞게 조정
        originalVolume = RoundToStep(originalVolume);

        // 현재 값 초기화
        currentVolume = originalVolume;

        // 슬라이더 설정
        SetupVolumeSliders();
    }

    // 볼륨 슬라이더 초기화 및 이벤트 설정
    private void SetupVolumeSliders()
    {
        // BGM 슬라이더 설정
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = currentVolume;
            volumeSlider.onValueChanged.AddListener(OnvolumeSliderChanged);
            UpdateBGMValueText(currentVolume);
        }
    }

    // BGM 슬라이더 변경 이벤트
    private void OnvolumeSliderChanged(float value)
    {
        // 단계별 값으로 변환
        float steppedValue = RoundToStep(value);

        // 슬라이더 값 설정 (중복 호출 방지)
        if (volumeSlider.value != steppedValue)
            volumeSlider.value = steppedValue;

        // 현재 임시 값 업데이트
        currentVolume = steppedValue;

        // AudioManager를 통해 오디오 믹서 업데이트 (즉시 들리도록)
        if (GameManager.Instance != null)
            GameManager.Instance.SetVolumes(steppedValue);

        // 텍스트 업데이트
        UpdateBGMValueText(steppedValue);
    }


    // 값을 단계별로 변환하는 함수
    private float RoundToStep(float value)
    {
        // 0-1 사이의 값을 0-volumeSteps 사이의 단계로 변환 후 다시 0-1로 정규화
        int step = Mathf.RoundToInt(value * volumeSteps);
        return (float)step / volumeSteps;
    }

    // BGM 값 텍스트 업데이트
    private void UpdateBGMValueText(float value)
    {
        if (volumeValueText != null)
        {
            // 0-10 단계로 표시
            int volumeLevel = Mathf.RoundToInt(value * volumeSteps);
            volumeValueText.text = volumeLevel.ToString();
        }
    }

    #endregion


    #region Language Settings

    public void InitializeLanguageButtons()
    {
        // 시스템 언어 버튼 색상 설정
        UpdateSystemLanguageButtonColors(currentSystemLanguage);

        // 게임 언어 버튼 색상 설정 (기존 방식 유지)
        if (currentGameLanguage == SystemLanguage.English)
        {
            gameLanguageEnglishButton.image.color = selectedButtonColor;
            gameLanguageKoreanButton.image.color = normalButtonColor;
        }
        else
        {
            gameLanguageEnglishButton.image.color = normalButtonColor;
            gameLanguageKoreanButton.image.color = selectedButtonColor;
        }
    }

    /// <summary>
    /// 시스템 언어 버튼들의 색상을 업데이트하는 공통 함수
    /// </summary>
    /// <param name="selectedLanguage">선택된 언어</param>
    private void UpdateSystemLanguageButtonColors(SystemLanguage selectedLanguage)
    {
        // 모든 시스템 언어 버튼을 기본 색상으로 초기화
        systemLanguageEnglishButton.image.color = normalButtonColor;
        systemLanguageKoreanButton.image.color = normalButtonColor;
        systemLanguageSpanishButton.image.color = normalButtonColor;
        systemLanguageFrenchButton.image.color = normalButtonColor;

        // 선택된 언어 버튼만 활성화 색상으로 설정
        switch (selectedLanguage)
        {
            case SystemLanguage.English:
                systemLanguageEnglishButton.image.color = selectedButtonColor;
                break;
            case SystemLanguage.Korean:
                systemLanguageKoreanButton.image.color = selectedButtonColor;
                break;
            case SystemLanguage.Spanish:
                systemLanguageSpanishButton.image.color = selectedButtonColor;
                break;
            case SystemLanguage.French:
                systemLanguageFrenchButton.image.color = selectedButtonColor;
                break;
            default:
                // 지원하지 않는 언어의 경우 영어를 기본으로 설정
                systemLanguageEnglishButton.image.color = selectedButtonColor;
                break;
        }
    }

    public void OnClickSystemLanguageKoreanButton()
    {
        currentSystemLanguage = SystemLanguage.Korean;
        LocalizationManager.ChangeSystemLanguage(SystemLanguage.Korean);
        UpdateSystemLanguageButtonColors(SystemLanguage.Korean);
    }

    public void OnClickSystemLanguageEnglishButton()
    {
        currentSystemLanguage = SystemLanguage.English;
        LocalizationManager.ChangeSystemLanguage(SystemLanguage.English);
        UpdateSystemLanguageButtonColors(SystemLanguage.English);
    }

    public void OnClickSystemLanguageJapaneseButton()
    {
        currentSystemLanguage = SystemLanguage.Japanese;
        LocalizationManager.ChangeSystemLanguage(SystemLanguage.Japanese);
        UpdateSystemLanguageButtonColors(SystemLanguage.Japanese);
    }

    public void OnClickSystemLanguageSpanishButton()
    {
        currentSystemLanguage = SystemLanguage.Spanish;
        LocalizationManager.ChangeSystemLanguage(SystemLanguage.Spanish);
        UpdateSystemLanguageButtonColors(SystemLanguage.Spanish);
    }

    public void OnClickSystemLanguageFrenchButton()
    {
        currentSystemLanguage = SystemLanguage.French;
        LocalizationManager.ChangeSystemLanguage(SystemLanguage.French);
        UpdateSystemLanguageButtonColors(SystemLanguage.French);
    }

    public void OnClickGameLanguageKoreanButton()
    {
        currentGameLanguage = SystemLanguage.Korean;
        LocalizationManager.ChangeGameLanguage(SystemLanguage.Korean);
        gameLanguageEnglishButton.image.color = normalButtonColor;
        gameLanguageKoreanButton.image.color = selectedButtonColor;
    }

    public void OnClickGameLanguageEnglishButton()
    {
        currentGameLanguage = SystemLanguage.English;
        LocalizationManager.ChangeGameLanguage(SystemLanguage.English);
        gameLanguageEnglishButton.image.color = selectedButtonColor;
        gameLanguageKoreanButton.image.color = normalButtonColor;
    }

    #endregion

    #region Confirm/Cancel Buttons

    // 확인 버튼 이벤트 - 설정 저장
    public void ConfirmSettings()
    {
        // 볼륨 설정 저장
        if (GameManager.Instance != null) GameManager.Instance.SaveVolumeSettings(currentVolume);

        // 원래 값 업데이트 (다음 취소를 위해)
        originalVolume = currentVolume;
        originalSystemLanguage = currentGameLanguage;
        originalGameLanguage = currentGameLanguage;


        Debug.Log("설정이 저장되었습니다!");

        // 설정창 닫기
        CloseSettingsPanel();
    }

    // 취소 버튼 이벤트 - 원래 설정으로 복원
    public void CancelSettings()
    {
        // 볼륨 설정 복원
        currentVolume = originalVolume;


        if (GameManager.Instance != null) GameManager.Instance.SetVolumes(originalVolume);

        // 볼륨 UI 업데이트
        if (volumeSlider != null)
            volumeSlider.value = originalVolume;

        // 언어 설정 복원
        currentSystemLanguage = originalSystemLanguage;
        currentGameLanguage = originalGameLanguage;

        if (LocalizationManager.IsInitialized)
        {
            LocalizationManager.ChangeSystemLanguage(originalSystemLanguage);
            LocalizationManager.ChangeGameLanguage(originalGameLanguage);
        }

        // 언어 버튼 색상도 원래대로 복원
        UpdateSystemLanguageButtonColors(originalSystemLanguage);

        // 게임 언어 버튼도 복원
        if (originalGameLanguage == SystemLanguage.English)
        {
            gameLanguageEnglishButton.image.color = selectedButtonColor;
            gameLanguageKoreanButton.image.color = normalButtonColor;
        }
        else
        {
            gameLanguageEnglishButton.image.color = normalButtonColor;
            gameLanguageKoreanButton.image.color = selectedButtonColor;
        }

        // 설정창 닫기
        CloseSettingsPanel();
    }

    private void CloseSettingsPanel()
    {
        // 직접 비활성화
        gameObject.SetActive(false);
    }

    #endregion
}
