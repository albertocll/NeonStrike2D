using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private string serverUrl = "https://neonstrike2d-production.up.railway.app";

    private HubConnection _connection;

    public int UserId { get; private set; }
    public string Username { get; private set; }
    public string Token { get; private set; }
    public bool IsGuest { get; private set; }
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event Action<string, int> OnPlayerJoined;
    public event Action OnGameStart;
    public event Action<string> OnPlayerLeft;
    public event Action<string> OnReceiveGameState;
    public event Action<string> OnRoundEnded;

    public event Action<string, string> OnInviteReceived;
    public event Action<string> OnInviteWaiting;
    public event Action<string> OnInviteError;
    public event Action OnInviteDeclined;

    private void Awake()
    {
        Debug.Log("[NetworkManager] Awake llamado");
        if (Instance != null && Instance != this)
        {
            Debug.Log("[NetworkManager] Instancia duplicada, destruyendo...");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[NetworkManager] Instancia creada y persistiendo");
    }

    public void SetUserData(int userId, string username, string token)
    {
        UserId = userId;
        Username = username;
        Token = token;
        IsGuest = false;
    }

    public void SetGuestData()
    {
        UserId = -1;
        Username = "Invitado_" + UnityEngine.Random.Range(1000, 9999);
        Token = null;
        IsGuest = true;
    }

    public async Task ConnectAsync(string roomId = null)
    {
        Debug.Log($"[NetworkManager] Iniciando conexión como {Username}...");

        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string, int>("PlayerJoined", (username, count) =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnPlayerJoined?.Invoke(username, count)));

        _connection.On("GameStart", () =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnGameStart?.Invoke()));

        _connection.On("PlayerLeft", () =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnPlayerLeft?.Invoke("opponent")));

        _connection.On<string>("ReceiveGameState", (stateJson) =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnReceiveGameState?.Invoke(stateJson)));

        _connection.On<string>("RoundEnded", (winner) =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnRoundEnded?.Invoke(winner)));

        _connection.On<string, string>("InviteReceived", (fromUsername, roomId) =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnInviteReceived?.Invoke(fromUsername, roomId)));

        _connection.On<string>("InviteWaiting", (roomId) =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnInviteWaiting?.Invoke(roomId)));

        _connection.On<string>("InviteError", (message) =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnInviteError?.Invoke(message)));

        _connection.On("InviteDeclined", () =>
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnInviteDeclined?.Invoke()));

        try
        {
            await _connection.StartAsync();
            await _connection.InvokeAsync("Register", Username);
            Debug.Log($"[NetworkManager] Conectado y registrado como {Username}");

            if (roomId != null)
                await _connection.InvokeAsync("JoinRoom", roomId, Username);
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] Error conectando al GameHub: {e.Message}");
        }
    }

    public async Task SendInviteAsync(string toUsername)
    {
        if (!IsConnected) return;
        await _connection.InvokeAsync("SendInvite", Username, toUsername);
    }

    public async Task AcceptInviteAsync(string roomId)
    {
        if (!IsConnected) return;
        await _connection.InvokeAsync("AcceptInvite", Username, roomId);
    }

    public async Task DeclineInviteAsync(string fromUsername)
    {
        if (!IsConnected) return;
        await _connection.InvokeAsync("DeclineInvite", fromUsername);
    }

    public async Task SendGameStateAsync(string roomId, string stateJson)
    {
        if (!IsConnected) return;
        await _connection.InvokeAsync("SendGameState", roomId, stateJson);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
            await _connection.StopAsync();
    }

    private void OnDestroy()
    {
        _ = DisconnectAsync();
    }
}