using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

// 게임 매니저와 통합할 수 있는 정적 클래스 버전의 LocalizationManager
[DefaultExecutionOrder(-100)]
public static class LocalizationManager
{
    private static SystemLanguage systemLanguage;

    private static SystemLanguage gameLanguage;

    public static SystemLanguage GameLanguage => gameLanguage;

    public static SystemLanguage CurrentSystemLanguage
    {
        get => systemLanguage;
        set
        {
            if (systemLanguage != value)
            {
                systemLanguage = value;
                OnLanguageChanged?.Invoke(systemLanguage);
            }
        }
    }


    public delegate void LanguageChangedHandler(SystemLanguage newLanguage);

    public static event LanguageChangedHandler OnLanguageChanged;

    public static event Action OnLanguageInitialized;


    // 로컬라이제이션 데이터
    private static Dictionary<string, Dictionary<string, string>> localizedTexts = new();

    // 초기화 완료 여부
    private static bool isInitialized = false;
    private static bool isInitializing = false;

    public static bool IsInitialized => isInitialized;

    // 지원하는 언어 목록 (일본어 제외)
    public static List<SystemLanguage> SupportedLanguages { get; } = new()
    {
        SystemLanguage.English, SystemLanguage.Korean, SystemLanguage.Spanish, SystemLanguage.French
    };

    // 매니저 초기화 (게임 매니저 Start에서 호출)
    public static void Initialize(MonoBehaviour coroutineRunner)
    {
        if (isInitialized || isInitializing)
            return;

        isInitializing = true;
        Debug.Log("LocalizationManager 초기화 시작");

        // 시스템 언어 감지 또는 저장된 설정 불러오기
        LoadSystemLanguage();
        LoadGameLanguage();

        // 언어 파일 로딩
        string filePath = Path.Combine(Application.streamingAssetsPath, "Language.json");

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // WebGL에서는 코루틴 완료 후 초기화 완료 처리
            coroutineRunner.StartCoroutine(LoadLocalizedTextWebGL(filePath));
        }
        else
        {
            // 일반 플랫폼에서는 동기적으로 로딩 후 즉시 초기화 완료
            LoadLocalizedText(filePath);
            CompleteInitialization();
        }
    }

    // 초기화 완료 처리를 별도 메서드로 분리
    private static void CompleteInitialization()
    {
        if (isInitialized)
        {
            Debug.LogWarning("LocalizationManager가 이미 초기화되었습니다.");
            return;
        }

        isInitialized = true;
        isInitializing = false;

        Debug.Log("LocalizationManager 초기화 완료");

        // 초기화 완료 이벤트 발생
        try
        {
            OnLanguageInitialized?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"OnLanguageInitialized 이벤트 처리 중 오류: {e.Message}");
        }
    }

    // 시스템 언어 감지 또는 설정 불러오기
    private static void LoadSystemLanguage()
    {
        // PlayerPrefs에서 저장된 언어 설정 확인
        if (PlayerPrefs.HasKey("SystemLanguage"))
        {
            string savedLanguage = PlayerPrefs.GetString("SystemLanguage");
            if (Enum.TryParse(savedLanguage, out SystemLanguage language))
            {
                systemLanguage = language;
                Debug.Log("시스템 언어 불러옴" + systemLanguage.ToString());

                return;
            }
        }

        // 저장된 설정이 없으면 시스템 언어 감지
        SystemLanguage deviceLanguage = Application.systemLanguage;

        // 지원하는 언어인지 확인
        if (SupportedLanguages.Contains(deviceLanguage))
            systemLanguage = deviceLanguage;
        else
            // 기본 언어로 영어 설정
            systemLanguage = SystemLanguage.English;

        // 설정 저장
        PlayerPrefs.SetString("SystemLanguage", systemLanguage.ToString());
        PlayerPrefs.Save();
    }

    private static void LoadGameLanguage()
    {
        // PlayerPrefs에서 저장된 언어 설정 확인
        if (PlayerPrefs.HasKey("GameLanguage"))
        {
            string savedLanguage = PlayerPrefs.GetString("GameLanguage");
            if (Enum.TryParse(savedLanguage, out SystemLanguage language))
            {
                gameLanguage = language;
                Debug.Log("게임 언어 불러옴" + gameLanguage.ToString());
                return;
            }
        }

        // 저장된 설정이 없으면 시스템 언어 감지
        SystemLanguage deviceLanguage = SystemLanguage.English;

        // 지원하는 언어인지 확인
        if (SupportedLanguages.Contains(deviceLanguage))
            gameLanguage = deviceLanguage;
        else
            // 기본 언어로 영어 설정
            gameLanguage = SystemLanguage.English;

        // 설정 저장
        PlayerPrefs.SetString("GameLanguage", gameLanguage.ToString());
        PlayerPrefs.Save();
    }

    // 언어 파일 로딩
    private static void LoadLocalizedText(string filePath)
    {
        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            ProcessJsonData(jsonText);
        }
        else
        {
            Debug.LogError($"Cannot find localization file at path: {filePath}");
        }
    }

    // JSON 데이터 처리 - 스페인어, 프랑스어 추가
    private static void ProcessJsonData(string jsonText)
    {
        try
        {
            // Newtonsoft.Json 사용하여 파싱
            Dictionary<string, LocalizationEntry> jsonData =
                JsonConvert.DeserializeObject<Dictionary<string, LocalizationEntry>>(jsonText);

            if (jsonData == null)
            {
                Debug.LogError("Failed to parse JSON data");
                return;
            }

            // JSON 데이터를 순회하며 로컬라이제이션 데이터 구축
            foreach (KeyValuePair<string, LocalizationEntry> entry in jsonData)
                if (!string.IsNullOrEmpty(entry.Value.key))
                {
                    Dictionary<string, string> translations = new();

                    // 모든 지원 언어 처리
                    if (!string.IsNullOrEmpty(entry.Value.ko))
                        translations["ko"] = entry.Value.ko;

                    if (!string.IsNullOrEmpty(entry.Value.eng))
                        translations["eng"] = entry.Value.eng;

                    if (!string.IsNullOrEmpty(entry.Value.es))
                        translations["es"] = entry.Value.es;

                    if (!string.IsNullOrEmpty(entry.Value.fr))
                        translations["fr"] = entry.Value.fr;

                    localizedTexts[entry.Value.key] = translations;
                }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing JSON data: {e.Message}");
        }
    }

    // WebGL 플랫폼을 위한 비동기 로딩
    private static IEnumerator LoadLocalizedTextWebGL(string filePath)
    {
        Debug.Log($"WebGL에서 언어 파일 로딩 시작: {filePath}");

        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
        yield return www.SendWebRequest();

        // 초기화 중복 체크 (코루틴 실행 중에 다른 곳에서 초기화가 완료되었을 수 있음)
        if (isInitialized)
        {
            Debug.Log("WebGL 로딩 중 이미 다른 곳에서 초기화가 완료됨");
            yield break;
        }

        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string jsonText = www.downloadHandler.text;
            Debug.Log("WebGL 언어 파일 로딩 성공, JSON 파싱 시작");

            try
            {
                ProcessJsonData(jsonText);
                Debug.Log("JSON 파싱 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON 파싱 중 오류: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"Error loading localization file: {www.error}");
        }

        // WebGL에서 로딩 완료 후 초기화 완료 처리
        CompleteInitialization();
    }

    // 언어 코드 얻기 - 스페인어, 프랑스어 추가
    private static string GetLanguageCode(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.English:
                return "eng";
            case SystemLanguage.Korean:
                return "ko";
            case SystemLanguage.Spanish:
                return "es";
            case SystemLanguage.French:
                return "fr";
            default:
                return "eng"; // 기본값
        }
    }

    // 키를 사용하여 현재 언어로 텍스트 얻기
    public static string GetLocalizedText(string key)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("LocalizationManager not initialized. Call Initialize() first.");
            return key;
        }

        string languageCode = GetLanguageCode(systemLanguage);

        // 키가 없는 경우 키 자체를 반환 (디버깅 용이)
        if (!localizedTexts.ContainsKey(key) || !localizedTexts[key].ContainsKey(languageCode))
        {
            Debug.LogWarning($"Localization key not found: {key} for language {languageCode}");

            // 영어로 폴백
            if (languageCode != "eng" && localizedTexts.ContainsKey(key) &&
                localizedTexts[key].ContainsKey("eng"))
                return localizedTexts[key]["eng"];

            return key;
        }

        return localizedTexts[key][languageCode];
    }

    // 포맷팅을 지원하는 버전 (예: "Hello, {0}!" -> "Hello, Player!")
    public static string GetLocalizedText(string key, params object[] args)
    {
        string value = GetLocalizedText(key);

        if (args != null && args.Length > 0)
            try
            {
                return string.Format(value, args);
            }
            catch (FormatException ex)
            {
                Debug.LogError($"Format error for key '{key}': {ex.Message}");
                return value;
            }

        return value;
    }

    // 언어 변경 메서드
    public static void ChangeSystemLanguage(SystemLanguage language)
    {
        if (SupportedLanguages.Contains(language))
        {
            CurrentSystemLanguage = language;
            PlayerPrefs.SetString("SystemLanguage", language.ToString());
            PlayerPrefs.Save();

            Debug.Log($"시스템 언어 변경됨: {language} (코드: {GetLanguageCode(language)})");
        }
        else
        {
            Debug.LogWarning($"Language {language} is not supported");
        }
    }

    public static void ChangeGameLanguage(SystemLanguage language)
    {
        if (SupportedLanguages.Contains(language))
        {
            gameLanguage = language;
            PlayerPrefs.SetString("GameLanguage", language.ToString());
            PlayerPrefs.Save();

            Debug.Log($"게임 언어 변경됨: {language}");
        }
        else
        {
            Debug.LogWarning($"Language {language} is not supported");
        }
    }

    // 로컬라이제이션 데이터 구조 - 스페인어, 프랑스어 필드 추가
    [Serializable]
    private class LocalizationEntry
    {
        public string key;
        public string ko;
        public string eng;
        public string es; // 스페인어 추가
        public string fr; // 프랑스어 추가
    }
}

// 확장 메소드
public static class LocalizationExtensions
{
    // string 확장 메소드: 직접 키를 문자열로 변환
    public static string Localize(this string key)
    {
        return LocalizationManager.GetLocalizedText(key);
    }

    // string 확장 메소드: 매개변수를 포함하는 로컬라이제이션
    public static string Localize(this string key, params object[] args)
    {
        return LocalizationManager.GetLocalizedText(key, args);
    }
}
