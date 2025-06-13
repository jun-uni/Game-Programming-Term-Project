using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("버튼들")] [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;

    [SerializeField] private GameObject difficultySelectPanel;

    [Header("전체화면 알림")] [SerializeField] private GameObject fullscreenPanel;
    [SerializeField] private Button dontShowAgainButton;
    [SerializeField] private Button closeButton;

    private const string FULLSCREEN_DONT_SHOW_KEY = "FullscreenDontShow";

    private void Start()
    {
        // 게임 시작 시 전체화면 알림 표시 여부 확인
        CheckAndShowFullscreenNotification();
    }

    /// <summary>
    /// 전체화면 알림을 표시할지 확인하고 표시
    /// </summary>
    private void CheckAndShowFullscreenNotification()
    {
        // "다시 보지 않기"를 선택했는지 확인 (기본값: 0 = 보여줌, 1 = 안보여줌)
        bool dontShow = PlayerPrefs.GetInt(FULLSCREEN_DONT_SHOW_KEY, 0) == 1;

        if (!dontShow && fullscreenPanel != null)
            fullscreenPanel.SetActive(true);
        else if (fullscreenPanel != null) fullscreenPanel.SetActive(false);
    }

    /// <summary>
    /// "다시 보지 않기" 버튼 클릭 시
    /// </summary>
    public void OnDontShowAgainClicked()
    {
        // PlayerPrefs에 다시 보지 않기 설정 저장
        PlayerPrefs.SetInt(FULLSCREEN_DONT_SHOW_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("전체화면 알림을 다시 표시하지 않습니다.");

        // 패널 닫기
        if (fullscreenPanel != null)
            fullscreenPanel.SetActive(false);
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 (일반적으로 닫기)
    /// </summary>
    public void OnCloseFullscreenPanel()
    {
        if (fullscreenPanel != null)
            fullscreenPanel.SetActive(false);
    }

    /// <summary>
    /// 전체화면 알림 설정 초기화 (개발/테스트용)
    /// </summary>
    [ContextMenu("Reset Fullscreen Notification Setting")]
    public void ResetFullscreenNotificationSetting()
    {
        PlayerPrefs.DeleteKey(FULLSCREEN_DONT_SHOW_KEY);
        PlayerPrefs.Save();
        Debug.Log("전체화면 알림 설정이 초기화되었습니다.");
    }

    /// <summary>
    /// 시작 버튼 클릭 시 - 이제 난이도 선택 UI를 보여주어야 함
    /// (실제 난이도 선택 UI는 사용자가 구현)
    /// </summary>
    public void OnStartButtonClicked()
    {
        // 여기서 난이도 선택 UI를 활성화하는 코드를 추가
        ShowDifficultySelectionUI();
    }

    public void ShowDifficultySelectionUI()
    {
        difficultySelectPanel.SetActive(true);
    }

    /// <summary>
    /// 쉬운 난이도로 게임 시작
    /// </summary>
    public void StartGameWithEasyDifficulty()
    {
        SetDifficultyAndStartGame(DifficultyLevel.Easy);
    }

    /// <summary>
    /// 보통 난이도로 게임 시작
    /// </summary>
    public void StartGameWithNormalDifficulty()
    {
        SetDifficultyAndStartGame(DifficultyLevel.Normal);
    }

    /// <summary>
    /// 어려운 난이도로 게임 시작
    /// </summary>
    public void StartGameWithHardDifficulty()
    {
        SetDifficultyAndStartGame(DifficultyLevel.Hard);
    }

    /// <summary>
    /// 난이도 설정 후 게임 시작하는 공통 메서드
    /// </summary>
    /// <param name="difficulty">설정할 난이도</param>
    private void SetDifficultyAndStartGame(DifficultyLevel difficulty)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 없어서 게임을 시작할 수 없습니다!");
            return;
        }

        // 난이도 설정
        GameManager.Instance.SetDifficulty(difficulty);

        Debug.Log($"난이도 {difficulty}로 게임 시작");

        // 게임 씬으로 이동 후 게임 시작
        SceneChanger.Instance.LoadScene("MainScene");
        GameManager.Instance.StartGame();
    }

    /// <summary>
    /// 난이도를 설정하는 메서드 (int 값으로 받는 버전 - UI에서 사용하기 편함)
    /// </summary>
    /// <param name="difficultyIndex">0: Easy, 1: Normal, 2: Hard</param>
    public void SetDifficultyAndStartGame(int difficultyIndex)
    {
        DifficultyLevel difficulty = (DifficultyLevel)difficultyIndex;
        SetDifficultyAndStartGame(difficulty);
    }
}
