using UnityEngine;
using TMPro;

public class LanguageController : MonoBehaviour
{
    public TMP_Dropdown languageDropdown;

    private void Start()
    {
        // Cargar idioma guardado
        int savedLang = PlayerPrefs.GetInt("language", 0);
        languageDropdown.value = savedLang;

        // Actualizar opciones según idioma guardado
        ApplyLanguage(savedLang);

        // Refrescar texto mostrado
        languageDropdown.RefreshShownValue();
    }

    public void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt("language", index);
        ApplyLanguage(index);
    }

    private void ApplyLanguage(int index)
    {
        switch (index)
        {
            case 0:
                // Español
                SetDropdownLabels("Español", "Inglés", "Francés");
                Debug.Log("Idioma cambiado a Español");
                break;

            case 1:
                // Inglés
                SetDropdownLabels("Spanish", "English", "French");
                Debug.Log("Idioma cambiado a Inglés");
                break;

            case 2:
                // Francés
                SetDropdownLabels("Espagnol", "Anglais", "Français");
                Debug.Log("Idioma cambiado a Francés");
                break;
        }

        // Forzar actualización visual
        languageDropdown.RefreshShownValue();
    }

    private void SetDropdownLabels(string opt0, string opt1, string opt2)
    {
        languageDropdown.options[0].text = opt0;
        languageDropdown.options[1].text = opt1;
        languageDropdown.options[2].text = opt2;
    }
}
