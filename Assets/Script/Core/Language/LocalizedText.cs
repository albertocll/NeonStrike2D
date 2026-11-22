using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Texto fijo de una escena o un prefab que se traduce por clave (LocalizedTexts). Se pone al activarse
// y se refresca al momento cuando cambia el idioma. Vale para TMP y para el Text antiguo de UI.
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    public string Key
    {
        get => key;
        set
        {
            key = value;
            Apply();
        }
    }

    private void OnEnable()
    {
        LanguageManager.Changed += Apply;
        Apply();
    }

    private void OnDisable()
    {
        LanguageManager.Changed -= Apply;
    }

    public void Apply()
    {
        if (string.IsNullOrEmpty(key)) return;

        string text = LanguageManager.Get(key);
        if (TryGetComponent(out TMP_Text tmp)) tmp.text = text;
        else if (TryGetComponent(out Text legacy)) legacy.text = text;
    }
}
