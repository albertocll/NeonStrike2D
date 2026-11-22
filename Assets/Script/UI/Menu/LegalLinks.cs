using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Enlaces a la política de privacidad y a los términos de uso (issue #117, requisito de Google Play).
// Se crean por código en la fila de pie de la ventana de Opciones, en pequeño y en una sola fila.
// Abren las páginas publicadas en la web del juego.
[RequireComponent(typeof(OptionsMenu))]
public class LegalLinks : MonoBehaviour
{
    public const string PrivacyUrl = "https://neonstrike-game.netlify.app/politica-privacidad.html";
    public const string TermsUrl = "https://neonstrike-game.netlify.app/terminos-uso.html";

    private const string KeyPrivacy = "options.privacy";
    private const string KeyTerms = "options.terms";
    private const float FontSize = 20f;
    private static readonly Vector2 LinkSize = new Vector2(330f, 36f);
    private static readonly Color LinkColor = Color.white;

    private void Start()
    {
        var options = GetComponent<OptionsMenu>();
        if (options.legalRow == null || options.backButton == null)
        {
            Debug.LogWarning("[LegalLinks] Falta la fila legal o el botón de volver en OptionsMenu; no se crean los enlaces.");
            enabled = false;
            return;
        }

        var template = options.backButton.GetComponentInChildren<TMP_Text>(true);
        CreateLink(template, options.legalRow, "PrivacyLink", KeyPrivacy, PrivacyUrl);
        CreateLink(template, options.legalRow, "TermsLink", KeyTerms, TermsUrl);
    }

    private static void CreateLink(TMP_Text template, Transform parent, string name, string key, string url)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = LinkSize;

        var label = MenuUiFactory.CreateText(template, rect, "Label", key, LinkSize, FontSize, false);
        MenuUiFactory.Stretch(label.rectTransform);
        label.color = LinkColor;
        label.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.targetGraphic = label;
        button.onClick.AddListener(() => Application.OpenURL(url));
    }
}
