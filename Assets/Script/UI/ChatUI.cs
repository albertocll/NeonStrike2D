using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text chatWithText;
    [SerializeField] private TMP_Text messagesText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_InputField inputMessage;
    [SerializeField] private Button buttonSend;
    [SerializeField] private Button buttonClose;

    public static event Action<string> OnUnreadMessageFrom;

    // Historial en memoria por amigo, solo de esta sesion (sin persistencia).
    private readonly Dictionary<string, string> _history = new Dictionary<string, string>();
    private string _currentPartner;

    private void Awake()
    {
        panelRoot.SetActive(false);
    }

    private void Start()
    {
        buttonSend.onClick.AddListener(OnSendClicked);
        buttonClose.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnChatMessageReceived += OnChatMessageReceived;
        NetworkManager.Instance.OnChatError += OnChatError;
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnChatMessageReceived -= OnChatMessageReceived;
        NetworkManager.Instance.OnChatError -= OnChatError;
    }

    public void OpenChatWith(string username)
    {
        _currentPartner = username;
        chatWithText.text = $"Chat con {username}";
        messagesText.text = _history.TryGetValue(username, out var history) ? history : "";
        panelRoot.SetActive(true);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    public void Close()
    {
        _currentPartner = null;
        panelRoot.SetActive(false);
    }

    private async void OnSendClicked()
    {
        string message = inputMessage.text.Trim();
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(_currentPartner)) return;

        AppendHistoryLine(_currentPartner, $"Yo: {message}");
        inputMessage.text = "";
        await NetworkManager.Instance.SendChatMessageAsync(_currentPartner, message);
    }

    private void OnChatMessageReceived(string fromUsername, string message, string timestamp)
    {
        AppendHistoryLine(fromUsername, $"{fromUsername}: {message}");

        if (!(panelRoot.activeSelf && fromUsername == _currentPartner))
            OnUnreadMessageFrom?.Invoke(fromUsername);
    }

    private void OnChatError(string message)
    {
        if (string.IsNullOrEmpty(_currentPartner)) return;
        AppendHistoryLine(_currentPartner, $"[Sistema] {message}");
    }

    private void AppendHistoryLine(string username, string line)
    {
        string existing = _history.TryGetValue(username, out var history) ? history : "";
        string updated = existing + (existing.Length > 0 ? "\n" : "") + line;
        _history[username] = updated;

        if (username == _currentPartner)
        {
            messagesText.text = updated;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
