using System.Collections.Generic;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    public enum Language
    {
        Spanish,
        English
    }

    public Language currentLanguage = Language.Spanish;

    private Dictionary<string, string> spanishTexts = new Dictionary<string, string>()
    {
        { "play", "JUGAR" },
        { "options", "OPCIONES" },
        { "exit", "SALIR" },
        { "about", "ABOUT" },
        { "back", "VOLVER" },
        { "music", "MÚSICA" },
        { "language", "IDIOMA" },
        { "gameOver", "FIN DE PARTIDA" },
        { "restart", "REINICIAR" },
        { "mainMenu", "MENÚ PRINCIPAL" }
    };

    private Dictionary<string, string> englishTexts = new Dictionary<string, string>()
    {
        { "play", "PLAY" },
        { "options", "OPTIONS" },
        { "exit", "EXIT" },
        { "about", "ABOUT" },
        { "back", "BACK" },
        { "music", "MUSIC" },
        { "language", "LANGUAGE" },
        { "gameOver", "GAME OVER" },
        { "restart", "RESTART" },
        { "mainMenu", "MAIN MENU" }
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLanguage();
    }

    public void SetLanguage(int languageIndex)
    {
        currentLanguage = (Language)languageIndex;
        PlayerPrefs.SetInt("language", languageIndex);
        PlayerPrefs.Save();
    }

    public void LoadLanguage()
    {
        int savedLanguage = PlayerPrefs.GetInt("language", 0);
        currentLanguage = (Language)savedLanguage;
    }

    public string GetText(string key)
    {
        switch (currentLanguage)
        {
            case Language.English:
                return englishTexts.ContainsKey(key) ? englishTexts[key] : key;

            case Language.Spanish:
            default:
                return spanishTexts.ContainsKey(key) ? spanishTexts[key] : key;
        }
    }
}