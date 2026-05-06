using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiplayerUI : MonoBehaviour
{
    [Header("Panel Invitar")]
    [SerializeField] private GameObject invitePanel;
    [SerializeField] private TMP_InputField inputFriendUsername;
    [SerializeField] private Button buttonSendInvite;
    [SerializeField] private Button buttonAddFriend;
    [SerializeField] private TMP_Text textInviteStatus;

    [Header("Panel Recibir Invitación")]
    [SerializeField] private GameObject receivePanel;
    [SerializeField] private TMP_Text textInviteFrom;
    [SerializeField] private Button buttonAccept;
    [SerializeField] private Button buttonDecline;

    [Header("Panel Solicitud de Amistad")]
    [SerializeField] private GameObject friendRequestPanel;
    [SerializeField] private TMP_Text textFriendRequestFrom;
    [SerializeField] private Button buttonAcceptFriend;
    [SerializeField] private Button buttonDeclineFriend;

    [Header("Lista Amigos Online")]
    [SerializeField] private Transform friendListContent;
    [SerializeField] private GameObject friendRowPrefab;
    [SerializeField] private TMP_Text textNoFriends;

    private string _pendingRoomId;
    private string _pendingFromUsername;
    private string _pendingFriendRequestFrom;
    private System.Threading.CancellationTokenSource _refreshCts;

    private void Start()
    {
        buttonSendInvite.onClick.AddListener(OnSendInviteClicked);
        buttonAddFriend.onClick.AddListener(OnAddFriendClicked);
        buttonAccept.onClick.AddListener(OnAcceptClicked);
        buttonDecline.onClick.AddListener(OnDeclineClicked);
        buttonAcceptFriend.onClick.AddListener(OnAcceptFriendClicked);
        buttonDeclineFriend.onClick.AddListener(OnDeclineFriendClicked);
    }

    private void OnEnable()
    {
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.OnInviteReceived += OnInviteReceived;
        NetworkManager.Instance.OnInviteWaiting += OnInviteWaiting;
        NetworkManager.Instance.OnInviteError += OnInviteError;
        NetworkManager.Instance.OnInviteDeclined += OnInviteDeclined;
        NetworkManager.Instance.OnGameStart += OnGameStart;
        NetworkManager.Instance.OnFriendRequestReceived += OnFriendRequestReceived;

        _ = LoadOnlineFriendsAsync();
        _refreshCts = new System.Threading.CancellationTokenSource();
        _ = RefreshFriendsLoopAsync(_refreshCts.Token);
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnInviteReceived -= OnInviteReceived;
        NetworkManager.Instance.OnInviteWaiting -= OnInviteWaiting;
        NetworkManager.Instance.OnInviteError -= OnInviteError;
        NetworkManager.Instance.OnInviteDeclined -= OnInviteDeclined;
        NetworkManager.Instance.OnGameStart -= OnGameStart;
        NetworkManager.Instance.OnFriendRequestReceived -= OnFriendRequestReceived;

        _refreshCts?.Cancel();
    }

    private async void ShowStatus(string message, int durationMs = 3000)
    {
        textInviteStatus.text = message;
        await System.Threading.Tasks.Task.Delay(durationMs);
        textInviteStatus.text = "";
    }

    private async System.Threading.Tasks.Task LoadOnlineFriendsAsync()
    {
        if (ApiManager.Instance == null || NetworkManager.Instance.IsGuest) return;

        foreach (Transform child in friendListContent)
        {
            if (child.gameObject != textNoFriends.gameObject)
                Destroy(child.gameObject);
        }

        var json = await ApiManager.Instance.GetOnlineFriendsAsync();
        if (string.IsNullOrEmpty(json)) return;

        var response = JsonUtility.FromJson<OnlineFriendsResponse>("{\"items\":" + json + "}");
        if (response == null || response.items == null || response.items.Length == 0)
        {
            textNoFriends.gameObject.SetActive(true);
            return;
        }

        textNoFriends.gameObject.SetActive(false);

        foreach (var username in response.items)
        {
            var row = Instantiate(friendRowPrefab, friendListContent);
            var nameText = row.GetComponentInChildren<TMP_Text>();
            if (nameText != null) nameText.text = username;

            var btn = row.GetComponentInChildren<Button>();
            if (btn != null)
            {
                string captured = username;
                btn.onClick.AddListener(async () =>
                {
                    await NetworkManager.Instance.SendInviteAsync(captured);
                });
            }
        }
    }

    private async void OnSendInviteClicked()
    {
        string toUsername = inputFriendUsername.text.Trim();
        if (string.IsNullOrEmpty(toUsername)) return;

        buttonSendInvite.interactable = false;
        ShowStatus("Enviando invitación...");
        await NetworkManager.Instance.SendInviteAsync(toUsername);
        buttonSendInvite.interactable = true;
    }

    private async void OnAddFriendClicked()
    {
        string username = inputFriendUsername.text.Trim();
        if (string.IsNullOrEmpty(username)) return;

        buttonAddFriend.interactable = false;
        ShowStatus("Enviando solicitud...");
        var result = await ApiManager.Instance.SendFriendRequestAsync(username);
        await NetworkManager.Instance.SendFriendRequestSignalRAsync(username);
        ShowStatus(string.IsNullOrEmpty(result) ? "Error al enviar solicitud." : "Solicitud enviada!");
        buttonAddFriend.interactable = true;
    }

    private void OnFriendRequestReceived(string fromUsername)
    {
        _pendingFriendRequestFrom = fromUsername;
        textFriendRequestFrom.text = $"{fromUsername} quiere ser tu amigo";
        friendRequestPanel.SetActive(true);
    }

    private async void OnAcceptFriendClicked()
    {
        friendRequestPanel.SetActive(false);
        var requester = await ApiManager.Instance.GetUserIdByUsernameAsync(_pendingFriendRequestFrom);
        if (requester != null)
            await ApiManager.Instance.AcceptFriendRequestAsync(requester.Value);
        _ = LoadOnlineFriendsAsync();
    }

    private void OnDeclineFriendClicked()
    {
        friendRequestPanel.SetActive(false);
    }

    private void OnInviteWaiting(string roomId)
    {
        _pendingRoomId = roomId;
        ShowStatus("Esperando respuesta...", 10000);
    }

    private void OnInviteError(string message)
    {
        ShowStatus(message);
        buttonSendInvite.interactable = true;
    }

    private void OnInviteDeclined()
    {
        ShowStatus("Invitación rechazada.");
        buttonSendInvite.interactable = true;
    }

    private void OnInviteReceived(string fromUsername, string roomId)
    {
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

    private async System.Threading.Tasks.Task RefreshFriendsLoopAsync(System.Threading.CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await System.Threading.Tasks.Task.Delay(10000, token).ContinueWith(_ => { });
            if (!token.IsCancellationRequested)
                _ = LoadOnlineFriendsAsync();
        }
    }
}