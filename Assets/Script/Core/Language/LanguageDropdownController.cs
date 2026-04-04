using TMPro;
using UnityEngine;

public class LanguageDropdownController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Start()
    {
        if (languageDropdown == null)
        {
            languageDropdown = GetComponent<TMP_Dropdown>();
        }

        if (languageDropdown == null || LanguageManager.Instance == null)
        {
            return;
        }

        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.value = (int)LanguageManager.Instance.currentLanguage;
        languageDropdown.RefreshShownValue();
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    public void OnLanguageChanged(int index)
    {
        if (LanguageManager.Instance == null)
        {
            return;
        }

        LanguageManager.Instance.SetLanguage(index);
    }
}