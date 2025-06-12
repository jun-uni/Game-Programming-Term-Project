using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TypingManager : MonoBehaviour
{
    public static TypingManager Instance;

    [Header("활성 타겟들")] public List<WordTarget> activeTargets = new();

    [Header("입력 설정")] public bool allowBackspace = true;

    [Header("전역 오타 설정")] public float typoEffectDuration = 0.5f;

    [Header("오타 효과")] public bool isGlobalTypo = false;
    private float typoTimer = 0f;

    [Header("디버그")] public bool showDebugInfo = true;

    [Header("한국어 지원")] public bool isKoreanMode = false;

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

    private void Start()
    {
        // 게임 언어에 따라 한국어 모드 설정
        CheckLanguageMode();

        // 언어 변경 이벤트 구독
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(SystemLanguage newLanguage)
    {
        CheckLanguageMode();
    }

    private void CheckLanguageMode()
    {
        // LocalizationManager가 초기화되었는지 확인
        if (LocalizationManager.IsInitialized)
        {
            isKoreanMode = LocalizationManager.GameLanguage == SystemLanguage.Korean;

            if (showDebugInfo)
                Debug.Log($"타이핑 모드 변경: {(isKoreanMode ? "한국어" : "영어")}");
        }
        else
        {
            // 초기화되지 않았다면 기본값 사용
            isKoreanMode = false;
        }
    }

    private void Update()
    {
        HandleInput();
        HandleTypoEffect();
    }

    private void HandleInput()
    {
        // 게임이 활성 상태가 아니면 입력 무시
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            return;

        // 백스페이스 처리
        if (Input.inputString.Contains("\b") && allowBackspace) HandleBackspace();

        if (isKoreanMode)
            // 한국어 모드: KeyCode로 쌍자음 지원
            HandleKoreanKeyInput();
        else
            // 영어 모드: 기존 방식
            foreach (char c in Input.inputString)
                if (c != '\b' && IsValidInputCharacter(c))
                    ProcessInputCharacter(c);
    }

    private bool IsValidInputCharacter(char c)
    {
        if (isKoreanMode)
            // 한국어 모드: 영어 알파벳만 허용 (한글 자모로 변환됨)
            return char.IsLetter(c) && c >= 'A' && c <= 'z';
        else
            // 영어 모드: 영문자만 허용
            return char.IsLetter(c);
    }

    /// <summary>
    /// 한국어 모드에서 KeyCode로 쌍자음 지원하는 입력 처리
    /// </summary>
    private void HandleKoreanKeyInput()
    {
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 한글 자모 키 매핑 체크
        string koreanJamo = GetKoreanJamoFromKeyInput(isShiftPressed);

        if (!string.IsNullOrEmpty(koreanJamo)) ProcessKoreanJamo(koreanJamo);
    }

    /// <summary>
    /// 현재 프레임에서 눌린 키와 Shift 상태로 한글 자모 반환
    /// </summary>
    private string GetKoreanJamoFromKeyInput(bool isShiftPressed)
    {
        // 자음 (초성/종성)
        if (Input.GetKeyDown(KeyCode.Q)) return isShiftPressed ? "ㅃ" : "ㅂ";
        if (Input.GetKeyDown(KeyCode.W)) return isShiftPressed ? "ㅉ" : "ㅈ";
        if (Input.GetKeyDown(KeyCode.E)) return isShiftPressed ? "ㄸ" : "ㄷ";
        if (Input.GetKeyDown(KeyCode.R)) return isShiftPressed ? "ㄲ" : "ㄱ";
        if (Input.GetKeyDown(KeyCode.T)) return isShiftPressed ? "ㅆ" : "ㅅ";
        if (Input.GetKeyDown(KeyCode.A)) return "ㅁ";
        if (Input.GetKeyDown(KeyCode.S)) return "ㄴ";
        if (Input.GetKeyDown(KeyCode.D)) return "ㅇ";
        if (Input.GetKeyDown(KeyCode.F)) return "ㄹ";
        if (Input.GetKeyDown(KeyCode.G)) return "ㅎ";
        if (Input.GetKeyDown(KeyCode.Z)) return "ㅋ";
        if (Input.GetKeyDown(KeyCode.X)) return "ㅌ";
        if (Input.GetKeyDown(KeyCode.C)) return "ㅊ";
        if (Input.GetKeyDown(KeyCode.V)) return "ㅍ";

        // 모음 (중성)
        if (Input.GetKeyDown(KeyCode.Y)) return "ㅛ";
        if (Input.GetKeyDown(KeyCode.U)) return "ㅕ";
        if (Input.GetKeyDown(KeyCode.I)) return "ㅑ";
        if (Input.GetKeyDown(KeyCode.O)) return isShiftPressed ? "ㅒ" : "ㅐ";
        if (Input.GetKeyDown(KeyCode.P)) return isShiftPressed ? "ㅖ" : "ㅔ";
        if (Input.GetKeyDown(KeyCode.H)) return "ㅗ";
        if (Input.GetKeyDown(KeyCode.J)) return "ㅓ";
        if (Input.GetKeyDown(KeyCode.K)) return "ㅏ";
        if (Input.GetKeyDown(KeyCode.L)) return "ㅣ";
        if (Input.GetKeyDown(KeyCode.B)) return "ㅠ";
        if (Input.GetKeyDown(KeyCode.N)) return "ㅜ";
        if (Input.GetKeyDown(KeyCode.M)) return "ㅡ";

        return null; // 해당하는 키가 없음
    }

    /// <summary>
    /// 영어 모드 입력 문자 처리
    /// </summary>
    private void ProcessInputCharacter(char inputChar)
    {
        // 한글 입력 감지 (게임 언어가 영어일 때)
        if (LocalizationManager.GameLanguage == SystemLanguage.English && IsKoreanCharacter(inputChar))
        {
            // 한/영 키 경고 표시
            if (UIManager.Instance != null) UIManager.Instance.ShowKoreanEnglishKeyWarning();

            if (showDebugInfo)
                Debug.Log($"한글 입력 감지됨: '{inputChar}' - 경고 표시");

            return; // 한글 입력은 처리하지 않음
        }

        // 영어 모드: 기존 방식
        ProcessSingleCharacter(char.ToLower(inputChar));
    }

    /// <summary>
    /// 한글 문자인지 확인
    /// </summary>
    private bool IsKoreanCharacter(char c)
    {
        // 한글 유니코드 범위 확인
        return (c >= 0xAC00 && c <= 0xD7A3) || // 완성된 한글
               (c >= 0x1100 && c <= 0x11FF) || // 한글 자모 (초성)
               (c >= 0x3130 && c <= 0x318F) || // 한글 호환 자모
               (c >= 0xA960 && c <= 0xA97F) || // 한글 자모 확장-A
               (c >= 0xD7B0 && c <= 0xD7FF); // 한글 자모 확장-B
    }

    private void ProcessKoreanJamo(string jamoInput)
    {
        if (showDebugInfo)
            Debug.Log($"한글 자모 입력: '{jamoInput}'");

        // 모든 타겟의 이전 진행도 저장
        Dictionary<WordTarget, int> previousProgress = new();
        foreach (WordTarget target in activeTargets)
            previousProgress[target] = target.GetCurrentProgress();

        // 각 타겟이 이 자모를 처리할 수 있는지 확인
        Dictionary<WordTarget, int> individualTypoTargets = new();
        List<WordTarget> acceptingTargets = new();

        foreach (WordTarget target in activeTargets)
        {
            bool canAccept = target.CanAcceptNextJamo(jamoInput);

            if (canAccept)
            {
                // 이 타겟은 자모를 받을 수 있음
                target.AcceptJamo(jamoInput);
                acceptingTargets.Add(target);

                if (showDebugInfo)
                    Debug.Log(
                        $"{target.GetDisplayWord()}: '{jamoInput}' 수용, 진행도 {previousProgress[target]} → {target.GetCurrentProgress()}");
            }
            else
            {
                // 이 타겟은 자모를 받을 수 없음 - 개별 오타
                int prevProgress = previousProgress[target];
                if (prevProgress > 0) // 진행 중이었다면 개별 오타
                {
                    individualTypoTargets[target] = prevProgress;
                    target.TriggerIndividualTypo();

                    if (showDebugInfo)
                        Debug.Log($"{target.GetDisplayWord()}: '{jamoInput}' 개별 오타, 진행도 {prevProgress} → 0");
                }
            }
        }

        // 전역 오타 체크 (기존과 동일한 로직)
        if (CheckGlobalTypo(individualTypoTargets, acceptingTargets))
        {
            TriggerGlobalTypo(individualTypoTargets.Keys);
            return;
        }

        // 완성된 단어 체크
        CheckCompletedWords();
    }

    private void HandleBackspace()
    {
        // 모든 타겟에서 마지막 글자/자모 제거
        foreach (WordTarget target in activeTargets)
            target.HandleBackspace();

        if (showDebugInfo)
            Debug.Log("백스페이스 처리됨");
    }

    private void ProcessSingleCharacter(char inputChar)
    {
        if (showDebugInfo)
            Debug.Log($"새 글자 입력: '{inputChar}'");

        // 모든 타겟의 이전 진행도 저장
        Dictionary<WordTarget, int> previousProgress = new();
        foreach (WordTarget target in activeTargets)
            previousProgress[target] = target.GetCurrentProgress();

        // 각 타겟이 이 글자를 처리할 수 있는지 확인
        Dictionary<WordTarget, int> individualTypoTargets = new();
        List<WordTarget> acceptingTargets = new();

        foreach (WordTarget target in activeTargets)
        {
            bool canAccept = target.CanAcceptNextChar(inputChar);

            if (canAccept)
            {
                // 이 타겟은 글자를 받을 수 있음
                target.AcceptCharacter(inputChar);
                acceptingTargets.Add(target);

                if (showDebugInfo)
                    Debug.Log(
                        $"{target.Word}: '{inputChar}' 수용, 진행도 {previousProgress[target]} → {target.GetCurrentProgress()}");
            }
            else
            {
                // 이 타겟은 글자를 받을 수 없음 - 개별 오타
                int prevProgress = previousProgress[target];
                if (prevProgress > 0) // 진행 중이었다면 개별 오타
                {
                    individualTypoTargets[target] = prevProgress;
                    target.TriggerIndividualTypo();

                    if (showDebugInfo)
                        Debug.Log($"{target.Word}: '{inputChar}' 개별 오타, 진행도 {prevProgress} → 0");
                }
            }
        }

        // 전역 오타 체크 (6번 규칙)
        if (CheckGlobalTypo(individualTypoTargets, acceptingTargets))
        {
            TriggerGlobalTypo(individualTypoTargets.Keys);
            return;
        }

        // 완성된 단어 체크
        CheckCompletedWords();
    }

    private bool CheckGlobalTypo(Dictionary<WordTarget, int> individualTypoTargets, List<WordTarget> acceptingTargets)
    {
        // 개별 오타가 없으면 전역 오타 아님
        if (individualTypoTargets.Count == 0)
            return false;

        // 진행도가 2 이상인 개별 오타만 고려 (첫 글자 오타는 단순 선택 문제)
        List<KeyValuePair<WordTarget, int>> significantTypoTargets =
            individualTypoTargets.Where(kvp => kvp.Value >= 2).ToList();

        if (significantTypoTargets.Count == 0)
        {
            if (showDebugInfo)
                Debug.Log("전역 오타 회피: 첫 글자 선택 오타만 발생함");
            return false; // 첫 글자 오타만 있다면 전역 오타 아님
        }

        // 6번 규칙: 개별 오타가 발생한 타겟들의 이전 진행도 중 최대값
        int maxTypoProgress = individualTypoTargets.Values.Max();

        // 개별 오타가 발생하지 않은 타겟들 중에서 더 진행된 것이 있는지 확인
        foreach (WordTarget target in acceptingTargets)
            if (target.GetCurrentProgress() > maxTypoProgress)
            {
                if (showDebugInfo)
                    Debug.Log(
                        $"전역 오타 회피: {target.GetDisplayWord()}가 더 진행됨 (진행도: {target.GetCurrentProgress()}, 오타 최대 진행도: {maxTypoProgress})");
                return false;
            }

        if (showDebugInfo)
            Debug.Log($"전역 오타 발생: 6번 규칙 적용 (개별 오타 최대 진행도: {maxTypoProgress})");
        return true; // 전역 오타
    }

    private void TriggerGlobalTypo(IEnumerable<WordTarget> typoTargets)
    {
        Debug.LogError("전역 오타 발생!");

        isGlobalTypo = true;
        typoTimer = typoEffectDuration;

        // GameManager에 전역 오타 발생 알림
        if (GameManager.Instance != null)
            GameManager.Instance.AddGlobalTypo();

        // 개별 오타가 발생한 타겟들에게 시각적 효과 적용
        foreach (WordTarget target in typoTargets) target.ShowTypoEffect();

        // 개별 오타가 발생한 타겟들은 이미 TriggerIndividualTypo()로 리셋됨
        // 정상 진행 중인 타겟들은 그대로 유지

        // 여기에 전역 오타 시각/음향 효과 추가 가능
    }

    private void HandleTypoEffect()
    {
        if (isGlobalTypo)
        {
            typoTimer -= Time.deltaTime;
            if (typoTimer <= 0f)
            {
                isGlobalTypo = false;
                if (showDebugInfo)
                    Debug.Log("전역 오타 효과 종료");
            }
        }
    }

    private void CheckCompletedWords()
    {
        List<WordTarget> completedTargets = new();

        foreach (WordTarget target in activeTargets)
            if (target.IsWordCompleted())
                completedTargets.Add(target);

        foreach (WordTarget completedTarget in completedTargets)
            CompleteWord(completedTarget);
    }

    private void CompleteWord(WordTarget target)
    {
        if (showDebugInfo)
            Debug.Log($"단어 완성: {target.GetDisplayWord()}");

        target.OnWordCompleted();
    }

    public void RegisterTarget(WordTarget target)
    {
        if (!activeTargets.Contains(target))
            activeTargets.Add(target);
    }

    public void UnregisterTarget(WordTarget target)
    {
        if (activeTargets.Contains(target))
            activeTargets.Remove(target);
    }

    public bool IsGlobalTypo()
    {
        return isGlobalTypo;
    }

    public bool IsKoreanMode()
    {
        return isKoreanMode;
    }

    /// <summary>
    /// 런타임에서 한국어 모드 강제 설정 (테스트용)
    /// </summary>
    [ContextMenu("Toggle Korean Mode")]
    public void ToggleKoreanMode()
    {
        isKoreanMode = !isKoreanMode;
        Debug.Log($"한국어 모드 토글: {isKoreanMode}");
    }
}
