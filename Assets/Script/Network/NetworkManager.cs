using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private string serverUrl = "http://192.168.1.26:5036";

    private HubConnection _connection;

    // Datos del jugador logueado
    public int UserId { get; private set; }
    public string Username { get; private set; }
    public string Token { get; private set; }
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    // Eventos que otros scripts pueden escuchar
    public event Action<string, int> OnPlayerJoined;
    public event Action OnGameStart;
    public event Action<string> OnPlayerLeft;
    public event Action<string> OnReceiveGameState;
    public event Action<string> OnRoundEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Guardar datos del login
    public void SetUserData(int userId, string username, string token)
    {
        UserId = userId;
        Username = username;
        Token = token;
    }

    // Conectar al GameHub
    public async Task ConnectAsync(string roomId)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        // Escuchar eventos del servidor
        _connection.On<string, int>("PlayerJoined", (username, count) =>
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnPlayerJoined?.Invoke(username, count));
        });

        _connection.On("GameStart", () =>
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnGameStart?.Invoke());
        });

        _connection.On("PlayerLeft", () =>
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnPlayerLeft?.Invoke("opponent"));
        });

        _connection.On<string>("ReceiveGameState", (stateJson) =>
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnReceiveGameState?.Invoke(stateJson));
        });

        _connection.On<string>("RoundEnded", (winner) =>
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                OnRoundEnded?.Invoke(winner));
        });

        try
        {
            await _connection.StartAsync();
            await _connection.InvokeAsync("JoinRoom", roomId, Username);
            Debug.Log($"Conectado al GameHub en sala {roomId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error conectando al GameHub: {e.Message}");
        }
    }

    // Enviar estado del juego
    public async Task SendGameStateAsync(string roomId, string stateJson)
    {
        if (!IsConnected) return;
        await _connection.InvokeAsync("SendGameState", roomId, stateJson);
    }

    // Desconectar
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