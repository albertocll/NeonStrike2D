using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerUI : MonoBehaviour
{
    [Header("Panel Invitar")]
    [SerializeField] private GameObject invitePanel;
    [SerializeField] private TMP_InputField inputFriendUsername;
    [SerializeField] private Button buttonSendInvite;
    [SerializeField] private TMP_Text textInviteStatus;

    [Header("Panel Recibir Invitación")]
    [SerializeField] private GameObject receivePanel;
    [SerializeField] private TMP_Text textInviteFrom;
    [SerializeField] private Button buttonAccept;
    [SerializeField] private Button buttonDecline;

    private string _pendingRoomId;
    private string _pendingFromUsername;

    private void Start()
    {
        buttonSendInvite.onClick.AddListener(OnSendInviteClicked);
        buttonAccept.onClick.AddListener(OnAcceptClicked);
        buttonDecline.onClick.AddListener(OnDeclineClicked);

        NetworkManager.Instance.OnInviteReceived += OnInviteReceived;
        NetworkManager.Instance.OnInviteWaiting += OnInviteWaiting;
        NetworkManager.Instance.OnInviteError += OnInviteError;
        NetworkManager.Instance.OnInviteDeclined += OnInviteDeclined;
        NetworkManager.Instance.OnGameStart += OnGameStart;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnInviteReceived -= OnInviteReceived;
        NetworkManager.Instance.OnInviteWaiting -= OnInviteWaiting;
        NetworkManager.Instance.OnInviteError -= OnInviteError;
        NetworkManager.Instance.OnInviteDeclined -= OnInviteDeclined;
        NetworkManager.Instance.OnGameStart -= OnGameStart;
    }

    private async void OnSendInviteClicked()
    {
        string toUsername = inputFriendUsername.text.Trim();
        if (string.IsNullOrEmpty(toUsername)) return;

        buttonSendInvite.interactable = false;
        textInviteStatus.text = "Enviando invitación...";
        await NetworkManager.Instance.SendInviteAsync(toUsername);
    }

    private void OnInviteWaiting(string roomId)
    {
        _pendingRoomId = roomId;
        textInviteStatus.text = "Esperando respuesta...";
    }

    private void OnInviteError(string message)
    {
        textInviteStatus.text = message;
        buttonSendInvite.interactable = true;
    }

    private void OnInviteDeclined()
    {
        textInviteStatus.text = "Invitación rechazada.";
        buttonSendInvite.interactable = true;
    }

    private void OnInviteReceived(string fromUsername, string roomId)
    {
        Debug.Log($"[MultiplayerUI] Invitación recibida de {fromUsername}, roomId: {roomId}");
        _pendingFromUsername = fromUsername;
        _pendingRoomId = roomId;
        textInviteFrom.text = $"{fromUsername} te ha invitado a jugar";
        receivePanel.SetActive(true);
    }

    private async void OnAcceptClicked()
    {
        receivePanel.SetActive(false);
        await NetworkManager.Instance.AcceptInviteAsync(_pendingRoomId);
    }

    private async void OnDeclineClicked()
    {
        receivePanel.SetActive(false);
        await NetworkManager.Instance.DeclineInviteAsync(_pendingFromUsername);
    }

    private void OnGameStart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
    }
}