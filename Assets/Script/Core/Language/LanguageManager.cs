using System;
using UnityEngine;

// Idioma del juego (español o inglés). El primer arranque es en inglés; la elección se guarda en
// PlayerPrefs. Al cambiar de idioma se lanza Changed para que los textos en pantalla se refresquen
// al momento (LocalizedText y los scripts que ponen textos por código).
public static class LanguageManager
{
    public enum Language
    {
        English,
        Spanish
    }

    private const string PrefKey = "idioma";
    private const string PrefEnglish = "en";
    private const string PrefSpanish = "es";

    private static bool loaded;
    private static Language current;

    public static event Action Changed;

    public static Language Current
    {
        get
        {
            EnsureLoaded();
            return current;
        }
    }

    public static void SetLanguage(Language language)
    {
        EnsureLoaded();
        if (current == language) return;

        current = language;
        PlayerPrefs.SetString(PrefKey, language == Language.Spanish ? PrefSpanish : PrefEnglish);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void Toggle()
    {
        SetLanguage(Current == Language.English ? Language.Spanish : Language.English);
    }

    // Texto de la clave en el idioma actual. Si la clave no existe se devuelve la propia clave,
    // para que el fallo se vea en pantalla y no quede un hueco.
    public static string Get(string key)
    {
        return LocalizedTexts.TryGet(key, Current, out var text) ? text : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    // Mensajes del servidor que se enseñan al jugador: llegan en español, así que se traducen en el
    // cliente. Si el mensaje no es uno conocido se usa la clave de respaldo.
    public static string ServerMessage(string message, string fallbackKey)
    {
        if (!string.IsNullOrEmpty(message) && LocalizedTexts.TryGetServerKey(message.Trim(), out var key))
            return Get(key);
        return Get(fallbackKey);
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        current = PlayerPrefs.GetString(PrefKey, PrefEnglish) == PrefSpanish ? Language.Spanish : Language.English;
    }

    // Sin recarga de dominio al entrar en Play, el estado estático sobreviviría entre sesiones.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        Changed = null;
    }
}
