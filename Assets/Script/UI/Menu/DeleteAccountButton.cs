using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Botón de borrar cuenta en Opciones (issue #280, requisito de Google Play). Se crea por código en la fila
// de cuenta de la ventana de Opciones, que solo se ve con sesión iniciada. Abre una ventana de confirmación
// (también creada por código con el estilo de Opciones) que avisa de que el borrado es irreversible;
// al confirmar borra la cuenta en el servidor, olvida la sesión y recarga el menú principal.
[RequireComponent(typeof(OptionsMenu))]
public class DeleteAccountButton : MonoBehaviour
{
    private const string TextButton = "borrar cuenta";
    private const string TextTitle = "borrar cuenta";
    private const string TextBody = "tu cuenta y tus datos se borraran para siempre";
    private const string TextWorking = "borrando";
    private const string TextError = "no se pudo borrar la cuenta revisa tu conexion";
    private const string TextCancel = "cancelar";
    private const string TextConfirm = "borrar";

    private const float TitleFontSize = 40f;
    private const float BodyFontSize = 26f;
    private static readonly Vector2 TextSize = new Vector2(560f, 50f);
    private static readonly Vector2 BodySize = new Vector2(560f, 80f);
    private static readonly Vector2 DialogButtonSize = new Vector2(240f, 56f);
    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.95f);
    private static readonly Color TitleColor = new Color(1f, 0.35f, 0.35f, 1f);

    private GameObject dialog;
    private TMP_Text bodyText;
    private Button cancelButton;
    private Button confirmButton;
    private bool busy;

    private void Start()
    {
        var options = GetComponent<OptionsMenu>();
        Button back = options.backButton;
        if (back == null || options.accountRow == null)
        {
            Debug.LogWarning("[DeleteAccountButton] Falta el botón de volver o la fila de cuenta; no se crea el botón de borrar cuenta.");
            enabled = false;
            return;
        }

        var button = MenuUiFactory.CloneButton(back, options.accountRow, "DeleteAccountButton", TextButton,
            OptionsMenu.AccountButtonSize, OptionsMenu.AccountFontSize, OpenDialog);
        button.transform.SetAsLastSibling();

        BuildDialog(options.optionsWindow.transform, back);
    }

    // Fondo oscuro a pantalla completa (bloquea los toques a Opciones) con una ventana centrada
    // que copia la imagen de la ventana de Opciones. Título, texto y botones van en columna.
    private void BuildDialog(Transform panel, Button back)
    {
        dialog = new GameObject("DeleteAccountDialog", typeof(RectTransform), typeof(Image));
        var dimRect = (RectTransform)dialog.transform;
        dimRect.SetParent(panel, false);
        MenuUiFactory.Stretch(dimRect);
        dialog.GetComponent<Image>().color = Dim;

        var window = new GameObject("Window", typeof(RectTransform));
        var windowRect = (RectTransform)window.transform;
        windowRect.SetParent(dimRect, false);
        windowRect.anchorMin = windowRect.anchorMax = windowRect.pivot = new Vector2(0.5f, 0.5f);

        // El fondo va en un hijo que se estira y no cuenta para el layout: si la imagen estuviera en la
        // propia ventana, su tamaño de sprite mandaría sobre el ContentSizeFitter.
        var background = new GameObject("Fondo", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var backgroundRect = (RectTransform)background.transform;
        backgroundRect.SetParent(windowRect, false);
        MenuUiFactory.Stretch(backgroundRect);
        background.GetComponent<LayoutElement>().ignoreLayout = true;
        var windowImage = background.GetComponent<Image>();
        var optionsImage = OptionsMenu.WindowBackground(back.transform.parent);
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

        var column = window.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(40, 40, 40, 40);
        column.spacing = 24f;
        column.childAlignment = TextAnchor.MiddleCenter;
        column.childControlWidth = column.childControlHeight = false;
        column.childForceExpandWidth = column.childForceExpandHeight = false;
        var fitter = window.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var labelTemplate = back.GetComponentInChildren<TMP_Text>(true);
        var title = MenuUiFactory.CreateText(labelTemplate, windowRect, "Title", TextTitle, TextSize, TitleFontSize, false);
        title.color = TitleColor;
        bodyText = MenuUiFactory.CreateText(labelTemplate, windowRect, "Body", TextBody, BodySize, BodyFontSize, true);

        var row = MenuUiFactory.CreateGroup(windowRect, "Buttons", new Vector2(TextSize.x, DialogButtonSize.y), true, 40f);
        cancelButton = MenuUiFactory.CloneButton(back, row, "CancelButton", TextCancel, DialogButtonSize, OptionsMenu.AccountFontSize, CloseDialog);
        confirmButton = MenuUiFactory.CloneButton(back, row, "ConfirmDeleteButton", TextConfirm, DialogButtonSize, OptionsMenu.AccountFontSize, OnConfirmClicked);

        dialog.SetActive(false);
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
