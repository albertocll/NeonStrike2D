using System;
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
        messagesText.text = "";
        panelRoot.SetActive(true);
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

        AppendLine($"Yo: {message}");
        inputMessage.text = "";
        await NetworkManager.Instance.SendChatMessageAsync(_currentPartner, message);
    }

    private void OnChatMessageReceived(string fromUsername, string message, string timestamp)
    {
        bool isOpenWithSender = panelRoot.activeSelf && fromUsername == _currentPartner;

        if (isOpenWithSender)
        {
            AppendLine($"{fromUsername}: {message}");
        }
        else
        {
            OnUnreadMessageFrom?.Invoke(fromUsername);
        }
    }

    private void OnChatError(string message)
    {
        AppendLine($"[Sistema] {message}");
    }

    private void AppendLine(string line)
    {
        messagesText.text += (messagesText.text.Length > 0 ? "\n" : "") + line;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
