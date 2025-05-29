using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LocalizationManager.Initialize(this);
            LoadVolumes();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetVolumes(GetVolumes());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += PlayBGMForScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= PlayBGMForScene;
    }

    #region 소리

    [Header("Audio Mixer")] [SerializeField]
    private AudioMixer audioMixer;

    [Header("Audio Sources")] [SerializeField]
    private AudioSource bgmSource;

    [SerializeField] private AudioSource sfxSource;

    [Header("BGM 클립들 (씬 이름 기준)")] [SerializeField]
    private AudioClip defaultBGM;

    [SerializeField] private List<SceneBGM> sceneBGMList;

    [Serializable]
    public class SceneBGM
    {
        public string sceneName;
        public AudioClip bgmClip;
    }

    private string currentSceneName = "";


    private void LoadVolumes()
    {
        // PlayerPrefs에서 볼륨 값 불러오기
        float volume = PlayerPrefs.GetFloat("Volume", 0.8f);

        // AudioMixer에 적용
        SetVolumes(volume);
    }

    public float GetVolumes()
    {
        return PlayerPrefs.GetFloat("Volume", 0.8f);
    }

    public void SetVolumes(float volume)
    {
        if (audioMixer != null)
        {
            float dB = volume > 0.0001f ? 20f * Mathf.Log10(volume) - 10f : -80f;
            audioMixer.SetFloat("MasterVolume", dB);
        }
    }

    public void SaveVolumeSettings(float newVolume)
    {
        PlayerPrefs.SetFloat("Volume", newVolume);
        PlayerPrefs.Save();

        // 믹서에도 적용
        SetVolumes(newVolume);
    }


    // 씬 전환 시 호출
    public void PlayBGMForScene(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("호출됨");
        string sceneName = scene.name;

        if (sceneName == currentSceneName) return; // 중복 재생 방지

        currentSceneName = sceneName;

        AudioClip clipToPlay = defaultBGM;

        foreach (SceneBGM entry in sceneBGMList)
            if (entry.sceneName == sceneName)
            {
                clipToPlay = entry.bgmClip;
                break;
            }

        if (bgmSource != null && clipToPlay != null)
        {
            bgmSource.clip = clipToPlay;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    #endregion

    #region 언어

    #endregion
}
