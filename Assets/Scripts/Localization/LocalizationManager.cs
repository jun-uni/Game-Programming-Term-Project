using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// WebGL 완전 호환 버전 (JsonUtility 문제 해결)
[DefaultExecutionOrder(-100)]
public static class LocalizationManager
{
    private static SystemLanguage systemLanguage;
    private static SystemLanguage gameLanguage;

    public static SystemLanguage GameLanguage => gameLanguage;

    public static SystemLanguage CurrentSystemLanguage
    {
        get => systemLanguage;
        set
        {
            if (systemLanguage != value)
            {
                systemLanguage = value;
                try
                {
                    OnLanguageChanged?.Invoke(systemLanguage);
                }
                catch
                {
                    // 이벤트 에러 무시
                }
            }
        }
    }

    public delegate void LanguageChangedHandler(SystemLanguage newLanguage);

    public static event LanguageChangedHandler OnLanguageChanged;
    public static event Action OnLanguageInitialized;

    // 로컬라이제이션 데이터
    private static Dictionary<string, Dictionary<string, string>> localizedTexts = new();

    // 초기화 완료 여부
    private static bool isInitialized = false;
    private static bool isInitializing = false;
    public static bool IsInitialized => isInitialized;

    // 지원하는 언어 목록
    public static List<SystemLanguage> SupportedLanguages { get; } = new()
    {
        SystemLanguage.English, SystemLanguage.Korean, SystemLanguage.Spanish, SystemLanguage.French
    };

    // 매니저 초기화 (WebGL 완전 호환)
    public static void Initialize(MonoBehaviour coroutineRunner)
    {
        if (isInitialized)
        {
            Debug.Log("LocalizationManager가 이미 초기화되었습니다.");
            return;
        }

        if (isInitializing)
        {
            Debug.LogWarning("LocalizationManager가 초기화 중입니다.");
            return;
        }

        isInitializing = true;
        Debug.Log("LocalizationManager 초기화 시작 (WebGL 안전 버전)");

        try
        {
            LoadSystemLanguage();
            LoadGameLanguage();

            // JSON 로딩을 피하고 하드코딩된 데이터 사용 (WebGL 안전성)
            LoadHardcodedData();
            CompleteInitialization();
        }
        catch
        {
            Debug.LogError("LocalizationManager 초기화 실패 - 기본 데이터 사용");
            isInitializing = false;
            LoadFallbackData();
            CompleteInitialization();
        }
    }

    // 하드코딩된 데이터 (WebGL에서 가장 안전)
    private static void LoadHardcodedData()
    {
        localizedTexts.Clear();

        // ui.settings.systemlanguage
        localizedTexts["ui.settings.systemlanguage"] = new Dictionary<string, string>
        {
            ["ko"] = "시스템 언어 :",
            ["eng"] = "System Language :",
            ["es"] = "Idioma del sistema:",
            ["fr"] = "Langue du système :"
        };

        // ui.settings.gamelanguage
        localizedTexts["ui.settings.gamelanguage"] = new Dictionary<string, string>
        {
            ["ko"] = "게임 언어 :",
            ["eng"] = "Game Language :",
            ["es"] = "Idioma del juego:",
            ["fr"] = "Langue du jeu :"
        };

        // ui.settings.confirm
        localizedTexts["ui.settings.confirm"] = new Dictionary<string, string>
        {
            ["ko"] = "확인", ["eng"] = "Confirm", ["es"] = "Confirmar", ["fr"] = "Confirmer"
        };

        // ui.settings.cancel
        localizedTexts["ui.settings.cancel"] = new Dictionary<string, string>
        {
            ["ko"] = "취소", ["eng"] = "Cancel", ["es"] = "Cancelar", ["fr"] = "Annuler"
        };

        // ui.main.start
        localizedTexts["ui.main.start"] = new Dictionary<string, string>
        {
            ["ko"] = "게임 시작", ["eng"] = "Game Start", ["es"] = "Iniciar juego", ["fr"] = "Démarrer le jeu"
        };

        // ui.main.settings
        localizedTexts["ui.main.settings"] = new Dictionary<string, string>
        {
            ["ko"] = "환경 설정", ["eng"] = "Settings", ["es"] = "Configuración", ["fr"] = "Paramètres"
        };

        // ui.main.credits
        localizedTexts["ui.main.credits"] = new Dictionary<string, string>
        {
            ["ko"] = "크레딧", ["eng"] = "Credits", ["es"] = "Créditos", ["fr"] = "Crédits"
        };

        // ui.credits.close
        localizedTexts["ui.credits.close"] = new Dictionary<string, string>
        {
            ["ko"] = "닫기", ["eng"] = "Close", ["es"] = "Cerrar", ["fr"] = "Fermer"
        };

        // ui.how.first
        localizedTexts["ui.how.first"] = new Dictionary<string, string>
        {
            ["ko"] = "원할한 게임을 위해 Google Chrome\n브라우저에서 플레이해주시기 바랍니다.\n\n타 브라우저에서는 원활하게 조작되지\n않을 수도 있습니다.",
            ["eng"] =
                "For the best experience, please play using the Google Chrome browser.\n\nOther browsers may not work properly.",
            ["es"] =
                "Para una mejor experiencia, se recomienda jugar en el navegador Google Chrome.\n\nOtros navegadores pueden no funcionar correctamente.",
            ["fr"] =
                "Pour une meilleure expérience, veuillez jouer avec le navigateur Google Chrome.\n\nD'autres navigateurs peuvent ne pas fonctionner correctement."
        };

        // ui.settings.volume
        localizedTexts["ui.settings.volume"] = new Dictionary<string, string>
        {
            ["ko"] = "음량 :", ["eng"] = "Volume :", ["es"] = "Volumen :", ["fr"] = "Volume :"
        };

        // ui.how.second.move
        localizedTexts["ui.how.second.move"] = new Dictionary<string, string>
        {
            ["ko"] = "방향키로 이동",
            ["eng"] = "Use arrow keys to move",
            ["es"] = "Mover con las flechas",
            ["fr"] = "Se déplacer avec les flèches"
        };

        // ui.how.second.dash
        localizedTexts["ui.how.second.dash"] = new Dictionary<string, string>
        {
            ["ko"] = "Shift로 대쉬",
            ["eng"] = "Press Shift to dash",
            ["es"] = "Presiona Shift para correr",
            ["fr"] = "Appuyez sur Shift pour sprinter"
        };

        // ui.how.second.attack
        localizedTexts["ui.how.second.attack"] = new Dictionary<string, string>
        {
            ["ko"] = "TYPE로 공격",
            ["eng"] = "TYPE to attack",
            ["es"] = "Escribe para atacar",
            ["fr"] = "Tapez pour attaquer"
        };

        // ui.main.how
        localizedTexts["ui.main.how"] = new Dictionary<string, string>
        {
            ["ko"] = "조작법", ["eng"] = "How to Play", ["es"] = "Cómo jugar", ["fr"] = "Comment jouer"
        };

        // ui.gameover.title
        localizedTexts["ui.gameover.title"] = new Dictionary<string, string>
        {
            ["ko"] = "게임 오버", ["eng"] = "Game Over", ["es"] = "Juego terminado", ["fr"] = "Jeu terminé"
        };

        // ui.gameover.score
        localizedTexts["ui.gameover.score"] = new Dictionary<string, string>
        {
            ["ko"] = "점수 : {0}", ["eng"] = "Score : {0}", ["es"] = "Puntuación: {0}", ["fr"] = "Score : {0}"
        };

        // ui.gameover.highscore
        localizedTexts["ui.gameover.highscore"] = new Dictionary<string, string>
        {
            ["ko"] = "최고 점수 : {0}",
            ["eng"] = "High Score : {0}",
            ["es"] = "Puntuación más alta: {0}",
            ["fr"] = "Meilleur score : {0}"
        };

        // ui.victory.title
        localizedTexts["ui.victory.title"] = new Dictionary<string, string>
        {
            ["ko"] = "승리!", ["eng"] = "Victory!", ["es"] = "¡Victoria!", ["fr"] = "Victoire !"
        };

        // ui.victory.score
        localizedTexts["ui.victory.score"] = new Dictionary<string, string>
        {
            ["ko"] = "점수 : {0}", ["eng"] = "Score : {0}", ["es"] = "Puntuación: {0}", ["fr"] = "Score : {0}"
        };

        // ui.victory.highscore
        localizedTexts["ui.victory.highscore"] = new Dictionary<string, string>
        {
            ["ko"] = "최고 점수 : {0}",
            ["eng"] = "High Score : {0}",
            ["es"] = "Puntuación más alta: {0}",
            ["fr"] = "Meilleur score : {0}"
        };

        // ui.victory.typo
        localizedTexts["ui.victory.typo"] = new Dictionary<string, string>
        {
            ["ko"] = "오타 개수 : {0}",
            ["eng"] = "Typos Count : {0}",
            ["es"] = "Errores tipográficos: {0}",
            ["fr"] = "Fautes de frappe : {0}"
        };

        // ui.how.second.dash2
        localizedTexts["ui.how.second.dash2"] = new Dictionary<string, string>
        {
            ["ko"] = "대쉬하는 동안 스태미너를 소모합니다.\n단어를 올바르게 입력하면 스태미너를 회복합니다.",
            ["eng"] = "Dashing consumes stamina.\nYou can recover stamina by typing words correctly.",
            ["es"] = "Correr consume energía.\nEscribe correctamente las palabras para recuperarla.",
            ["fr"] = "Sprinter consomme de l'endurance.\nTapez correctement les mots pour la récupérer."
        };

        // ui.difficult.easy
        localizedTexts["ui.difficult.easy"] = new Dictionary<string, string>
        {
            ["ko"] = "쉬움", ["eng"] = "Easy", ["es"] = "Fácil", ["fr"] = "Facile"
        };

        // ui.difficult.normal
        localizedTexts["ui.difficult.normal"] = new Dictionary<string, string>
        {
            ["ko"] = "보통", ["eng"] = "Normal", ["es"] = "Normal", ["fr"] = "Normal"
        };

        // ui.difficult.hard
        localizedTexts["ui.difficult.hard"] = new Dictionary<string, string>
        {
            ["ko"] = "어려움", ["eng"] = "Hard", ["es"] = "Difícil", ["fr"] = "Difficile"
        };

        // ui.difficult.select
        localizedTexts["ui.difficult.select"] = new Dictionary<string, string>
        {
            ["ko"] = "난이도를 선택하세요.",
            ["eng"] = "Select difficulty.",
            ["es"] = "Seleccione la dificultad.",
            ["fr"] = "Sélectionnez la difficulté."
        };

        // ui.how.third.difficult
        localizedTexts["ui.how.third.difficult"] = new Dictionary<string, string>
        {
            ["ko"] =
                "쉬움 난이도 : 짧은 단어가 더 자주 나옵니다. 점수 x 0.8\n\n보통 난이도 : 일반적인 난이도입니다.\n\n어려움 난이도 : 긴 단어가 더 자주 나옵니다. 점수 x 1.2",
            ["eng"] =
                "Easy difficulty: Short words appear more often. Score x 0.8\n\nNormal difficulty: This is the standard difficulty.\n\nHard difficulty: Long words appear more often. Score x 1.2",
            ["es"] =
                "Dificultad fácil: Las palabras cortas aparecen con más frecuencia. Puntuación x 0.8\n\nDificultad normal: Es la dificultad estándar.\n\nDificultad difícil: Las palabras largas aparecen con más frecuencia. Puntuación x 1.2",
            ["fr"] =
                "Difficulté facile : Les mots courts apparaissent plus souvent. Score x 0,8\n\nDifficulté normale : C'est la difficulté standard.\n\nDifficulté difficile : Les mots longs apparaissent plus souvent. Score x 1,2"
        };

        // ui.how.third.score
        localizedTexts["ui.how.third.score"] = new Dictionary<string, string>
        {
            ["ko"] = "적을 처치하면 점수를 얻습니다.\n\n오타가 발생하면 점수를 잃습니다.",
            ["eng"] = "You gain points by defeating enemies.\n\nYou lose points when you make a typo.",
            ["es"] = "Obtienes puntos al derrotar enemigos.\n\nPierdes puntos si cometes un error tipográfico.",
            ["fr"] =
                "Vous gagnez des points en éliminant les ennemis.\n\nVous perdez des points en cas de faute de frappe."
        };

        // ui.game.koreankey.warning
        localizedTexts["ui.game.koreankey.warning"] = new Dictionary<string, string>
        {
            ["ko"] = "한/영 키를 누르세요. 영어 입력이 안되고 있습니다.",
            ["eng"] = "Press the Korean/English key. English input is not working.",
            ["es"] = "Presione la tecla de cambio coreano/inglés. La entrada en inglés no funciona.",
            ["fr"] = "Appuyez sur la touche coréen/anglais. La saisie en anglais ne fonctionne pas."
        };

        // ui.game.buff.speedup
        localizedTexts["ui.game.buff.speedup"] = new Dictionary<string, string>
        {
            ["ko"] = "플레이어 이동 속도 증가",
            ["eng"] = "Increases player movement speed",
            ["es"] = "Aumenta la velocidad de movimiento del jugador",
            ["fr"] = "Augmente la vitesse de déplacement du joueur"
        };

        // ui.game.buff.attackpowerup
        localizedTexts["ui.game.buff.attackpowerup"] = new Dictionary<string, string>
        {
            ["ko"] = "플레이어 공격력 증가",
            ["eng"] = "Increases player attack power",
            ["es"] = "Aumenta el poder de ataque del jugador",
            ["fr"] = "Augmente la puissance d'attaque du joueur"
        };

        // ui.game.buff.heal
        localizedTexts["ui.game.buff.heal"] = new Dictionary<string, string>
        {
            ["ko"] = "플레이어 회복",
            ["eng"] = "Heals the player",
            ["es"] = "Cura al jugador",
            ["fr"] = "Soigne le joueur"
        };

        // ui.game.buff.slowenmies
        localizedTexts["ui.game.buff.slowenmies"] = new Dictionary<string, string>
        {
            ["ko"] = "적군 이동 속도 감소",
            ["eng"] = "Slows enemy movement speed",
            ["es"] = "Reduce la velocidad de movimiento del enemigo",
            ["fr"] = "Ralentit la vitesse de déplacement des ennemis"
        };

        // ui.game.buff.slowenemyspawn
        localizedTexts["ui.game.buff.slowenemyspawn"] = new Dictionary<string, string>
        {
            ["ko"] = "적군 소환 속도 감소",
            ["eng"] = "Slows enemy spawn rate",
            ["es"] = "Reduce la velocidad de aparición de enemigos",
            ["fr"] = "Ralentit la fréquence d'apparition des ennemis"
        };

        // ui.main.fullscreen.alert
        localizedTexts["ui.main.fullscreen.alert"] = new Dictionary<string, string>
        {
            ["ko"] =
                "무조건 Chrome에서 플레이 해주세요.\n(Edge, Whale 등 타 Chromium 브라우저에서 WebGL 입력 이슈 존재)\n\n모니터의 해상도가 작아 UI가 짤릴 경우 F11을 눌러서 전체화면을 해주세요.",
            ["eng"] =
                "Please use Chrome only.\n(WebGL input issues may occur in other Chromium browsers such as Edge or Whale)\n\nIf the UI is cut off due to low screen resolution, press F11 for fullscreen mode.",
            ["es"] =
                "Por favor, juegue solo en Chrome.\n(Pueden ocurrir problemas de entrada WebGL en otros navegadores Chromium como Edge o Whale)\n\nSi la resolución del monitor es baja y la interfaz se ve recortada, presione F11 para el modo de pantalla completa.",
            ["fr"] =
                "Veuillez jouer uniquement avec Chrome.\n(Des problèmes de saisie WebGL peuvent survenir avec d'autres navigateurs Chromium comme Edge ou Whale)\n\nSi l'interface est coupée à cause d'une faible résolution, appuyez sur F11 pour passer en plein écran."
        };

        // ui.main.fullscreen.neversee
        localizedTexts["ui.main.fullscreen.neversee"] = new Dictionary<string, string>
        {
            ["ko"] = "다시 보지 않기",
            ["eng"] = "Don't show again",
            ["es"] = "No mostrar de nuevo",
            ["fr"] = "Ne plus afficher"
        };

        // ui.main.fullscreen.close
        localizedTexts["ui.main.fullscreen.close"] = new Dictionary<string, string>
        {
            ["ko"] = "닫기", ["eng"] = "Close", ["es"] = "Cerrar", ["fr"] = "Fermer"
        };

        Debug.Log($"하드코딩 데이터 로드 완료 - {localizedTexts.Count}개 키");
    }

    // 초기화 완료 처리
    private static void CompleteInitialization()
    {
        if (isInitialized)
        {
            Debug.LogWarning("LocalizationManager가 이미 초기화되었습니다.");
            return;
        }

        if (localizedTexts.Count == 0)
        {
            Debug.LogWarning("로컬라이제이션 데이터가 없습니다. 기본 데이터를 로드합니다.");
            LoadFallbackData();
        }

        isInitialized = true;
        isInitializing = false;

        Debug.Log($"LocalizationManager 초기화 완료 - {localizedTexts.Count}개 키 로드됨");

        try
        {
            OnLanguageInitialized?.Invoke();
        }
        catch
        {
            // 이벤트 에러 무시
        }
    }

    // 시스템 언어 로드
    private static void LoadSystemLanguage()
    {
        try
        {
            if (PlayerPrefs.HasKey("SystemLanguage"))
            {
                string savedLanguage = PlayerPrefs.GetString("SystemLanguage");
                if (Enum.TryParse(savedLanguage, out SystemLanguage language))
                {
                    systemLanguage = language;
                    Debug.Log("시스템 언어 불러옴: " + systemLanguage.ToString());
                    return;
                }
            }

            SystemLanguage deviceLanguage = Application.systemLanguage;
            systemLanguage = SupportedLanguages.Contains(deviceLanguage) ? deviceLanguage : SystemLanguage.English;

            PlayerPrefs.SetString("SystemLanguage", systemLanguage.ToString());
            PlayerPrefs.Save();
        }
        catch
        {
            systemLanguage = SystemLanguage.English;
        }
    }

    private static void LoadGameLanguage()
    {
        try
        {
            if (PlayerPrefs.HasKey("GameLanguage"))
            {
                string savedLanguage = PlayerPrefs.GetString("GameLanguage");
                if (Enum.TryParse(savedLanguage, out SystemLanguage language))
                {
                    gameLanguage = language;
                    Debug.Log("게임 언어 불러옴: " + gameLanguage.ToString());
                    return;
                }
            }

            gameLanguage = SystemLanguage.English;
            PlayerPrefs.SetString("GameLanguage", gameLanguage.ToString());
            PlayerPrefs.Save();
        }
        catch
        {
            gameLanguage = SystemLanguage.English;
        }
    }

    // 폴백 데이터
    private static void LoadFallbackData()
    {
        Debug.LogWarning("폴백 데이터 사용");

        localizedTexts.Clear();

        try
        {
            localizedTexts["ui.settings.confirm"] = new Dictionary<string, string>
            {
                ["ko"] = "확인", ["eng"] = "Confirm", ["es"] = "Confirmar", ["fr"] = "Confirmer"
            };

            localizedTexts["ui.settings.cancel"] = new Dictionary<string, string>
            {
                ["ko"] = "취소", ["eng"] = "Cancel", ["es"] = "Cancelar", ["fr"] = "Annuler"
            };

            Debug.Log($"폴백 데이터 로드 완료 - {localizedTexts.Count}개 키");
        }
        catch
        {
            // 최소한의 데이터
            localizedTexts["ui.settings.confirm"] = new Dictionary<string, string> { ["eng"] = "Confirm" };
        }
    }

    // 언어 코드 얻기
    private static string GetLanguageCode(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.English: return "eng";
            case SystemLanguage.Korean: return "ko";
            case SystemLanguage.Spanish: return "es";
            case SystemLanguage.French: return "fr";
            default: return "eng";
        }
    }

    // 텍스트 가져오기
    public static string GetLocalizedText(string key)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("LocalizationManager not initialized.");
            return key;
        }

        try
        {
            string languageCode = GetLanguageCode(systemLanguage);

            if (localizedTexts.ContainsKey(key) && localizedTexts[key].ContainsKey(languageCode))
                return localizedTexts[key][languageCode];

            // 영어로 폴백
            if (languageCode != "eng" && localizedTexts.ContainsKey(key) && localizedTexts[key].ContainsKey("eng"))
                return localizedTexts[key]["eng"];

            Debug.LogWarning($"Localization key not found: {key}");
            return key;
        }
        catch
        {
            return key;
        }
    }

    // 포맷팅 지원
    public static string GetLocalizedText(string key, params object[] args)
    {
        string value = GetLocalizedText(key);

        if (args != null && args.Length > 0)
            try
            {
                return string.Format(value, args);
            }
            catch
            {
                Debug.LogError($"Format error for key '{key}'");
                return value;
            }

        return value;
    }

    // 언어 변경 (안전한 버전)
    public static void ChangeSystemLanguage(SystemLanguage language)
    {
        try
        {
            if (!SupportedLanguages.Contains(language))
            {
                Debug.LogWarning($"Language {language} is not supported");
                return;
            }

            if (systemLanguage == language)
            {
                Debug.Log($"언어가 이미 {language}로 설정되어 있습니다.");
                return;
            }

            systemLanguage = language;
            PlayerPrefs.SetString("SystemLanguage", language.ToString());
            PlayerPrefs.Save();

            Debug.Log($"시스템 언어 변경됨: {language}");

            try
            {
                OnLanguageChanged?.Invoke(systemLanguage);
            }
            catch
            {
                // 이벤트 에러 무시
            }
        }
        catch
        {
            Debug.LogError("언어 변경 실패");
        }
    }

    public static void ChangeGameLanguage(SystemLanguage language)
    {
        try
        {
            if (!SupportedLanguages.Contains(language))
            {
                Debug.LogWarning($"Language {language} is not supported");
                return;
            }

            if (gameLanguage == language)
            {
                Debug.Log($"게임 언어가 이미 {language}로 설정되어 있습니다.");
                return;
            }

            SystemLanguage oldGameLanguage = gameLanguage;
            gameLanguage = language;
            PlayerPrefs.SetString("GameLanguage", language.ToString());
            PlayerPrefs.Save();

            Debug.Log($"게임 언어 변경됨: {oldGameLanguage} → {language}");

            try
            {
                OnLanguageChanged?.Invoke(systemLanguage);
            }
            catch
            {
                // 이벤트 에러 무시
            }
        }
        catch
        {
            Debug.LogError("게임 언어 변경 실패");
        }
    }
}

// 확장 메소드
public static class LocalizationExtensions
{
    public static string Localize(this string key)
    {
        return LocalizationManager.GetLocalizedText(key);
    }

    public static string Localize(this string key, params object[] args)
    {
        return LocalizationManager.GetLocalizedText(key, args);
    }
}
