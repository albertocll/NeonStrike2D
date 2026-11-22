using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Botones y textos de Opciones creados por código: tamaño y fuente fijos, sin tamaño automático,
// para que los coloque el layout de la ventana y ningún texto se salga de su botón.
public static class MenuUiFactory
{
    // Estilos que en Orbitronio no se leen o rompen la regla de textos (mayúsculas, subrayado).
    private const FontStyles StylesNotAllowed = FontStyles.UpperCase | FontStyles.SmallCaps | FontStyles.Underline;

    // Copia un botón (fondo y etiqueta) con el texto de otra clave, tamaño fijo y sin los listeners de la plantilla.
    public static Button CloneButton(Button template, Transform parent, string name, string key,
        Vector2 size, float fontSize, UnityAction onClick)
    {
        var clone = Object.Instantiate(template, parent);
        clone.name = name;
        clone.onClick = new Button.ButtonClickedEvent();
        clone.onClick.AddListener(onClick);
        ((RectTransform)clone.transform).sizeDelta = size;
        var label = clone.GetComponentInChildren<TMP_Text>(true);
        SetupLabel(label, LanguageManager.Get(key), fontSize);
        SetKey(label, key);
        return clone;
    }

    // Traduce el texto por clave y lo refresca al cambiar de idioma. Si el texto viene de una copia,
    // reutiliza el LocalizedText copiado cambiándole la clave.
    public static void SetKey(Component text, string key)
    {
        if (text == null) return;
        if (!text.TryGetComponent(out LocalizedText localized)) localized = text.gameObject.AddComponent<LocalizedText>();
        localized.Key = key;
    }

    // Etiqueta centrada que ocupa todo su padre, en una línea y con tamaño de fuente fijo.
    public static void SetupLabel(TMP_Text label, string text, float fontSize)
    {
        if (label == null) return;
        label.text = text;
        label.enableAutoSizing = false;
        label.fontSize = fontSize;
        label.fontStyle &= ~StylesNotAllowed;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Center;
        Stretch(label.rectTransform);
    }

    // Texto suelto copiado de una plantilla (misma fuente y material) con tamaño fijo, traducido por clave.
    public static TMP_Text CreateText(TMP_Text template, Transform parent, string name, string key,
        Vector2 size, float fontSize, bool wrap)
    {
        TMP_Text t;
        if (template != null)
        {
            t = Object.Instantiate(template, parent);
        }
        else
        {
            t = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(parent, false);
        }
        t.name = name;
        SetKey(t, key);
        t.enableAutoSizing = false;
        t.fontSize = fontSize;
        t.fontStyle &= ~StylesNotAllowed;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        t.raycastTarget = false;

        var rect = t.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return t;
    }

    // Contenedor vacío de tamaño fijo para colocar elementos en fila o en columna.
    public static RectTransform CreateGroup(Transform parent, string name, Vector2 size, bool horizontal, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = size;

        HorizontalOrVerticalLayoutGroup group = horizontal
            ? go.AddComponent<HorizontalLayoutGroup>()
            : go.AddComponent<VerticalLayoutGroup>();
        group.spacing = spacing;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = group.childControlHeight = false;
        group.childForceExpandWidth = group.childForceExpandHeight = false;
        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
