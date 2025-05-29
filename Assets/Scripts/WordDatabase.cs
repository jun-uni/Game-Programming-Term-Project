using System.Collections.Generic;
using UnityEngine;

public class WordDatabase : MonoBehaviour
{
    public static WordDatabase Instance;

    [Header("단어 리스트")] public List<string> easyWords = new()
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

    public List<string> mediumWords = new()
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

    public List<string> hardWords = new()
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

    [Header("난이도 설정")] public float easyWordChance = 0.5f;
    public float mediumWordChance = 0.3f;
    public float hardWordChance = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetRandomWord()
    {
        float random = Random.Range(0f, 1f);

        if (random < easyWordChance)
            return easyWords[Random.Range(0, easyWords.Count)];
        else if (random < easyWordChance + mediumWordChance)
            return mediumWords[Random.Range(0, mediumWords.Count)];
        else
            return hardWords[Random.Range(0, hardWords.Count)];
    }

    public string GetWordByDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            case 0: return easyWords[Random.Range(0, easyWords.Count)];
            case 1: return mediumWords[Random.Range(0, mediumWords.Count)];
            case 2: return hardWords[Random.Range(0, hardWords.Count)];
            default: return GetRandomWord();
        }
    }
}
