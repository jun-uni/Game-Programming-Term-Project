using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    #region 게임 상태 관리

    [Header("게임 설정")] [SerializeField] private float gameTimeLimit = 180f; // 3분 (초)
    [SerializeField] private int scorePerEnemyKill = 100; // 적 처치당 점수
    [SerializeField] private int typoScorePenalty = 10; // 전역 오타당 점수 차감

    [Header("게임 상태")] public bool isGameActive = false;
    private float currentGameTime = 0f;
    private int currentScore = 0;
    private int enemiesKilled = 0;
    private int globalTypoCount = 0;

    // 게임 상태 이벤트
    public static event Action<float> OnGameTimeUpdated; // UI 타이머 업데이트용
    public static event Action<int> OnScoreUpdated; // UI 점수 업데이트용
    public static event Action OnGameVictory; // 승리 이벤트
    public static event Action OnGameDefeat; // 패배 이벤트
    public static event Action<int, int, int> OnGameEnd; // 게임 종료 (최종점수, 적처치수, 오타수)

    #endregion

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

        // 플레이어 죽음 이벤트 구독
        PlayerController.OnPlayerDeath += HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        PlayerController.OnPlayerDeath -= HandlePlayerDeath;
    }

    private void Update()
    {
        if (isGameActive) UpdateGameTimer();
    }

    #region 게임 타이머 및 상태 관리

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        if (isGameActive) return;

        isGameActive = true;
        currentGameTime = 0f;
        currentScore = 0;
        enemiesKilled = 0;
        globalTypoCount = 0;

        // 시간 스케일 정상화
        Time.timeScale = 1.0f;

        Debug.Log("게임 시작!");

        // UI 즉시 업데이트 (게임 시작 시 초기값으로)
        OnGameTimeUpdated?.Invoke(GetRemainingTime());
        OnScoreUpdated?.Invoke(currentScore);

        // 약간의 딜레이 후 다시 한번 UI 업데이트 (확실하게)
        StartCoroutine(DelayedUIUpdate());
    }

    /// <summary>
    /// 딜레이 후 UI 업데이트 (게임 시작 시 확실한 초기화)
    /// </summary>
    private IEnumerator DelayedUIUpdate()
    {
        yield return new WaitForSeconds(0.1f);

        OnGameTimeUpdated?.Invoke(GetRemainingTime());
        OnScoreUpdated?.Invoke(currentScore);

        Debug.Log($"딜레이된 UI 업데이트 - 점수: {currentScore}, 시간: {GetRemainingTime()}");
    }

    public IEnumerator StopGameAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        StopGame();
    }

    public void StopGame()
    {
        isGameActive = false;
        Time.timeScale = 0.0f;
    }

    /// <summary>
    /// 게임 타이머 업데이트
    /// </summary>
    private void UpdateGameTimer()
    {
        currentGameTime += Time.deltaTime;

        // UI에 남은 시간 전달
        OnGameTimeUpdated?.Invoke(GetRemainingTime());

        // 시간 종료 체크
        if (currentGameTime >= gameTimeLimit) HandleGameVictory();
    }

    /// <summary>
    /// 남은 시간 반환
    /// </summary>
    public float GetRemainingTime()
    {
        return Mathf.Max(0f, gameTimeLimit - currentGameTime);
    }

    /// <summary>
    /// 현재 진행 시간 반환
    /// </summary>
    public float GetCurrentTime()
    {
        return currentGameTime;
    }

    /// <summary>
    /// 게임이 활성 상태인지 확인
    /// </summary>
    public bool IsGameActive()
    {
        return isGameActive;
    }

    #endregion

    #region 점수 시스템

    /// <summary>
    /// 적 처치시 점수 추가
    /// </summary>
    public void AddEnemyKillScore()
    {
        if (!isGameActive) return;

        enemiesKilled++;
        currentScore += scorePerEnemyKill;

        Debug.Log($"적 처치! 현재 점수: {currentScore} (처치 수: {enemiesKilled})");

        // UI 업데이트
        OnScoreUpdated?.Invoke(currentScore);
    }

    /// <summary>
    /// 전역 오타 발생시 카운터 증가
    /// </summary>
    public void AddGlobalTypo()
    {
        if (!isGameActive) return;

        globalTypoCount++;
        Debug.Log($"전역 오타 발생! 총 오타 수: {globalTypoCount}");
    }

    /// <summary>
    /// 최종 점수 계산
    /// </summary>
    private int CalculateFinalScore()
    {
        int penalty = globalTypoCount * typoScorePenalty;
        int finalScore = Mathf.Max(0, currentScore - penalty);

        Debug.Log($"최종 점수 계산: {currentScore} - ({globalTypoCount} × {typoScorePenalty}) = {finalScore}");

        return finalScore;
    }

    /// <summary>
    /// 현재 점수 반환
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// 최종 점수 반환 (오타 페널티 적용)
    /// </summary>
    public int GetFinalScore()
    {
        return CalculateFinalScore();
    }

    /// <summary>
    /// 현재 통계 반환
    /// </summary>
    public (int score, int kills, int typos) GetCurrentStats()
    {
        return (currentScore, enemiesKilled, globalTypoCount);
    }

    #endregion

    #region 게임 종료 처리

    /// <summary>
    /// 플레이어 죽음 처리
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (!isGameActive) return;

        Debug.Log("플레이어 사망으로 게임 패배!");
        HandleGameDefeat();
    }

    /// <summary>
    /// 게임 승리 처리
    /// </summary>
    private void HandleGameVictory()
    {
        if (!isGameActive) return;

        isGameActive = false;

        int finalScore = CalculateFinalScore();

        Debug.Log($"게임 승리! 최종 점수: {finalScore}");

        // 하이스코어 저장
        SaveHighScore(finalScore);

        // 이벤트 발생
        OnGameVictory?.Invoke();
        OnGameEnd?.Invoke(finalScore, enemiesKilled, globalTypoCount);
    }

    /// <summary>
    /// 게임 패배 처리
    /// </summary>
    private void HandleGameDefeat()
    {
        if (!isGameActive) return;

        isGameActive = false;

        int finalScore = CalculateFinalScore();

        Debug.Log($"게임 패배! 최종 점수: {finalScore}");

        // 하이스코어 저장
        SaveHighScore(finalScore);

        // 이벤트 발생
        OnGameDefeat?.Invoke();
        OnGameEnd?.Invoke(finalScore, enemiesKilled, globalTypoCount);
    }

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        isGameActive = false;
        currentGameTime = 0f;
        currentScore = 0;
        enemiesKilled = 0;
        globalTypoCount = 0;

        // 시간 스케일 복구
        Time.timeScale = 1.0f;

        Debug.Log("게임 상태 리셋 완료");

        // UI 업데이트 (즉시 실행)
        OnGameTimeUpdated?.Invoke(GetRemainingTime());
        OnScoreUpdated?.Invoke(currentScore);
    }

    /// <summary>
    /// 홈으로 돌아갈 때 호출 (완전 초기화)
    /// </summary>
    public void ResetToHome()
    {
        isGameActive = false;
        currentGameTime = 0f;
        currentScore = 0;
        enemiesKilled = 0;
        globalTypoCount = 0;

        // 시간 스케일 복구
        Time.timeScale = 1.0f;

        Debug.Log("홈으로 돌아가기 - 게임 상태 완전 초기화");

        // UI 즉시 업데이트 (홈에서는 UI가 안 보이지만 상태는 초기화)
        OnGameTimeUpdated?.Invoke(GetRemainingTime());
        OnScoreUpdated?.Invoke(currentScore);
    }

    #endregion

    #region 하이스코어 시스템

    /// <summary>
    /// 하이스코어 저장
    /// </summary>
    private void SaveHighScore(int score)
    {
        int currentHighScore = GetHighScore();

        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();

            Debug.Log($"새로운 최고 점수! {currentHighScore} → {score}");
        }
        else
        {
            Debug.Log($"최고 점수 갱신 실패. 현재 최고점: {currentHighScore}, 이번 점수: {score}");
        }
    }

    /// <summary>
    /// 하이스코어 불러오기
    /// </summary>
    public int GetHighScore()
    {
        return PlayerPrefs.GetInt("HighScore", 0);
    }

    /// <summary>
    /// 하이스코어 리셋
    /// </summary>
    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey("HighScore");
        PlayerPrefs.Save();
        Debug.Log("하이스코어 리셋됨");
    }

    #endregion

    #region 기존 소리 시스템 (수정 없음)

    private void OnEnable()
    {
        SceneManager.sceneLoaded += PlayBGMForScene;
        SceneManager.sceneLoaded += OnSceneLoadedForEvents; // 이벤트 재구독용
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= PlayBGMForScene;
        SceneManager.sceneLoaded -= OnSceneLoadedForEvents; // 이벤트 재구독용
    }

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

    /// <summary>
    /// 씬 로드 시 이벤트 재구독 처리
    /// </summary>
    private void OnSceneLoadedForEvents(Scene scene, LoadSceneMode mode)
    {
        // 플레이어 이벤트 재구독을 위한 코루틴 시작 (플레이어 생성 대기)
        StartCoroutine(ResubscribePlayerEvents());
    }

    /// <summary>
    /// 플레이어 이벤트 재구독 코루틴
    /// </summary>
    private IEnumerator ResubscribePlayerEvents()
    {
        // 플레이어가 생성될 때까지 대기
        PlayerController player = null;
        float waitTime = 0f;
        const float maxWaitTime = 5f; // 최대 5초 대기

        while (player == null && waitTime < maxWaitTime)
        {
            player = FindObjectOfType<PlayerController>();
            if (player == null)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }
        }

        if (player != null)
        {
            // 기존 이벤트 구독 해제 (중복 방지)
            PlayerController.OnPlayerDeath -= HandlePlayerDeath;

            // 새로운 이벤트 구독
            PlayerController.OnPlayerDeath += HandlePlayerDeath;

            Debug.Log("플레이어 이벤트 재구독 완료");
        }
        else
        {
            Debug.LogWarning("플레이어를 찾을 수 없어 이벤트 구독 실패");
        }
    }

    #endregion
}
