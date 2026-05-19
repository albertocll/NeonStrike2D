using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private string serverUrl = "https://neonstrike2d-production.up.railway.app";

    private WebSocket _ws;

    public int UserId { get; private set; }
    public string Username { get; private set; }
    public string Token { get; private set; }
    public bool IsGuest { get; private set; }
    public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

    public event Action<string, int, string> OnPlayerJoined;
    public event Action OnGameStart;
    public event Action<string> OnPlayerLeft;
    public event Action<string> OnReceiveGameState;
    public event Action<string> OnRoundEnded;

    public event Action<string, string> OnInviteReceived;
    public event Action<string> OnInviteWaiting;
    public event Action<string> OnInviteError;
    public event Action OnInviteDeclined;
    public event Action<string> OnFriendRequestReceived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _ = UnityMainThreadDispatcher.Instance;
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
        _ws = new WebSocket(serverUrl);

        _ws.OnOpen += () =>
        {
            Debug.Log("[NetworkManager] WebSocket conectado");
            SendMessage("Register", Username);
            if (roomId != null)
                SendMessage("JoinRoom", roomId, Username, GameData.SelectedCharacter);
        };

        _ws.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            UnityMainThreadDispatcher.Instance.Enqueue(() => HandleMessage(message));
        };

        _ws.OnError += (error) =>
        {
            Debug.LogError($"[NetworkManager] WebSocket error: {error}");
        };

        _ws.OnClose += (code) =>
        {
            Debug.Log($"[NetworkManager] WebSocket cerrado: {code}");
        };

        await _ws.Connect();
    }

    private void Update()
    {
        if (_ws != null)
            _ws.DispatchMessageQueue();
    }

    private void HandleMessage(string raw)
    {
        try
        {
            var msg = JsonUtility.FromJson<WsMessage>(raw);

            switch (msg.type)
            {
                case "Registered":
                    Debug.Log("[NetworkManager] Registrado en servidor");
                    break;

                case "PlayerJoined":
                    var pj = JsonUtility.FromJson<WsPlayerJoined>(raw);
                    OnPlayerJoined?.Invoke(pj.username, pj.count, pj.character);
                    break;

                case "GameStart":
                    var gs = JsonUtility.FromJson<WsGameStart>(raw);
                    string remoteUser = gs.user1 == Username ? gs.user2 : gs.user1;
                    string remoteChar = gs.user1 == Username ? gs.char2 : gs.char1;
                    GameData.RemoteUsername = remoteUser;
                    GameData.RemoteCharacter = remoteChar;
                    OnGameStart?.Invoke();
                    break;

                case "PlayerLeft":
                    OnPlayerLeft?.Invoke("opponent");
                    break;

                case "ReceiveGameState":
                    var state = JsonUtility.FromJson<WsGameState>(raw);
                    OnReceiveGameState?.Invoke(state.stateJson);
                    break;

                case "RoundEnded":
                    var re = JsonUtility.FromJson<WsRoundEnded>(raw);
                    OnRoundEnded?.Invoke(re.winner);
                    break;

                case "InviteReceived":
                    var ir = JsonUtility.FromJson<WsInviteReceived>(raw);
                    OnInviteReceived?.Invoke(ir.fromUsername, ir.roomId);
                    break;

                case "InviteWaiting":
                    var iw = JsonUtility.FromJson<WsInviteWaiting>(raw);
                    OnInviteWaiting?.Invoke(iw.roomId);
                    break;

                case "InviteError":
                    var ie = JsonUtility.FromJson<WsInviteError>(raw);
                    OnInviteError?.Invoke(ie.message);
                    break;

                case "InviteDeclined":
                    OnInviteDeclined?.Invoke();
                    break;

                case "FriendRequestReceived":
                    var fr = JsonUtility.FromJson<WsFriendRequest>(raw);
                    OnFriendRequestReceived?.Invoke(fr.fromUsername);
                    break;

                case "RoomFull":
                    Debug.LogWarning("[NetworkManager] La sala está llena");
                    break;

                default:
                    Debug.LogWarning($"[NetworkManager] Mensaje desconocido: {msg.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] Error procesando mensaje: {e.Message}\nRaw: {raw}");
        }
    }

    private async void SendMessage(string type, params string[] args)
    {
        if (!IsConnected) return;
        var msg = new WsOutgoing { type = type, args = args };
        string json = JsonUtility.ToJson(msg);
        await _ws.SendText(json);
    }

    public async Task SendInviteAsync(string toUsername)
    {
        SendMessage("SendInvite", Username, toUsername);
        await Task.CompletedTask;
    }

    public async Task SendFriendRequestSignalRAsync(string toUsername)
    {
        SendMessage("SendFriendRequest", Username, toUsername);
        await Task.CompletedTask;
    }

    public async Task AcceptInviteAsync(string roomId)
    {
        SendMessage("AcceptInvite", Username, roomId, GameData.SelectedCharacter);
        await Task.CompletedTask;
    }

    public async Task DeclineInviteAsync(string fromUsername)
    {
        SendMessage("DeclineInvite", fromUsername);
        await Task.CompletedTask;
    }

    public async Task SendGameStateAsync(string roomId, string stateJson)
    {
        SendMessage("SendGameState", roomId, stateJson);
        await Task.CompletedTask;
    }

    public async Task SendPlayerReadyAsync(string roomId, string character)
    {
        SendMessage("PlayerReady", roomId, Username, character);
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        if (_ws != null && _ws.State == WebSocketState.Open)
            await _ws.Close();
    }

    private void OnDestroy()
    {
        _ = DisconnectAsync();
    }
}

// ── DTOs WebSocket ───────────────────────────────────────────────────────────

[Serializable] public class WsMessage { public string type; }
[Serializable] public class WsOutgoing { public string type; public string[] args; }
[Serializable] public class WsPlayerJoined { public string type; public string username; public int count; public string character; }
[Serializable] public class WsGameStart { public string type; public string user1; public string char1; public string user2; public string char2; }
[Serializable] public class WsGameState { public string type; public string stateJson; }
[Serializable] public class WsRoundEnded { public string type; public string winner; }
[Serializable] public class WsInviteReceived { public string type; public string fromUsername; public string roomId; }
[Serializable] public class WsInviteWaiting { public string type; public string roomId; }
[Serializable] public class WsInviteError { public string type; public string message; }
[Serializable] public class WsFriendRequest { public string type; public string fromUsername; }