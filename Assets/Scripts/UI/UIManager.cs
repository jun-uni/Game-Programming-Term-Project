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

    [Header("타이머")] [SerializeField] private TextMeshProUGUI timerText;

    [Header("실시간 점수")] [SerializeField] private TextMeshProUGUI currentScoreText;

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
        // 타이머 업데이트 이벤트 구독
        GameManager.OnGameTimeUpdated += UpdateTimerUI;
        GameManager.OnGameVictory += ShowVictoryUI;
        GameManager.OnGameDefeat += ShowGameOverUI;
        GameManager.OnScoreUpdated += UpdateScoreUI;
    }

    private void OnDestroy()
    {
        GameManager.OnGameTimeUpdated -= UpdateTimerUI;
        GameManager.OnGameDefeat -= ShowVictoryUI;
        GameManager.OnGameVictory -= ShowGameOverUI;
        GameManager.OnScoreUpdated -= UpdateScoreUI;
    }

    private void UpdateTimerUI(float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    #region 게임 오버

    public void ShowGameOverUI()
    {
        StartCoroutine(ShowGameOverUIAfterDelay(2.0f));
    }

    public IEnumerator ShowGameOverUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameManager.Instance.StopGame();
        GameOverContainer.gameObject.SetActive(true);
        scoreText.text = "Score : " + GameManager.Instance.GetCurrentScore();
        highScoreText.text = "High Score : " + GameManager.Instance.GetHighScore();
    }

    public void OnRestartButtonClicked()
    {
        GameOverContainer.gameObject.SetActive(false);

        SceneChanger.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHomeButtonClicked()
    {
        GameOverContainer.gameObject.SetActive(false);

        SceneChanger.Instance.LoadScene("FirstScene");
    }

    #endregion

    #region 승리 화면

    public void ShowVictoryUI()
    {
    }

    #endregion

    #region 점수

    public void UpdateScoreUI(int score)
    {
        currentScoreText.text = score.ToString();
    }

    #endregion
}
