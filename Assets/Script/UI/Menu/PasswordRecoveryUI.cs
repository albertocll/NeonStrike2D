using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Recuperar la contraseña con un código por correo (issue #199). Lo crea LoginUI al arrancar:
// - en el panel de login, el enlace "¿Olvidaste tu contraseña?" bajo el campo de contraseña;
// - panel 1: el correo, para pedir el código (POST /password/forgot);
// - panel 2: el código y la contraseña nueva dos veces (POST /password/reset).
// Los dos paneles son copias del panel de registro, así que tienen su estilo y sus posiciones.
public class PasswordRecoveryUI : MonoBehaviour
{
    public const int CodeLength = 6;

    // Mismas reglas que el registro en el backend (AccountRules).
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    // Enlace bajo el campo de contraseña, alineado a su borde derecho y a la altura de "Recordar cuenta".
    private static readonly Vector2 LinkPosition = new Vector2(285f, -6f);
    private static readonly Vector2 LinkSize = new Vector2(250f, 40f);

    private MenuManager menu;
    private TMP_InputField loginEmail;
    private TMP_Text loginFeedback;

    private GameObject panelEmail;
    private TMP_InputField emailInput;
    private Button sendButton;
    private TMP_Text emailFeedback;

    private GameObject panelReset;
    private TMP_InputField codeInput;
    private TMP_InputField newPasswordInput;
    private TMP_InputField repeatInput;
    private Button changeButton;
    private TMP_Text resetFeedback;

    private string pendingEmail = "";
    private bool busy;

    public void Build(MenuManager menuManager, GameObject loginPanel, GameObject registerPanel,
        TMP_InputField loginEmailInput, TMP_Text loginFeedbackText, TMP_Text linkTemplate)
    {
        menu = menuManager;
        loginEmail = loginEmailInput;
        loginFeedback = loginFeedbackText;

        CreateLink(loginPanel.transform, linkTemplate);
        BuildEmailPanel(registerPanel);
        BuildResetPanel(registerPanel);
    }

    // ── Validación (también la usan las pruebas EditMode) ───────────────────

    // Clave del texto de error del panel 2, o null si los datos valen.
    public static string ValidateReset(string code, string newPassword, string repeat)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(repeat))
            return "msg.fillFields";
        if (!IsWellFormedCode(code))
            return "reset.codeRule";
        if (newPassword.Length < PasswordMinLength || newPassword.Length > PasswordMaxLength)
            return "server.passwordRule";
        if (newPassword != repeat)
            return "reset.mismatch";
        return null;
    }

    public static bool IsWellFormedCode(string code)
    {
        if (code == null || code.Length != CodeLength) return false;
        foreach (char c in code)
            if (c < '0' || c > '9') return false;
        return true;
    }

    public static string LanguageCode() =>
        LanguageManager.Current == LanguageManager.Language.Spanish ? "es" : "en";

    // ── Construcción ────────────────────────────────────────────────────────

    private void CreateLink(Transform loginPanel, TMP_Text template)
    {
        if (template == null) return;

        var link = Instantiate(template, loginPanel);
        link.name = "TextForgotPassword";
        MenuUiFactory.SetKey(link, "login.forgotPassword");
        link.alignment = TextAlignmentOptions.Right;
        link.textWrappingMode = TextWrappingModes.NoWrap;
        link.enableAutoSizing = true;
        link.fontSizeMax = template.fontSize;
        link.fontSizeMin = Mathf.Min(template.fontSize, 14f);
        link.raycastTarget = true;

        var rect = link.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = LinkPosition;
        rect.sizeDelta = LinkSize;

        var button = link.gameObject.AddComponent<Button>();
        button.targetGraphic = link;
        button.onClick.AddListener(OpenEmailPanel);
    }

    private void BuildEmailPanel(GameObject registerPanel)
    {
        panelEmail = ClonePanel(registerPanel, "PanelForgotPassword");

        SetKey(panelEmail, "TextTitle", "forgot.title");
        Hide(panelEmail, "InputUsername");
        Hide(panelEmail, "TextLabelUsername");
        Hide(panelEmail, "InputPassword");
        Hide(panelEmail, "TextLabelPassword");

        emailInput = Find<TMP_InputField>(panelEmail, "InputEmail");
        emailFeedback = Find<TMP_Text>(panelEmail, "TextFeedbackRegister");

        sendButton = SetupButton(panelEmail, "ButtonRegister", "forgot.send", OnSendClicked);
        SetupButton(panelEmail, "ButtonGoToLogin", "menu.login", OpenLogin);
        SetupButton(panelEmail, "ButtonClose", "common.close", menu.CerrarPaneles);
        // "¿Ya tienes cuenta?" sigue igual, junto al botón de volver al login.
    }

    private void BuildResetPanel(GameObject registerPanel)
    {
        panelReset = ClonePanel(registerPanel, "PanelResetPassword");

        SetKey(panelReset, "TextTitle", "reset.title");
        SetKey(panelReset, "TextHaveAccount", "reset.noCode");

        codeInput = SetupInput(panelReset, "InputUsername", "TextLabelUsername",
            "reset.codeLabel", "placeholder.code", TMP_InputField.ContentType.IntegerNumber, CodeLength);
        newPasswordInput = SetupInput(panelReset, "InputEmail", "TextLabelEmail",
            "reset.newPasswordLabel", "placeholder.newPassword", TMP_InputField.ContentType.Password, PasswordMaxLength);
        repeatInput = SetupInput(panelReset, "InputPassword", "TextLabelPassword",
            "reset.repeatLabel", "placeholder.repeatPassword", TMP_InputField.ContentType.Password, PasswordMaxLength);
        resetFeedback = Find<TMP_Text>(panelReset, "TextFeedbackRegister");

        changeButton = SetupButton(panelReset, "ButtonRegister", "reset.confirm", OnChangeClicked);
        // "¿No te ha llegado?" + volver: de vuelta al panel del correo para pedir otro código.
        SetupButton(panelReset, "ButtonGoToLogin", "common.back", OpenEmailPanelKeepingEmail);
        SetupButton(panelReset, "ButtonClose", "common.close", menu.CerrarPaneles);
    }

    private GameObject ClonePanel(GameObject template, string name)
    {
        var panel = Instantiate(template, template.transform.parent);
        panel.name = name;
        panel.SetActive(false);
        // Justo detrás del panel de registro, para quedar en la misma capa.
        panel.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
        menu.AddPanel(panel);
        return panel;
    }

    private static TMP_InputField SetupInput(GameObject panel, string inputName, string labelName,
        string labelKey, string placeholderKey, TMP_InputField.ContentType contentType, int characterLimit)
    {
        var input = Find<TMP_InputField>(panel, inputName);
        if (input == null) return null;

        input.text = "";
        input.contentType = contentType;
        input.characterLimit = characterLimit;
        input.onValidateInput = null;
        if (input.placeholder != null) MenuUiFactory.SetKey(input.placeholder, placeholderKey);
        if (input.TryGetComponent(out TextEntryField entry)) entry.LabelKey = labelKey;

        var label = Find<TMP_Text>(panel, labelName);
        if (label != null)
        {
            MenuUiFactory.SetKey(label, labelKey);
            // Los nombres nuevos son más largos que "Usuario" o "Correo": se encogen si no caben.
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = Mathf.Min(label.fontSize, 14f);
            label.enableAutoSizing = true;
        }
        return input;
    }

    private static Button SetupButton(GameObject panel, string name, string key, UnityEngine.Events.UnityAction onClick)
    {
        var button = Find<Button>(panel, name);
        if (button == null) return null;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        MenuUiFactory.SetKey(label, key);
        return button;
    }

    private static void SetKey(GameObject panel, string name, string key)
    {
        var text = Find<TMP_Text>(panel, name);
        if (text != null) MenuUiFactory.SetKey(text, key);
    }

    private static void Hide(GameObject panel, string name)
    {
        var child = FindChild(panel.transform, name);
        if (child != null) child.gameObject.SetActive(false);
    }

    private static T Find<T>(GameObject panel, string name) where T : Component
    {
        var child = FindChild(panel.transform, name);
        if (child == null)
        {
            Debug.LogWarning($"[PasswordRecoveryUI] No se encuentra {name} en {panel.name}.");
            return null;
        }
        return child.GetComponent<T>();
    }

    // Hijo directo por nombre; algunos nombres de la escena llevan un espacio al final.
    private static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name.Trim() == name) return child;
        return null;
    }

    // ── Navegación ─────────────────────────────────────────────────────────

    private void OpenLogin() => menu.AbrirLogin();

    private void OpenEmailPanel()
    {
        // Si en el login ya hay un correo escrito, se aprovecha.
        string typed = loginEmail != null ? loginEmail.text.Trim() : "";
        OpenEmailPanelWith(typed.Contains("@") ? typed : pendingEmail);
    }

    private void OpenEmailPanelKeepingEmail() => OpenEmailPanelWith(pendingEmail);

    private void OpenEmailPanelWith(string email)
    {
        if (emailInput != null) emailInput.text = email ?? "";
        SetFeedback(emailFeedback, LanguageManager.Get("forgot.hint"));
        menu.AbrirPanel(panelEmail);
    }

    private void OpenResetPanel()
    {
        foreach (var input in new[] { codeInput, newPasswordInput, repeatInput })
            if (input != null) input.text = "";
        SetFeedback(resetFeedback, LanguageManager.Get("reset.sent"));
        menu.AbrirPanel(panelReset);
    }

    // ── Peticiones ─────────────────────────────────────────────────────────

    private async void OnSendClicked()
    {
        if (busy) return;
        string email = emailInput != null ? emailInput.text.Trim() : "";
        if (string.IsNullOrEmpty(email))
        {
            SetFeedback(emailFeedback, LanguageManager.Get("msg.fillFields"));
            return;
        }

        busy = true;
        SetInteractable(sendButton, false);
        SetFeedback(emailFeedback, LanguageManager.Get("forgot.sending"));
        try
        {
            var response = await ApiManager.Instance.ForgotPasswordAsync(email, LanguageCode());
            if (this == null) return;

            if (response == null)
                SetFeedback(emailFeedback, LanguageManager.Get("msg.connectError"));
            else if (response.success)
            {
                pendingEmail = email;
                OpenResetPanel();
            }
            else
                SetFeedback(emailFeedback, LanguageManager.ServerError(response.code, response.message, "forgot.error"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[PasswordRecoveryUI] Error al pedir el código: {e.GetType().Name}: {e.Message}");
            if (this != null) SetFeedback(emailFeedback, LanguageManager.Get("msg.connectError"));
        }
        finally
        {
            busy = false;
            if (this != null) SetInteractable(sendButton, true);
        }
    }

    private async void OnChangeClicked()
    {
        if (busy) return;
        string code = codeInput != null ? codeInput.text.Trim() : "";
        string newPassword = newPasswordInput != null ? newPasswordInput.text : "";
        string repeat = repeatInput != null ? repeatInput.text : "";

        string error = ValidateReset(code, newPassword, repeat);
        if (error != null)
        {
            SetFeedback(resetFeedback, LanguageManager.Get(error));
            return;
        }

        busy = true;
        SetInteractable(changeButton, false);
        SetFeedback(resetFeedback, LanguageManager.Get("reset.changing"));
        try
        {
            var response = await ApiManager.Instance.ResetPasswordAsync(pendingEmail, code, newPassword);
            if (this == null) return;

            if (response == null)
                SetFeedback(resetFeedback, LanguageManager.Get("msg.connectError"));
            else if (response.success)
            {
                menu.AbrirLogin();
                if (loginEmail != null) loginEmail.text = pendingEmail;
                SetFeedback(loginFeedback, LanguageManager.Get("reset.done"));
            }
            else
                SetFeedback(resetFeedback, LanguageManager.ServerError(response.code, response.message, "reset.error"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[PasswordRecoveryUI] Error al cambiar la contraseña: {e.GetType().Name}: {e.Message}");
            if (this != null) SetFeedback(resetFeedback, LanguageManager.Get("msg.connectError"));
        }
        finally
        {
            busy = false;
            if (this != null) SetInteractable(changeButton, true);
        }
    }

    private static void SetFeedback(TMP_Text text, string message)
    {
        if (text != null) text.text = message;
    }

    private static void SetInteractable(Button button, bool on)
    {
        if (button != null) button.interactable = on;
    }
}
