using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Botón DELETE ACCOUNT en Opciones (issue #280, requisito de Google Play). Se crea por código copiando
// BackButton, centrado debajo de él, y solo se ve con sesión iniciada. El primer toque pide confirmación;
// el segundo (antes de ConfirmSeconds) borra la cuenta en el servidor, olvida la sesión y recarga el menú.
[RequireComponent(typeof(OptionsMenu))]
public class DeleteAccountButton : MonoBehaviour
{
    private const string TextIdle = "DELETE ACCOUNT";
    private const string TextConfirm = "TAP AGAIN TO DELETE";
    private const string TextError = "ERROR, TRY AGAIN";
    private const float ConfirmSeconds = 3f;
    private const float Gap = 16f;

    private Button button;
    private TMP_Text label;
    private bool armed;
    private float resetAt;
    private bool busy;

    private void Start()
    {
        Button back = null;
        foreach (var b in GetComponent<OptionsMenu>().optionsWindow.GetComponentsInChildren<Button>(true))
            if (b.name == "BackButton") { back = b; break; }
        if (back == null)
        {
            Debug.LogWarning("[DeleteAccountButton] No se encontró BackButton; no se crea el botón de borrar cuenta.");
            enabled = false;
            return;
        }

        button = Instantiate(back, back.transform.parent);
        button.name = "DeleteAccountButton";
        button.onClick = new Button.ButtonClickedEvent(); // quita el CloseOptions copiado del botón de volver
        button.onClick.AddListener(OnClicked);

        var rect = (RectTransform)button.transform;
        var backRect = (RectTransform)back.transform;
        rect.anchoredPosition = new Vector2(0f, backRect.anchoredPosition.y - backRect.sizeDelta.y - Gap);

        label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            // Ancho para el texto más largo, así el fondo no cambia de tamaño al confirmar.
            float needed = label.GetPreferredValues(TextConfirm).x + 20f;
            if (needed > rect.sizeDelta.x) rect.sizeDelta = new Vector2(needed, rect.sizeDelta.y);
        }

        SetText(TextIdle);
        button.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (button == null) return;

        // La sesión recordada se restaura en segundo plano, así que la visibilidad se comprueba cada frame.
        bool hasSession = NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.Token);
        if (button.gameObject.activeSelf != hasSession) button.gameObject.SetActive(hasSession);

        if (!busy && resetAt > 0f && Time.unscaledTime > resetAt)
        {
            armed = false;
            resetAt = 0f;
            SetText(TextIdle);
        }
    }

    private async void OnClicked()
    {
        if (busy) return;

        if (!armed)
        {
            armed = true;
            resetAt = Time.unscaledTime + ConfirmSeconds;
            SetText(TextConfirm);
            return;
        }

        busy = true;
        button.interactable = false;
        bool deleted = await ApiManager.Instance.DeleteAccountAsync();

        if (!deleted)
        {
            busy = false;
            armed = false;
            resetAt = Time.unscaledTime + ConfirmSeconds;
            button.interactable = true;
            SetText(TextError);
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

    private void SetText(string text)
    {
        if (label != null) label.text = text;
    }
}
