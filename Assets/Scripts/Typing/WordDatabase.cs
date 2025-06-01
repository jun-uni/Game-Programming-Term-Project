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

    [Header("단어 리스트")] public List<string> easyWords = new();
    public List<string> mediumWords = new();
    public List<string> hardWords = new();
    public List<string> allWords = new(); // 전체 단어 목록

    [Header("난이도 분류 설정")] [Tooltip("Easy 난이도 최대 글자 수")]
    public int easyMaxLength = 4;

    [Tooltip("Medium 난이도 최대 글자 수")] public int mediumMaxLength = 6;

    [Header("난이도 설정")] public float easyWordChance = 0.5f;
    public float mediumWordChance = 0.3f;
    public float hardWordChance = 0.2f;

    [Header("JSON 파일 설정")] [Tooltip("Resources 폴더 내 JSON 파일명 (확장자 제외)")]
    public string jsonFileName = "english word list";

    [Header("디버그")] public bool showLoadingInfo = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadWordsFromJSON();
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
                Debug.LogError($"JSON 파일을 찾을 수 없습니다: {jsonFileName}.json");
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
                Debug.LogError("JSON 파싱에 실패했습니다.");
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
                Debug.Log($"단어 로드 완료!");
                Debug.Log($"전체: {allWords.Count}개");
                Debug.Log($"Easy ({easyMaxLength}글자 이하): {easyWords.Count}개");
                Debug.Log($"Medium ({easyMaxLength + 1}-{mediumMaxLength}글자): {mediumWords.Count}개");
                Debug.Log($"Hard ({mediumMaxLength + 1}글자 이상): {hardWords.Count}개");
            }

            // 비어있는 카테고리 체크
            if (easyWords.Count == 0 || mediumWords.Count == 0 || hardWords.Count == 0)
                Debug.LogWarning("일부 난이도 카테고리가 비어있습니다. 전체 목록에서 랜덤 선택으로 대체합니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON 로드 중 오류 발생: {e.Message}");
            LoadDefaultWords();
        }
    }

    private void LoadDefaultWords()
    {
        Debug.Log("기본 단어 목록을 사용합니다.");

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

    private string GetRandomWordFromCategory(List<string> categoryWords, string categoryName)
    {
        if (categoryWords.Count == 0)
        {
            Debug.LogWarning($"{categoryName} 카테고리가 비어있습니다. 전체 목록에서 선택합니다.");
            if (allWords.Count > 0)
                return allWords[Random.Range(0, allWords.Count)];
            else
                return "word"; // 최후의 수단
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
    }

    /// <summary>
    /// 현재 로드된 단어 통계 출력 (개발/테스트용)
    /// </summary>
    [ContextMenu("Show Word Statistics")]
    public void ShowWordStatistics()
    {
        Debug.Log($"=== 단어 통계 ===");
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
    }
}
