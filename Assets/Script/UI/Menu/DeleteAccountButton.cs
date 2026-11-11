using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Botón DELETE ACCOUNT en Opciones (issue #280, requisito de Google Play). Se crea por código copiando
// BackButton, centrado debajo de él, y solo se ve con sesión iniciada. Abre una ventana de confirmación
// (también creada por código con el estilo de Opciones) que avisa de que el borrado es irreversible;
// al confirmar borra la cuenta en el servidor, olvida la sesión y recarga el menú principal.
[RequireComponent(typeof(OptionsMenu))]
public class DeleteAccountButton : MonoBehaviour
{
    private const string TextButton = "DELETE ACCOUNT";
    private const string TextTitle = "DELETE ACCOUNT?";
    private const string TextBody = "Your account, scores and friends will be deleted permanently.\nThis cannot be undone.";
    private const string TextWorking = "Deleting...";
    private const string TextError = "Could not delete the account. Check your connection and try again.";
    private const float Gap = 16f;
    private static readonly Vector2 DialogSize = new Vector2(600f, 300f);
    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.75f);

    private Button button;
    private GameObject dialog;
    private TMP_Text bodyText;
    private Button cancelButton;
    private Button confirmButton;
    private bool busy;

    private void Start()
    {
        GameObject panel = GetComponent<OptionsMenu>().optionsWindow;
        Button back = null;
        foreach (var b in panel.GetComponentsInChildren<Button>(true))
            if (b.name == "BackButton") { back = b; break; }
        if (back == null)
        {
            Debug.LogWarning("[DeleteAccountButton] No se encontró BackButton; no se crea el botón de borrar cuenta.");
            enabled = false;
            return;
        }

        button = CloneButton(back, back.transform.parent, "DeleteAccountButton", TextButton, OpenDialog);
        var backRect = (RectTransform)back.transform;
        ((RectTransform)button.transform).anchoredPosition =
            new Vector2(0f, backRect.anchoredPosition.y - backRect.sizeDelta.y - Gap);
        button.gameObject.SetActive(false);

        BuildDialog(panel.transform, back);
    }

    private void Update()
    {
        if (button == null) return;

        // La sesión recordada se restaura en segundo plano, así que la visibilidad se comprueba cada frame.
        bool hasSession = NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.Token);
        if (button.gameObject.activeSelf != hasSession) button.gameObject.SetActive(hasSession);
    }

    // Fondo oscuro a pantalla completa (bloquea los toques a Opciones) con una ventana centrada
    // que copia la imagen de la ventana de Opciones.
    private void BuildDialog(Transform panel, Button back)
    {
        dialog = new GameObject("DeleteAccountDialog", typeof(RectTransform), typeof(Image));
        var dimRect = (RectTransform)dialog.transform;
        dimRect.SetParent(panel, false);
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
        dialog.GetComponent<Image>().color = Dim;

        var window = new GameObject("Window", typeof(RectTransform), typeof(Image));
        var windowRect = (RectTransform)window.transform;
        windowRect.SetParent(dimRect, false);
        windowRect.sizeDelta = DialogSize;
        var windowImage = window.GetComponent<Image>();
        var optionsImage = back.transform.parent.GetComponent<Image>();
        if (optionsImage != null)
        {
            windowImage.sprite = optionsImage.sprite;
            windowImage.type = optionsImage.type;
            windowImage.color = optionsImage.color;
            windowImage.pixelsPerUnitMultiplier = optionsImage.pixelsPerUnitMultiplier;
        }
        else
        {
            windowImage.color = new Color(0.03f, 0.04f, 0.06f, 1f);
        }

        var labelTemplate = back.GetComponentInChildren<TMP_Text>(true);
        var title = CreateText(labelTemplate, windowRect, TextTitle, new Vector2(0f, 100f), new Vector2(540f, 50f), 1.4f);
        title.color = new Color(1f, 0.35f, 0.35f, 1f);
        bodyText = CreateText(labelTemplate, windowRect, TextBody, new Vector2(0f, 15f), new Vector2(540f, 110f), 1f);

        cancelButton = CloneButton(back, windowRect, "CancelButton", "CANCEL", CloseDialog);
        confirmButton = CloneButton(back, windowRect, "ConfirmDeleteButton", "DELETE", OnConfirmClicked);
        PlaceInWindow(cancelButton, new Vector2(-130f, -100f));
        PlaceInWindow(confirmButton, new Vector2(130f, -100f));

        dialog.SetActive(false);
    }

    private Button CloneButton(Button template, Transform parent, string name, string text, UnityEngine.Events.UnityAction onClick)
    {
        var clone = Instantiate(template, parent);
        clone.name = name;
        clone.onClick = new Button.ButtonClickedEvent(); // quita el CloseOptions copiado del botón de volver
        clone.onClick.AddListener(onClick);

        var label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
            var rect = (RectTransform)clone.transform;
            float needed = label.GetPreferredValues(text).x + 20f;
            if (needed > rect.sizeDelta.x) rect.sizeDelta = new Vector2(needed, rect.sizeDelta.y);
        }
        return clone;
    }

    private static void PlaceInWindow(Button b, Vector2 pos)
    {
        var rect = (RectTransform)b.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
    }

    private static TMP_Text CreateText(TMP_Text template, RectTransform parent, string text, Vector2 pos, Vector2 size, float scale)
    {
        TMP_Text t;
        if (template != null)
        {
            t = Instantiate(template, parent);
            t.fontSize = template.fontSize * scale;
        }
        else
        {
            t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.transform.SetParent(parent, false);
            t.fontSize = 24f * scale;
        }

        var rect = t.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.text = text;
        return t;
    }

    private void OpenDialog()
    {
        bodyText.text = TextBody;
        SetDialogInteractable(true);
        dialog.SetActive(true);
        dialog.transform.SetAsLastSibling();
    }

    private void CloseDialog()
    {
        if (!busy) dialog.SetActive(false);
    }

    private void SetDialogInteractable(bool on)
    {
        cancelButton.interactable = on;
        confirmButton.interactable = on;
    }

    private async void OnConfirmClicked()
    {
        if (busy) return;
        busy = true;
        SetDialogInteractable(false);
        bodyText.text = TextWorking;

        bool deleted = await ApiManager.Instance.DeleteAccountAsync();
        if (!deleted)
        {
            busy = false;
            SetDialogInteractable(true);
            bodyText.text = TextError;
            return;
        }

        LoginUI.ForgetRememberedAccount();
        GameData.Username = "";
        NetworkManager.Instance.ClearUserData();
        try
        {
            await NetworkManager.Instance.DisconnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DeleteAccountButton] Error al cerrar el WebSocket: {e.Message}");
        }

        SceneLoader.Load("MainMenu");
    }
}
