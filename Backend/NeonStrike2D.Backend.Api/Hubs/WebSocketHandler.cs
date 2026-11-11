using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace NeonStrike2D.Backend.Api.Hubs;

public static class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocket> Connections = new();
    private static readonly ConcurrentDictionary<string, string> UsernameToId = new();
    private static readonly ConcurrentDictionary<string, List<string>> Rooms = new();
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> RoomReady = new();
    // Por sala: escena del nivel siguiente y conexiones que ya la han cargado.
    private static readonly ConcurrentDictionary<string, (string Scene, HashSet<string> Loaded)> RoomLevelLoaded = new();

    public static async Task Handle(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var ws = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        Connections[connectionId] = ws;

        try
        {
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await ProcessMessage(connectionId, raw);
            }
        }
        catch (WebSocketException) { }
        finally
        {
            await HandleDisconnect(connectionId);
            Connections.TryRemove(connectionId, out _);
        }
    }

    private static async Task ProcessMessage(string connectionId, string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();
            var args = root.TryGetProperty("args", out var argsEl)
                ? argsEl.EnumerateArray().Select(a => a.GetString()!).ToArray()
                : Array.Empty<string>();

            switch (type)
            {
                case "Register":
                    await OnRegister(connectionId, args[0]);
                    break;
                case "JoinRoom":
                    await OnJoinRoom(connectionId, args[0], args[1], args.Length > 2 ? args[2] : "Violet");
                    break;
                case "PlayerReady":
                    await OnPlayerReady(connectionId, args[0], args[1], args[2]);
                    break;
                case "SendInvite":
                    await OnSendInvite(connectionId, args[0], args[1]);
                    break;
                case "AcceptInvite":
                    await OnAcceptInvite(connectionId, args[0], args[1], args.Length > 2 ? args[2] : "Violet");
                    break;
                case "DeclineInvite":
                    await OnDeclineInvite(connectionId, args[0]);
                    break;
                case "SendGameState":
                    await OnSendGameState(connectionId, args[0], args[1]);
                    break;
                case "SendFriendRequest":
                    await OnSendFriendRequest(connectionId, args[0], args[1]);
                    break;
                case "LevelComplete":
                    await OnLevelComplete(args[0], args[1]);
                    break;
                case "LevelLoaded":
                    await OnLevelLoaded(connectionId, args[0], args[1]);
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[WebSocketHandler] Error procesando mensaje: {e.Message}");
        }
    }

    private static async Task OnRegister(string connectionId, string username)
    {
        UsernameToId[username] = connectionId;
        await Send(connectionId, new { type = "Registered" });
    }

    private static async Task OnJoinRoom(string connectionId, string roomId, string username, string character)
    {
        if (!Rooms.ContainsKey(roomId))
            Rooms[roomId] = new List<string>();

        var room = Rooms[roomId];

        if (room.Count >= 2)
        {
            await Send(connectionId, new { type = "RoomFull" });
            return;
        }

        room.Add(connectionId);
        await SendToRoom(roomId, new { type = "PlayerJoined", username, count = room.Count, character });
    }

    private static async Task OnPlayerReady(string connectionId, string roomId, string username, string character)
    {
        if (!RoomReady.ContainsKey(roomId))
            RoomReady[roomId] = new Dictionary<string, string>();

        RoomReady[roomId][username] = character;

        if (RoomReady[roomId].Count == 2)
        {
            var players = RoomReady[roomId].ToList();
            await SendToRoom(roomId, new
            {
                type = "GameStart",
                user1 = players[0].Key,
                char1 = players[0].Value,
                user2 = players[1].Key,
                char2 = players[1].Value
            });
            RoomReady.TryRemove(roomId, out _);
        }
    }

    private static async Task OnSendInvite(string connectionId, string fromUsername, string toUsername)
    {
        if (!UsernameToId.TryGetValue(toUsername, out var targetId))
        {
            await Send(connectionId, new { type = "InviteError", message = "El jugador no está conectado." });
            return;
        }

        string roomId = $"{fromUsername}_{toUsername}_{DateTime.UtcNow.Ticks}";
        await Send(targetId, new { type = "InviteReceived", fromUsername, roomId });
        await Send(connectionId, new { type = "InviteWaiting", roomId });
    }

    private static async Task OnAcceptInvite(string connectionId, string username, string roomId, string character)
    {
        await OnJoinRoom(connectionId, roomId, username, character);
    }

    private static async Task OnDeclineInvite(string connectionId, string fromUsername)
    {
        if (UsernameToId.TryGetValue(fromUsername, out var targetId))
            await Send(targetId, new { type = "InviteDeclined" });
    }

    private static async Task OnSendGameState(string connectionId, string roomId, string stateJson)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return;

        foreach (var id in room)
        {
            if (id != connectionId)
                await Send(id, new { type = "ReceiveGameState", stateJson });
        }
    }

    // El anfitrion avisa de que ha muerto su jefe: los dos clientes cargan el nivel siguiente.
    private static async Task OnLevelComplete(string roomId, string scene)
    {
        RoomLevelLoaded[roomId] = (scene, new HashSet<string>());
        await SendToRoom(roomId, new { type = "LoadLevel", scene });
    }

    // Igual que PlayerReady/GameStart: con el aviso de todos los de la sala se manda LevelStart.
    private static async Task OnLevelLoaded(string connectionId, string roomId, string scene)
    {
        if (!RoomLevelLoaded.TryGetValue(roomId, out var level) || level.Scene != scene) return;

        lock (level.Loaded)
            level.Loaded.Add(connectionId);

        await TryStartLevel(roomId);
    }

    private static async Task TryStartLevel(string roomId)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return;
        if (!RoomLevelLoaded.TryGetValue(roomId, out var level)) return;

        bool allLoaded;
        lock (level.Loaded)
            allLoaded = room.All(level.Loaded.Contains);

        if (allLoaded && RoomLevelLoaded.TryRemove(roomId, out _))
            await SendToRoom(roomId, new { type = "LevelStart", scene = level.Scene });
    }

    private static async Task OnSendFriendRequest(string connectionId, string fromUsername, string toUsername)
    {
        if (UsernameToId.TryGetValue(toUsername, out var targetId))
            await Send(targetId, new { type = "FriendRequestReceived", fromUsername });
    }

    private static async Task HandleDisconnect(string connectionId)
    {
        var userToRemove = UsernameToId.FirstOrDefault(x => x.Value == connectionId).Key;
        if (userToRemove != null)
            UsernameToId.TryRemove(userToRemove, out _);

        foreach (var room in Rooms)
        {
            if (room.Value.Contains(connectionId))
            {
                room.Value.Remove(connectionId);

                foreach (var id in room.Value)
                    await Send(id, new { type = "PlayerLeft" });

                if (room.Value.Count == 0)
                {
                    Rooms.TryRemove(room.Key, out _);
                    RoomReady.TryRemove(room.Key, out _);
                    RoomLevelLoaded.TryRemove(room.Key, out _);
                }
                else
                {
                    // Si se iba a esperar al que se ha ido, el que queda empieza el nivel.
                    await TryStartLevel(room.Key);
                }
                break;
            }
        }
    }

    private static async Task Send(string connectionId, object message)
    {
        if (!Connections.TryGetValue(connectionId, out var ws)) return;
        if (ws.State != WebSocketState.Open) return;

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task SendToRoom(string roomId, object message)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return;

        foreach (var id in room)
            await Send(id, message);
    }

    public static bool IsUserOnline(string username) => UsernameToId.ContainsKey(username);
    public static List<string> GetOnlineUsers() => UsernameToId.Keys.ToList();

    // Cuenta borrada (issue #280): deja de salir en línea al momento y se cierra su conexión.
    // Solo se envía el cierre: el bucle de Handle recibe la respuesta del cliente y limpia la sala.
    public static async Task DisconnectUserAsync(string username)
    {
        if (!UsernameToId.TryRemove(username, out var connectionId)) return;
        if (!Connections.TryGetValue(connectionId, out var ws) || ws.State != WebSocketState.Open) return;

        try
        {
            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Cuenta borrada", CancellationToken.None);
        }
        catch (WebSocketException) { }
    }
}