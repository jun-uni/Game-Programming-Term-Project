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
    [SerializeField] private AudioClip gameOverSound;

    [Header("게임 승리 창")] [SerializeField] private GameObject GameVictoryContainer;
    [SerializeField] private TextMeshProUGUI victoryScoreText;
    [SerializeField] private TextMeshProUGUI victoryHighScoreText;
    [SerializeField] private TextMeshProUGUI victoryTypoCountText;
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private Button victoryHomeButton;
    [SerializeField] private AudioClip gameVictorySound;

    [Header("효과음 오디오 소스")] [SerializeField]
    private AudioSource audioSource;

    [Header("타이머")] [SerializeField] private TextMeshProUGUI timerText;

    [Header("실시간 점수")] [SerializeField] private TextMeshProUGUI currentScoreText;

    [Header("체력")] [SerializeField] private Image firstHealthImage;
    [SerializeField] private Image secondHealthImage;
    [SerializeField] private Image thirdHealthImage;

    [Header("스태미너")] [SerializeField] private Image staminaBarBackground; // 스태미너 바 배경
    [SerializeField] private Image staminaBarFill; // 스태미너 바 Fill

    [Header("알림 시스템")] [SerializeField] private GameObject koreanEnglishKeyWarning;
    [SerializeField] private GameObject buffDescription;
    [SerializeField] private float notificationDuration = 3.0f;

    // 알림 코루틴 관리용
    private Coroutine koreanEnglishWarningCoroutine = null;
    private Coroutine buffDescriptionCoroutine = null;

    [Header("위험 상태 효과")] [SerializeField] private RawImage lowHealthVignette; // 체력 낮을 때 화면 가장자리 효과
    [SerializeField] private float vignetteAnimationSpeed = 2f; // 비네팅 애니메이션 속도
    [SerializeField] private float vignetteMaxAlpha = 0.4f; // 비네팅 최대 투명도 (0~1)
    [SerializeField] private Color vignetteColor = new(1f, 0.2f, 0.2f, 1f); // 비네팅 색상 (빨간색)
    [SerializeField] private bool enableVignettePulse = true; // 맥동 효과 활성화

    [Header("게임 UI 컨테이너")] [SerializeField]
    private GameObject gameUIContainer; // 게임 중에만 보일 UI들을 담을 컨테이너

    [Header("게임 씬 설정")] [SerializeField] private string[] gameSceneNames = { "MainScene" }; // 게임 씬 이름들

    // 비네팅 효과 관련 변수들
    private bool isLowHealth = false;
    private Coroutine vignetteCoroutine = null;

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

        // 비네팅 효과 초기화
        InitializeVignette();

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

    #region 공통 알림 시스템

    /// <summary>
    /// 지정된 시간동안 UI 요소를 활성화하는 공통 함수
    /// </summary>
    /// <param name="uiElement">활성화할 UI 요소</param>
    /// <param name="message">표시할 메시지</param>
    /// <param name="duration">표시 시간</param>
    /// <param name="currentCoroutine">현재 실행 중인 코루틴 참조</param>
    /// <returns>새로운 코루틴</returns>
    private Coroutine ShowTemporaryNotification(GameObject uiElement, string message, float duration,
        Coroutine currentCoroutine)
    {
        // 게임 씬이 아니면 알림 표시 안함
        if (!IsGameScene() || uiElement == null)
            return null;

        // 기존 코루틴이 있으면 중단
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        // 새로운 알림 표시 시작
        return StartCoroutine(ShowNotificationCoroutine(uiElement, message, duration));
    }

    /// <summary>
    /// 알림 표시 코루틴
    /// </summary>
    private IEnumerator ShowNotificationCoroutine(GameObject uiElement, string message, float duration)
    {
        // UI 요소 활성화
        uiElement.SetActive(true);

        // 메시지 설정
        TextMeshProUGUI textComponent = uiElement.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null) textComponent.text = message;

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // UI 요소 비활성화
        uiElement.SetActive(false);
    }

    #endregion

    #region 한/영키 경고

    /// <summary>
    /// 한/영 키 경고 표시
    /// </summary>
    public void ShowKoreanEnglishKeyWarning()
    {
        string message = "ui.game.koreankey.warning".Localize();
        koreanEnglishWarningCoroutine = ShowTemporaryNotification(
            koreanEnglishKeyWarning,
            message,
            notificationDuration,
            koreanEnglishWarningCoroutine
        );
    }

    /// <summary>
    /// 한/영 키 경고 즉시 숨기기
    /// </summary>
    public void HideKoreanEnglishKeyWarning()
    {
        if (koreanEnglishWarningCoroutine != null)
        {
            StopCoroutine(koreanEnglishWarningCoroutine);
            koreanEnglishWarningCoroutine = null;
        }

        if (koreanEnglishKeyWarning != null)
            koreanEnglishKeyWarning.SetActive(false);
    }

    #endregion

    #region 버프 설명

    /// <summary>
    /// 버프 설명 표시
    /// </summary>
    /// <param name="buffData">버프 데이터</param>
    public void ShowBuffDescription(BuffData buffData)
    {
        if (buffData == null) return;

        string message = $"{buffData.description.Localize()}";
        buffDescriptionCoroutine = ShowTemporaryNotification(
            buffDescription,
            message,
            notificationDuration,
            buffDescriptionCoroutine
        );
    }

    /// <summary>
    /// 버프 설명 즉시 숨기기
    /// </summary>
    public void HideBuffDescription()
    {
        if (buffDescriptionCoroutine != null)
        {
            StopCoroutine(buffDescriptionCoroutine);
            buffDescriptionCoroutine = null;
        }

        if (buffDescription != null)
            buffDescription.SetActive(false);
    }

    #endregion

    #region 비네팅 효과 시스템

    /// <summary>
    /// 비네팅 효과 초기화
    /// </summary>
    private void InitializeVignette()
    {
        if (lowHealthVignette != null)
        {
            // 초기 색상 설정
            Color initialColor = vignetteColor;
            initialColor.a = 0f; // 투명하게 시작
            lowHealthVignette.color = initialColor;

            // 비활성화 상태로 시작
            lowHealthVignette.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 위험 상태 비네팅 효과 활성화/비활성화
    /// </summary>
    /// <param name="enable">활성화 여부</param>
    private void SetLowHealthVignette(bool enable)
    {
        if (lowHealthVignette == null) return;

        // 이미 같은 상태면 무시
        if (isLowHealth == enable) return;

        isLowHealth = enable;

        // 기존 코루틴 정지
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
            vignetteCoroutine = null;
        }

        if (enable)
        {
            // 비네팅 효과 활성화
            lowHealthVignette.gameObject.SetActive(true);

            if (enableVignettePulse)
                // 맥동 효과 시작
                vignetteCoroutine = StartCoroutine(VignettePulseEffect());
            else
                // 단순 페이드 인
                vignetteCoroutine = StartCoroutine(FadeVignette(vignetteMaxAlpha));
        }
        else
        {
            // 비네팅 효과 비활성화 (페이드 아웃)
            vignetteCoroutine = StartCoroutine(FadeVignetteOut());
        }
    }

    /// <summary>
    /// 비네팅 맥동 효과 코루틴
    /// </summary>
    private IEnumerator VignettePulseEffect()
    {
        while (isLowHealth)
        {
            // 페이드 인
            yield return StartCoroutine(FadeVignette(vignetteMaxAlpha));

            if (!isLowHealth) break;

            // 페이드 아웃 (완전히 투명하지는 않게)
            yield return StartCoroutine(FadeVignette(vignetteMaxAlpha * 0.3f));

            if (!isLowHealth) break;
        }
    }

    /// <summary>
    /// 비네팅 페이드 효과 코루틴
    /// </summary>
    /// <param name="targetAlpha">목표 투명도</param>
    private IEnumerator FadeVignette(float targetAlpha)
    {
        if (lowHealthVignette == null) yield break;

        Color currentColor = lowHealthVignette.color;
        float startAlpha = currentColor.a;
        float elapsed = 0f;
        float duration = 1f / vignetteAnimationSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 부드러운 애니메이션을 위한 easing
            t = Mathf.SmoothStep(0f, 1f, t);

            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            Color newColor = vignetteColor;
            newColor.a = newAlpha;
            lowHealthVignette.color = newColor;

            yield return null;
        }

        // 최종 값 설정
        Color finalColor = vignetteColor;
        finalColor.a = targetAlpha;
        lowHealthVignette.color = finalColor;
    }

    /// <summary>
    /// 비네팅 페이드 아웃 후 비활성화
    /// </summary>
    private IEnumerator FadeVignetteOut()
    {
        // 페이드 아웃
        yield return StartCoroutine(FadeVignette(0f));

        // 완전히 투명해지면 비활성화
        if (lowHealthVignette != null) lowHealthVignette.gameObject.SetActive(false);
    }

    #endregion

    // 나머지 기존 코드들...

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

        // 게임오버 사운드 재생
        if (gameOverSound != null && audioSource != null)
            audioSource.PlayOneShot(gameOverSound);

        GameManager.Instance.StopGame();
        GameOverContainer.gameObject.SetActive(true);

        // 수정: GetFinalScore()를 사용하여 실제 저장되는 점수와 동일하게 표시
        int finalScore = GameManager.Instance.GetFinalScore(); // 오타 페널티 적용 후
        int highScore = GameManager.Instance.GetHighScore();

        scoreText.text = $"{"ui.gameover.score".Localize(finalScore)}";
        highScoreText.text = $"{"ui.gameover.highscore".Localize(highScore)}";
    }

    public void OnRestartButtonClicked()
    {
        GameOverContainer.gameObject.SetActive(false);

        // 비네팅 효과도 리셋
        SetLowHealthVignette(false);

        // 게임 상태 리셋 (재시작용)
        GameManager.Instance.RestartGame();

        // 씬 리로드
        SceneChanger.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeButtonClicked()
    {
        GameOverContainer.gameObject.SetActive(false);

        // 비네팅 효과도 리셋
        SetLowHealthVignette(false);

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
        // 비네팅 효과 리셋
        SetLowHealthVignette(false);

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
        PlayerController player = FindFirstObjectByType<PlayerController>();
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

        // 비네팅 효과는 게임 씬이 아닐 때 강제 비활성화
        if (!active)
            SetLowHealthVignette(false);
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

        // 승리 사운드 재생
        if (gameVictorySound != null && audioSource != null) audioSource.PlayOneShot(gameVictorySound);

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

        // 비네팅 효과도 리셋
        SetLowHealthVignette(false);

        // 게임 상태 리셋 (재시작용)
        GameManager.Instance.RestartGame();

        // 씬 리로드
        SceneChanger.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnVictoryHomeButtonClicked()
    {
        GameVictoryContainer.gameObject.SetActive(false);

        // 비네팅 효과도 리셋
        SetLowHealthVignette(false);

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
    /// 플레이어 체력 UI 업데이트 (비네팅 효과 포함)
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

        // 체력이 1일 때 비네팅 효과 활성화, 그 외에는 비활성화
        bool shouldShowVignette = currentHealth == 1;
        SetLowHealthVignette(shouldShowVignette);
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
