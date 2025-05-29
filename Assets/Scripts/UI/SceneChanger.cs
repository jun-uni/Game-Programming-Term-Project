using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public static SceneChanger Instance;

    [Header("페이드 설정")] public Image fadeImage; // 전체 화면을 덮는 검정 이미지
    public float fadeDuration = 0.5f;

    // 씬 전환 중 입력 차단을 위한 변수
    public static bool IsTransitioning { get; private set; } = false;

    private void Awake()
    {
        // 싱글턴 패턴
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

    /// <summary>
    /// 외부에서 호출하는 씬 전환 함수
    /// </summary>
    public void LoadScene(string sceneName)
    {
        // 이미 전환 중이라면 무시
        if (IsTransitioning) return;

        StartCoroutine(FadeAndSwitchScene(sceneName));
    }

    private IEnumerator FadeAndSwitchScene(string sceneName)
    {
        // 씬 전환 시작 - 입력 차단
        IsTransitioning = true;
        SetInputBlocking(true);

        yield return StartCoroutine(Fade(1)); // 페이드 아웃
        yield return SceneManager.LoadSceneAsync(sceneName);

        // AudioManager.Instance.PlayBGM();

        // 씬 로드 후 fadeImage가 확실히 검은색(알파 1)로 설정되어 있는지 확인
        fadeImage.color = new Color(0, 0, 0, 1);

        yield return StartCoroutine(Fade(0)); // 페이드 인

        // 씬 전환 완료 - 입력 차단 해제
        SetInputBlocking(false);
        IsTransitioning = false;
    }

    /// <summary>
    /// 입력 차단/해제 설정
    /// </summary>
    private void SetInputBlocking(bool blockInput)
    {
        if (fadeImage != null)
        {
            // Raycast Target 설정으로 UI 클릭 차단
            fadeImage.raycastTarget = blockInput;

            // 디버그용 - 입력 차단 상태 확인
            Debug.Log($"SceneChanger: 입력 차단 = {blockInput}");
        }
    }

    /// <summary>
    /// 알파값 0 → 1 또는 1 → 0 페이드 효과
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);
    }
}
