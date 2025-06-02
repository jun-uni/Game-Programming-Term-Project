using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WordListData
{
    public string[] words;
}

public class WordDatabase : MonoBehaviour
{
    public static WordDatabase Instance;

    [Header("영어 단어 리스트")] public List<string> easyWords = new();
    public List<string> mediumWords = new();
    public List<string> hardWords = new();
    public List<string> allWords = new(); // 전체 단어 목록

    [Header("한국어 단어 리스트")] public List<string> easyKoreanWords = new();
    public List<string> mediumKoreanWords = new();
    public List<string> hardKoreanWords = new();
    public List<string> allKoreanWords = new();

    [Header("난이도 분류 설정")] [Tooltip("Easy 난이도 최대 글자 수")]
    public int easyMaxLength = 4;

    [Tooltip("Medium 난이도 최대 글자 수")] public int mediumMaxLength = 6;

    [Header("한국어 난이도 분류 설정")] [Tooltip("한국어 Easy 난이도 최대 자모 수")]
    public int koreanEasyMaxJamo = 6;

    [Tooltip("한국어 Medium 난이도 최대 자모 수")] public int koreanMediumMaxJamo = 9;

    [Header("난이도 설정")] public float easyWordChance = 0.5f;
    public float mediumWordChance = 0.3f;
    public float hardWordChance = 0.2f;

    [Header("JSON 파일 설정")] [Tooltip("Resources 폴더 내 영어 JSON 파일명 (확장자 제외)")]
    public string jsonFileName = "english word list";

    [Tooltip("Resources 폴더 내 한국어 JSON 파일명 (확장자 제외)")]
    public string koreanJsonFileName = "korean word list";

    [Header("디버그")] public bool showLoadingInfo = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadWordsFromJSON();
            LoadKoreanWordsFromJSON();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadWordsFromJSON()
    {
        try
        {
            // Resources 폴더에서 JSON 파일 로드
            TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);

            if (jsonFile == null)
            {
                Debug.LogError($"영어 JSON 파일을 찾을 수 없습니다: {jsonFileName}.json");
                LoadDefaultWords(); // 기본 단어로 대체
                return;
            }

            // JSON 파싱
            string jsonText = jsonFile.text;

            // JSON 배열을 파싱하기 위해 래퍼 객체 사용
            string wrappedJson = "{\"words\":" + jsonText + "}";
            WordListData wordListData = JsonUtility.FromJson<WordListData>(wrappedJson);

            if (wordListData?.words == null)
            {
                Debug.LogError("영어 JSON 파싱에 실패했습니다.");
                LoadDefaultWords();
                return;
            }

            // 단어 목록 초기화
            allWords.Clear();
            easyWords.Clear();
            mediumWords.Clear();
            hardWords.Clear();

            // 단어들을 길이에 따라 분류
            foreach (string word in wordListData.words)
            {
                if (string.IsNullOrEmpty(word)) continue;

                string trimmedWord = word.Trim().ToLower();
                if (trimmedWord.Length < 2) continue; // 너무 짧은 단어 제외

                allWords.Add(trimmedWord);

                // 길이에 따른 난이도 분류
                if (trimmedWord.Length <= easyMaxLength)
                    easyWords.Add(trimmedWord);
                else if (trimmedWord.Length <= mediumMaxLength)
                    mediumWords.Add(trimmedWord);
                else
                    hardWords.Add(trimmedWord);
            }

            if (showLoadingInfo)
            {
                Debug.Log($"영어 단어 로드 완료!");
                Debug.Log($"전체: {allWords.Count}개");
                Debug.Log($"Easy ({easyMaxLength}글자 이하): {easyWords.Count}개");
                Debug.Log($"Medium ({easyMaxLength + 1}-{mediumMaxLength}글자): {mediumWords.Count}개");
                Debug.Log($"Hard ({mediumMaxLength + 1}글자 이상): {hardWords.Count}개");
            }

            // 비어있는 카테고리 체크
            if (easyWords.Count == 0 || mediumWords.Count == 0 || hardWords.Count == 0)
                Debug.LogWarning("일부 영어 난이도 카테고리가 비어있습니다. 전체 목록에서 랜덤 선택으로 대체합니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"영어 JSON 로드 중 오류 발생: {e.Message}");
            LoadDefaultWords();
        }
    }

    private void LoadKoreanWordsFromJSON()
    {
        try
        {
            // Resources 폴더에서 한국어 JSON 파일 로드
            TextAsset jsonFile = Resources.Load<TextAsset>(koreanJsonFileName);

            if (jsonFile == null)
            {
                Debug.LogWarning($"한국어 JSON 파일을 찾을 수 없습니다: {koreanJsonFileName}.json");
                LoadDefaultKoreanWords(); // 기본 한국어 단어로 대체
                return;
            }

            // JSON 파싱
            string jsonText = jsonFile.text;
            string wrappedJson = "{\"words\":" + jsonText + "}";
            WordListData wordListData = JsonUtility.FromJson<WordListData>(wrappedJson);

            if (wordListData?.words == null)
            {
                Debug.LogError("한국어 JSON 파싱에 실패했습니다.");
                LoadDefaultKoreanWords();
                return;
            }

            // 한국어 단어 목록 초기화
            allKoreanWords.Clear();
            easyKoreanWords.Clear();
            mediumKoreanWords.Clear();
            hardKoreanWords.Clear();

            // 단어들을 자모 수에 따라 분류
            foreach (string word in wordListData.words)
            {
                if (string.IsNullOrEmpty(word)) continue;

                string trimmedWord = word.Trim();
                if (trimmedWord.Length < 1) continue;

                // 한글인지 확인
                if (!IsKoreanWord(trimmedWord)) continue;

                // 자모로 분리하여 길이 계산
                string[] jamos = KoreanTool.SplitKoreanCharacters(trimmedWord);
                int jamoCount = jamos.Length;

                allKoreanWords.Add(trimmedWord);

                // 자모 수에 따른 난이도 분류
                if (jamoCount <= koreanEasyMaxJamo)
                    easyKoreanWords.Add(trimmedWord);
                else if (jamoCount <= koreanMediumMaxJamo)
                    mediumKoreanWords.Add(trimmedWord);
                else
                    hardKoreanWords.Add(trimmedWord);
            }

            if (showLoadingInfo)
            {
                Debug.Log($"한국어 단어 로드 완료!");
                Debug.Log($"전체: {allKoreanWords.Count}개");
                Debug.Log($"Easy ({koreanEasyMaxJamo}자모 이하): {easyKoreanWords.Count}개");
                Debug.Log($"Medium ({koreanEasyMaxJamo + 1}-{koreanMediumMaxJamo}자모): {mediumKoreanWords.Count}개");
                Debug.Log($"Hard ({koreanMediumMaxJamo + 1}자모 이상): {hardKoreanWords.Count}개");
            }

            // 비어있는 카테고리 체크
            if (easyKoreanWords.Count == 0 || mediumKoreanWords.Count == 0 || hardKoreanWords.Count == 0)
                Debug.LogWarning("일부 한국어 난이도 카테고리가 비어있습니다. 전체 목록에서 랜덤 선택으로 대체합니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"한국어 JSON 로드 중 오류 발생: {e.Message}");
            LoadDefaultKoreanWords();
        }
    }

    private bool IsKoreanWord(string word)
    {
        foreach (char c in word)
            if (c >= 0xAC00 && c <= 0xD7AF) // 한글 음절 범위
                return true;
        return false;
    }

    private void LoadDefaultWords()
    {
        Debug.Log("기본 영어 단어 목록을 사용합니다.");

        easyWords = new List<string>
        {
            "cat",
            "dog",
            "run",
            "jump",
            "fire",
            "ice",
            "rock",
            "tree",
            "bird",
            "fish"
        };
        mediumWords = new List<string>
        {
            "house",
            "water",
            "green",
            "black",
            "white",
            "quick",
            "brave",
            "smart",
            "ghost",
            "magic"
        };
        hardWords = new List<string>
        {
            "dragon",
            "wizard",
            "castle",
            "forest",
            "lightning",
            "thunder",
            "crystal",
            "shadow",
            "warrior",
            "monster"
        };

        allWords = new List<string>();
        allWords.AddRange(easyWords);
        allWords.AddRange(mediumWords);
        allWords.AddRange(hardWords);
    }

    private void LoadDefaultKoreanWords()
    {
        Debug.Log("기본 한국어 단어 목록을 사용합니다.");

        easyKoreanWords = new List<string>
        {
            "고양이",
            "강아지",
            "달리기",
            "점프",
            "불",
            "얼음",
            "바위",
            "나무",
            "새",
            "물고기"
        };
        mediumKoreanWords = new List<string>
        {
            "집",
            "물",
            "초록색",
            "검정색",
            "하얀색",
            "빠른",
            "용감한",
            "똑똑한",
            "유령",
            "마법"
        };
        hardKoreanWords = new List<string>
        {
            "용",
            "마법사",
            "성",
            "숲",
            "번개",
            "천둥",
            "수정",
            "그림자",
            "전사",
            "괴물"
        };


        allKoreanWords = new List<string>();
        allKoreanWords.AddRange(easyKoreanWords);
        allKoreanWords.AddRange(mediumKoreanWords);
        allKoreanWords.AddRange(hardKoreanWords);
    }

    public string GetRandomWord()
    {
        float random = Random.Range(0f, 1f);

        if (random < easyWordChance)
            return GetRandomWordFromCategory(easyWords, "easy");
        else if (random < easyWordChance + mediumWordChance)
            return GetRandomWordFromCategory(mediumWords, "medium");
        else
            return GetRandomWordFromCategory(hardWords, "hard");
    }

    public string GetRandomKoreanWord()
    {
        float random = Random.Range(0f, 1f);

        if (random < easyWordChance)
            return GetRandomKoreanWordFromCategory(easyKoreanWords, "easy");
        else if (random < easyWordChance + mediumWordChance)
            return GetRandomKoreanWordFromCategory(mediumKoreanWords, "medium");
        else
            return GetRandomKoreanWordFromCategory(hardKoreanWords, "hard");
    }

    public string GetWordByDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            case 0: return GetRandomWordFromCategory(easyWords, "easy");
            case 1: return GetRandomWordFromCategory(mediumWords, "medium");
            case 2: return GetRandomWordFromCategory(hardWords, "hard");
            default: return GetRandomWord();
        }
    }

    public string GetKoreanWordByDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            case 0: return GetRandomKoreanWordFromCategory(easyKoreanWords, "easy");
            case 1: return GetRandomKoreanWordFromCategory(mediumKoreanWords, "medium");
            case 2: return GetRandomKoreanWordFromCategory(hardKoreanWords, "hard");
            default: return GetRandomKoreanWord();
        }
    }

    private string GetRandomWordFromCategory(List<string> categoryWords, string categoryName)
    {
        if (categoryWords.Count == 0)
        {
            Debug.LogWarning($"영어 {categoryName} 카테고리가 비어있습니다. 전체 목록에서 선택합니다.");
            if (allWords.Count > 0)
                return allWords[Random.Range(0, allWords.Count)];
            else
                return "word"; // 최후의 수단
        }

        return categoryWords[Random.Range(0, categoryWords.Count)];
    }

    private string GetRandomKoreanWordFromCategory(List<string> categoryWords, string categoryName)
    {
        if (categoryWords.Count == 0)
        {
            Debug.LogWarning($"한국어 {categoryName} 카테고리가 비어있습니다. 전체 목록에서 선택합니다.");
            if (allKoreanWords.Count > 0)
                return allKoreanWords[Random.Range(0, allKoreanWords.Count)];
            else
                return "단어"; // 최후의 수단
        }

        return categoryWords[Random.Range(0, categoryWords.Count)];
    }

    /// <summary>
    /// 런타임에서 JSON 파일을 다시 로드 (개발/테스트용)
    /// </summary>
    [ContextMenu("Reload Words from JSON")]
    public void ReloadWords()
    {
        LoadWordsFromJSON();
        LoadKoreanWordsFromJSON();
    }

    /// <summary>
    /// 현재 로드된 단어 통계 출력 (개발/테스트용)
    /// </summary>
    [ContextMenu("Show Word Statistics")]
    public void ShowWordStatistics()
    {
        Debug.Log($"=== 영어 단어 통계 ===");
        Debug.Log($"전체: {allWords.Count}개");
        Debug.Log($"Easy: {easyWords.Count}개");
        Debug.Log($"Medium: {mediumWords.Count}개");
        Debug.Log($"Hard: {hardWords.Count}개");

        if (easyWords.Count > 0)
            Debug.Log($"Easy 예시: {string.Join(", ", easyWords.GetRange(0, Mathf.Min(5, easyWords.Count)))}");
        if (mediumWords.Count > 0)
            Debug.Log($"Medium 예시: {string.Join(", ", mediumWords.GetRange(0, Mathf.Min(5, mediumWords.Count)))}");
        if (hardWords.Count > 0)
            Debug.Log($"Hard 예시: {string.Join(", ", hardWords.GetRange(0, Mathf.Min(5, hardWords.Count)))}");

        Debug.Log($"=== 한국어 단어 통계 ===");
        Debug.Log($"전체: {allKoreanWords.Count}개");
        Debug.Log($"Easy: {easyKoreanWords.Count}개");
        Debug.Log($"Medium: {mediumKoreanWords.Count}개");
        Debug.Log($"Hard: {hardKoreanWords.Count}개");

        if (easyKoreanWords.Count > 0)
            Debug.Log(
                $"Easy 예시: {string.Join(", ", easyKoreanWords.GetRange(0, Mathf.Min(5, easyKoreanWords.Count)))}");
        if (mediumKoreanWords.Count > 0)
            Debug.Log(
                $"Medium 예시: {string.Join(", ", mediumKoreanWords.GetRange(0, Mathf.Min(5, mediumKoreanWords.Count)))}");
        if (hardKoreanWords.Count > 0)
            Debug.Log(
                $"Hard 예시: {string.Join(", ", hardKoreanWords.GetRange(0, Mathf.Min(5, hardKoreanWords.Count)))}");
    }
}
