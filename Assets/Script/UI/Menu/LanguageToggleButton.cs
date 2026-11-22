using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Botón de idioma de la esquina superior derecha del menú principal: muestra "es en" con el idioma
// activo resaltado y al pulsarlo cambia al otro. Sin signos, porque Orbitronio los dibuja como anillos.
[RequireComponent(typeof(Button))]
public class LanguageToggleButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.35f);

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        GetComponent<Button>().onClick.AddListener(LanguageManager.Toggle);
    }

    private void OnEnable()
    {
        LanguageManager.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LanguageManager.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (label == null) return;
        bool spanish = LanguageManager.Current == LanguageManager.Language.Spanish;
        label.text = $"{Paint("es", spanish)}  {Paint("en", !spanish)}";
    }

    private string Paint(string text, bool active)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(active ? activeColor : inactiveColor);
        return $"<color=#{hex}>{text}</color>";
    }
}
