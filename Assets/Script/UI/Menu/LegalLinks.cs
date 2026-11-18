using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Enlaces a la política de privacidad y a los términos de uso (issue #117, requisito de Google Play).
// Se crean por código en Opciones: dos textos pulsables justo debajo del marco de la ventana, fuera de él
// para no pisar los botones de la fila inferior. Abren las páginas publicadas en la web del juego.
[RequireComponent(typeof(OptionsMenu))]
public class LegalLinks : MonoBehaviour
{
    public const string PrivacyUrl = "https://neonstrike-game.netlify.app/politica-privacidad.html";
    public const string TermsUrl = "https://neonstrike-game.netlify.app/terminos-uso.html";

    private const string TextPrivacy = "PRIVACY POLICY";
    private const string TextTerms = "TERMS OF USE";
    private const float GapBelowWindow = 28f;
    private const float HalfSpacing = 150f;
    private const float FontScale = 0.8f;
    private static readonly Color LinkColor = new Color(0f, 0.9f, 1f, 1f);

    private void Start()
    {
        GameObject panel = GetComponent<OptionsMenu>().optionsWindow;
        Button back = null;
        foreach (var b in panel.GetComponentsInChildren<Button>(true))
            if (b.name == "BackButton") { back = b; break; }
        if (back == null)
        {
            Debug.LogWarning("[LegalLinks] No se encontró BackButton; no se crean los enlaces legales.");
            enabled = false;
            return;
        }

        var window = (RectTransform)back.transform.parent;
        var template = back.GetComponentInChildren<TMP_Text>(true);
        float y = -window.rect.height * 0.5f - GapBelowWindow;

        CreateLink(template, window, "PrivacyLink", TextPrivacy, new Vector2(-HalfSpacing, y), PrivacyUrl);
        CreateLink(template, window, "TermsLink", TextTerms, new Vector2(HalfSpacing, y), TermsUrl);
    }

    private static void CreateLink(TMP_Text template, RectTransform parent, string name, string text, Vector2 pos, string url)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;

        TMP_Text label;
        if (template != null)
        {
            label = Instantiate(template, rect);
            label.fontSize = template.fontSize * FontScale;
        }
        else
        {
            label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(rect, false);
            label.fontSize = 20f;
        }
        label.name = "Label";
        label.text = $"<u>{text}</u>";
        label.color = LinkColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = true;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

        Vector2 preferred = label.GetPreferredValues(label.text);
        rect.sizeDelta = new Vector2(preferred.x + 20f, Mathf.Max(preferred.y, 30f) + 10f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = label;
        button.onClick.AddListener(() => Application.OpenURL(url));
    }
}
