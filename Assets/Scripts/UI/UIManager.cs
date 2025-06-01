using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("게임 오버 창")] [SerializeField] private GameObject GameOverContainer;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button homeButton;

    [Header("게임 승리 창")] [SerializeField] private GameObject GameVictoryContainer;
    [SerializeField] private TextMeshProUGUI victoryScoreText;
    [SerializeField] private TextMeshProUGUI victoryHighScoreText;
    [SerializeField] private TextMeshProUGUI victoryTypoCountText;
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private Button victoryHomeButton;

    [Header("타이머")] [SerializeField] private TextMeshProUGUI timerText;

    [Header("실시간 점수")] [SerializeField] private TextMeshProUGUI currentScoreText;

    [Header("체력")] [SerializeField] private Image firstHealthImage;
    [SerializeField] private Image secondHealthImage;
    [SerializeField] private Image thirdHealthImage;

    [Header("스태미너")] [SerializeField] private Image staminaBarBackground; // 스태미너 바 배경
    [SerializeField] private Image staminaBarFill; // 스태미너 바 Fill

    [Header("게임 UI 컨테이너")] [SerializeField]
    private GameObject gameUIContainer; // 게임 중에만 보일 UI들을 담을 컨테이너

    [Header("게임 씬 설정")] [SerializeField] private string[] gameSceneNames = { "MainScene" }; // 게임 씬 이름들

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 게임 매니저 이벤트 구독
        GameManager.OnGameTimeUpdated += UpdateTimerUI;
        GameManager.OnGameVictory += ShowVictoryUI;
        GameManager.OnGameDefeat += ShowGameOverUI;
        GameManager.OnScoreUpdated += UpdateScoreUI;

        // 플레이어 체력 이벤트 구독
        PlayerController.OnPlayerHealthChanged += UpdateHealthUI;

        // 플레이어 스태미너 이벤트 구독
        PlayerController.OnPlayerStaminaChanged += UpdateStaminaUI;

        // 씬 변경 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 현재 씬에 따라 UI 상태 설정
        UpdateUIVisibility();
    }

    private void OnDestroy()
    {
        // 게임 매니저 이벤트 구독 해제
        GameManager.OnGameTimeUpdated -= UpdateTimerUI;
        GameManager.OnGameDefeat -= ShowVictoryUI;
        GameManager.OnGameVictory -= ShowGameOverUI;
        GameManager.OnScoreUpdated -= UpdateScoreUI;

        // 플레이어 체력 이벤트 구독 해제
        PlayerController.OnPlayerHealthChanged -= UpdateHealthUI;

        // 플레이어 스태미너 이벤트 구독 해제
        PlayerController.OnPlayerStaminaChanged -= UpdateStaminaUI;

        // 씬 변경 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void UpdateTimerUI(float remainingTime)
    {
        // 게임 씬이 아니면 타이머 UI 업데이트 건너뛰기
        if (!IsGameScene() || timerText == null)
            return;

        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    #region 게임 오버

    public void ShowGameOverUI()
    {
        // 게임 씬이 아니면 게임오버 UI 표시 안함
        if (!IsGameScene())
            return;

        StartCoroutine(ShowGameOverUIAfterDelay(2.0f));
    }

    public IEnumerator ShowGameOverUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 다시 한번 게임 씬인지 확인 (딜레이 중에 씬이 바뀔 수 있음)
        if (!IsGameScene())
            yield break;

        GameManager.Instance.StopGame();
        GameOverContainer.gameObject.SetActive(true);
        scoreText.text = $"{"ui.gameover.score".Localize(GameManager.Instance.GetCurrentScore())}";
        highScoreText.text = $"{"ui.gameover.highscore".Localize(GameManager.Instance.GetHighScore())}";
    }

    public void OnRestartButtonClicked()
    {
        GameOverContainer.gameObject.SetActive(false);

        // 게임 상태 리셋 (재시작용)
        GameManager.Instance.RestartGame();

        // 씬 리로드
        SceneChanger.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeButtonClicked()
    {
        GameOverContainer.gameObject.SetActive(false);

        // 게임 상태 완전 초기화 (홈으로 돌아가기)
        GameManager.Instance.ResetToHome();

        // 게임 UI 즉시 숨기기 (씬 전환 전에)
        if (gameUIContainer != null)
            gameUIContainer.SetActive(false);
        else
            SetGameUIActive(false);

        SceneChanger.Instance.LoadScene("FirstScene");
    }

    #endregion

    #region 씬 관리

    /// <summary>
    /// 씬 로드 시 호출
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateUIVisibility();
    }

    /// <summary>
    /// 현재 씬이 게임 씬인지 확인
    /// </summary>
    private bool IsGameScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        foreach (string gameSceneName in gameSceneNames)
            if (currentSceneName == gameSceneName)
                return true;

        return false;
    }

    /// <summary>
    /// 씬에 따라 UI 가시성 업데이트
    /// </summary>
    private void UpdateUIVisibility()
    {
        bool isGame = IsGameScene();

        // 게임 UI 컨테이너가 있으면 활성화/비활성화
        if (gameUIContainer != null)
            gameUIContainer.SetActive(isGame);
        else
            // 개별 UI 요소들 활성화/비활성화
            SetGameUIActive(isGame);

        // 게임오버 창은 항상 비활성화 (게임 씬이 아닐 때)
        if (!isGame && GameOverContainer != null) GameOverContainer.SetActive(false);

        // 승리 창도 항상 비활성화 (게임 씬이 아닐 때)
        if (!isGame && GameVictoryContainer != null) GameVictoryContainer.SetActive(false);

        // 게임 씬에 진입했을 때 UI를 현재 게임 상태로 업데이트
        if (isGame && GameManager.Instance != null) UpdateUIToCurrentGameState();
    }

    /// <summary>
    /// UI를 현재 게임 상태로 강제 업데이트
    /// </summary>
    private void UpdateUIToCurrentGameState()
    {
        // 게임이 활성 상태가 아니고 게임 씬이면 게임 시작
        if (!GameManager.Instance.IsGameActive() && IsGameScene()) GameManager.Instance.StartGame();

        // 점수 UI 업데이트
        if (currentScoreText != null) currentScoreText.text = GameManager.Instance.GetCurrentScore().ToString();

        // 타이머 UI 업데이트
        if (timerText != null)
        {
            float remainingTime = GameManager.Instance.GetRemainingTime();
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        // 플레이어가 존재하면 체력 & 스태미너 UI 업데이트
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            UpdateHealthUI(player.GetCurrentHitPoint(), 3); // maxHealth는 3으로 고정
            UpdateStaminaUI(player.GetCurrentStamina(), player.GetMaxStamina());
        }
        else
        {
            // 플레이어가 없으면 기본값으로 UI 설정
            UpdateHealthUI(3, 3);
            UpdateStaminaUI(100f, 100f);
        }
    }

    /// <summary>
    /// 개별 게임 UI 요소들 활성화/비활성화
    /// </summary>
    private void SetGameUIActive(bool active)
    {
        // 타이머
        if (timerText != null)
            timerText.gameObject.SetActive(active);

        // 점수
        if (currentScoreText != null)
            currentScoreText.gameObject.SetActive(active);

        // 체력 이미지들
        if (firstHealthImage != null)
            firstHealthImage.gameObject.SetActive(active);
        if (secondHealthImage != null)
            secondHealthImage.gameObject.SetActive(active);
        if (thirdHealthImage != null)
            thirdHealthImage.gameObject.SetActive(active);

        // 스태미너 UI
        if (staminaBarBackground != null)
            staminaBarBackground.gameObject.SetActive(active);
        if (staminaBarFill != null)
            staminaBarFill.gameObject.SetActive(active);
    }

    #endregion

    #region 승리 화면

    public void ShowVictoryUI()
    {
        // 게임 씬이 아니면 승리 UI 표시 안함
        if (!IsGameScene())
            return;

        StartCoroutine(ShowVictoryUIAfterDelay(0.5f));
    }

    public IEnumerator ShowVictoryUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 다시 한번 게임 씬인지 확인 (딜레이 중에 씬이 바뀔 수 있음)
        if (!IsGameScene())
            yield break;

        GameManager.Instance.StopGame();
        GameVictoryContainer.gameObject.SetActive(true);

        // 게임 통계 가져오기
        (int score, int kills, int typos) stats = GameManager.Instance.GetCurrentStats();
        int finalScore = GameManager.Instance.GetFinalScore(); // 최종 점수 (오타 페널티 적용)
        int highScore = GameManager.Instance.GetHighScore();

        // 승리 창에 정보 표시
        victoryScoreText.text = $"{"ui.victory.score".Localize(finalScore)}";
        victoryHighScoreText.text = $"{"ui.victory.highscore".Localize(highScore)}";
        victoryTypoCountText.text = $"{"ui.victory.typo".Localize(stats.typos)}";
    }

    public void OnVictoryRestartButtonClicked()
    {
        GameVictoryContainer.gameObject.SetActive(false);

        // 게임 상태 리셋 (재시작용)
        GameManager.Instance.RestartGame();

        // 씬 리로드
        SceneChanger.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnVictoryHomeButtonClicked()
    {
        GameVictoryContainer.gameObject.SetActive(false);

        // 게임 상태 완전 초기화 (홈으로 돌아가기)
        GameManager.Instance.ResetToHome();

        // 게임 UI 즉시 숨기기 (씬 전환 전에)
        if (gameUIContainer != null)
            gameUIContainer.SetActive(false);
        else
            SetGameUIActive(false);

        SceneChanger.Instance.LoadScene("FirstScene");
    }

    #endregion

    #region 점수

    public void UpdateScoreUI(int score)
    {
        // 게임 씬이 아니면 점수 UI 업데이트 건너뛰기
        if (!IsGameScene() || currentScoreText == null)
            return;

        currentScoreText.text = score.ToString();
    }

    #endregion

    #region 체력 & 스태미너

    /// <summary>
    /// 플레이어 체력 UI 업데이트
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    /// <param name="maxHealth">최대 체력</param>
    public void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        // 게임 씬이 아니면 체력 UI 업데이트 건너뛰기
        if (!IsGameScene())
            return;

        // 체력 이미지들을 배열로 관리
        Image[] healthImages = { firstHealthImage, secondHealthImage, thirdHealthImage };

        // 현재 체력에 따라 이미지 활성화/비활성화
        for (int i = 0; i < healthImages.Length; i++)
            if (healthImages[i] != null)
                // i번째 체력 칸이 현재 체력보다 작거나 같으면 활성화
                healthImages[i].enabled = i < currentHealth;
    }

    /// <summary>
    /// 플레이어 스태미너 UI 업데이트
    /// </summary>
    /// <param name="currentStamina">현재 스태미너</param>
    /// <param name="maxStamina">최대 스태미너</param>
    public void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        // 게임 씬이 아니면 스태미너 UI 업데이트 건너뛰기
        if (!IsGameScene() || staminaBarFill == null)
            return;

        // 스태미너 비율 계산 (0~1)
        float staminaRatio = maxStamina > 0 ? currentStamina / maxStamina : 0f;
        staminaRatio = Mathf.Clamp01(staminaRatio);

        // Fill Amount 설정
        staminaBarFill.fillAmount = staminaRatio;
    }

    #endregion
}
