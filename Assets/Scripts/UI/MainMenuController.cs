using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("버튼들")] [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;

    public void OnStartButtonClicked()
    {
        SceneChanger.Instance.LoadScene("MainScene");
    }
}
