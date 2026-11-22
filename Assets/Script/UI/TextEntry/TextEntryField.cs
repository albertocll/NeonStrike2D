using TMPro;
using UnityEngine;

// Se engancha a un TMP_InputField: al tocarlo se abre la ventana de escritura a pantalla completa
// (TextEntryOverlay) en lugar de escribir en el campo pequeño. Oculta la caja nativa de Android y el
// teclado del campo original; el teclado lo abre la caja grande, con el mismo tipo de contenido.
[RequireComponent(typeof(TMP_InputField))]
public class TextEntryField : MonoBehaviour
{
    [Tooltip("Clave de LocalizedTexts con el nombre del campo. Vacía: se usa el texto de ejemplo del campo.")]
    [SerializeField] private string labelKey;

    private TMP_InputField field;

    public TMP_InputField Field => field;

    public string Label
    {
        get
        {
            if (!string.IsNullOrEmpty(labelKey)) return LanguageManager.Get(labelKey);
            return field.placeholder is TMP_Text placeholder ? placeholder.text : "";
        }
    }

    private void Awake()
    {
        field = GetComponent<TMP_InputField>();
        field.shouldHideMobileInput = true;
        field.shouldHideSoftKeyboard = true;
        field.onSelect.AddListener(_ => TextEntryOverlay.Open(this));
    }
}
