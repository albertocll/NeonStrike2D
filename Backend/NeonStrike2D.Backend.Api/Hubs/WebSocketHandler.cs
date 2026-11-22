using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NeonStrike2D.Backend.Api.Data;
using NeonStrike2D.Backend.Api.Security;

namespace NeonStrike2D.Backend.Api.Hubs;

public static class WebSocketHandler
{
    // Ajuste Security__RequireWebSocketAuth: con true se rechaza la conexión sin token.
    // Apagado por defecto para que la 1.0.0 (que no manda token) siga funcionando.
    public const string RequireAuthSetting = "Security:RequireWebSocketAuth";

    // Una invitación sin usar deja de valer pasado este tiempo.
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<string, WebSocket> Connections = new();
    // Conexiones con token válido: su nombre sale del token, no de los mensajes.
    private static readonly ConcurrentDictionary<string, string> AuthenticatedNames = new();
    private static readonly ConcurrentDictionary<string, string> UsernameToId = new();
    private static readonly ConcurrentDictionary<string, List<string>> Rooms = new();
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> RoomReady = new();
    // Por sala: escena del nivel siguiente y conexiones que ya la han cargado.
    private static readonly ConcurrentDictionary<string, (string Scene, HashSet<string> Loaded)> RoomLevelLoaded = new();
    // Salas creadas por SendInvite: solo pueden entrar el que invita y el invitado.
    private static readonly ConcurrentDictionary<string, Invite> Invites = new();

    private sealed record Invite(string InviterConnection, string InviteeConnection, string InviterName, DateTime CreatedAt);

    private enum TokenStatus { Missing, Invalid, Valid }

    public static async Task Handle(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var (status, username) = await AuthenticateAsync(context);
        bool requireAuth = context.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>(RequireAuthSetting);
        if (status == TokenStatus.Invalid || (status == TokenStatus.Missing && requireAuth))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var ws = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        Connections[connectionId] = ws;
        if (username != null)
            AuthenticatedNames[connectionId] = username;

        try
        {
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Se contesta al cierre para que el cliente no se quede esperando.
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    break;
                }

                var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await ProcessMessage(connectionId, raw);
            }
        }
        // Conexión cortada sin cierre (app cerrada, sin red...): se limpia igual.
        catch (WebSocketException) { }
        catch (IOException) { }
        catch (OperationCanceledException) { }
        finally
        {
            await HandleDisconnect(connectionId);
            Connections.TryRemove(connectionId, out _);
            AuthenticatedNames.TryRemove(connectionId, out _);
        }
    }

    // El cliente 1.0.1 manda el JWT del login en la cabecera Authorization (o en ?access_token=).
    // El nombre del jugador se toma de la base de datos: un token de una cuenta borrada no vale.
    private static async Task<(TokenStatus Status, string? Username)> AuthenticateAsync(HttpContext context)
    {
        string? token = null;
        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = header["Bearer ".Length..].Trim();
        else if (context.Request.Query.TryGetValue("access_token", out var queryToken))
            token = queryToken.ToString();

        if (string.IsNullOrEmpty(token)) return (TokenStatus.Missing, null);

        try
        {
            var options = context.RequestServices.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, options.TokenValidationParameters, out _);
            if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return (TokenStatus.Invalid, null);

            var db = context.RequestServices.GetRequiredService<AppDbContext>();
            var username = await db.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefaultAsync();
            return username == null ? (TokenStatus.Invalid, null) : (TokenStatus.Valid, username);
        }
        catch (Exception)
        {
            return (TokenStatus.Invalid, null);
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
                    await OnJoinRoom(connectionId, args[1], args[0], args.Length > 2 ? args[2] : "Violet");
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
                    await OnLevelComplete(connectionId, args[0], args[1]);
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

    // Nombre del jugador de una conexión: el del token si lo trae; si no (1.0.0), el que dice el mensaje.
    private static string NameOf(string connectionId, string claimed) =>
        AuthenticatedNames.TryGetValue(connectionId, out var name) ? name : claimed;

    private static bool InRoom(string connectionId, string roomId)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return false;
        lock (room) return room.Contains(connectionId);
    }

    private static async Task OnRegister(string connectionId, string claimed)
    {
        var username = NameOf(connectionId, claimed);

        // Una conexión sin token no puede quitarle el nombre a una con token que sigue conectada.
        if (!AuthenticatedNames.ContainsKey(connectionId)
            && UsernameToId.TryGetValue(username, out var owner) && owner != connectionId
            && AuthenticatedNames.ContainsKey(owner) && Connections.ContainsKey(owner))
        {
            await Send(connectionId, new { type = "RegisterRejected" });
            return;
        }

        // Cada conexión responde a un solo nombre.
        foreach (var entry in UsernameToId.Where(x => x.Value == connectionId && x.Key != username).ToList())
            UsernameToId.TryRemove(entry);

        UsernameToId[username] = connectionId;
        await Send(connectionId, new { type = "Registered" });
    }

    private static async Task OnJoinRoom(string connectionId, string roomId, string claimed, string character)
    {
        // Solo se entra en salas creadas por una invitación, y solo el que invita o el invitado.
        if (!Invites.TryGetValue(roomId, out var invite)
            || (invite.InviterConnection != connectionId && invite.InviteeConnection != connectionId))
        {
            await Send(connectionId, new { type = "JoinRejected" });
            return;
        }

        var room = Rooms.GetOrAdd(roomId, _ => new List<string>());
        int count;
        lock (room)
        {
            if (room.Contains(connectionId)) return;
            if (room.Count >= 2)
            {
                count = -1;
            }
            else
            {
                room.Add(connectionId);
                count = room.Count;
            }
        }

        if (count < 0)
        {
            await Send(connectionId, new { type = "RoomFull" });
            return;
        }

        var username = NameOf(connectionId, claimed);
        await SendToRoom(roomId, new { type = "PlayerJoined", username, count, character });
    }

    private static async Task OnPlayerReady(string connectionId, string roomId, string claimed, string character)
    {
        if (!InRoom(connectionId, roomId)) return;

        var username = NameOf(connectionId, claimed);
        var ready = RoomReady.GetOrAdd(roomId, _ => new Dictionary<string, string>());
        List<KeyValuePair<string, string>>? players = null;
        lock (ready)
        {
            ready[username] = character;
            if (ready.Count == 2)
                players = ready.ToList();
        }

        if (players != null)
        {
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

    private static async Task OnSendInvite(string connectionId, string claimedFrom, string toUsername)
    {
        var fromUsername = NameOf(connectionId, claimedFrom);

        if (!UsernameToId.TryGetValue(toUsername, out var targetId) || targetId == connectionId)
        {
            await Send(connectionId, new { type = "InviteError", message = "El jugador no está conectado.", code = ErrorCodes.PlayerOffline });
            return;
        }

        RemoveOldInvites();
        string roomId = $"{fromUsername}_{toUsername}_{DateTime.UtcNow.Ticks}";
        Invites[roomId] = new Invite(connectionId, targetId, fromUsername, DateTime.UtcNow);
        await Send(targetId, new { type = "InviteReceived", fromUsername, roomId });
        await Send(connectionId, new { type = "InviteWaiting", roomId });
    }

    // Solo el invitado puede rechazar, y solo una invitación que le hayan hecho.
    private static async Task OnDeclineInvite(string connectionId, string fromUsername)
    {
        var entry = Invites.FirstOrDefault(x => x.Value.InviteeConnection == connectionId && x.Value.InviterName == fromUsername);
        if (entry.Value == null || !Invites.TryRemove(entry.Key, out var invite)) return;

        await Send(invite.InviterConnection, new { type = "InviteDeclined" });
    }

    private static async Task OnSendGameState(string connectionId, string roomId, string stateJson)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return;

        List<string> others;
        lock (room)
        {
            if (!room.Contains(connectionId)) return;
            others = room.Where(id => id != connectionId).ToList();
        }

        foreach (var id in others)
            await Send(id, new { type = "ReceiveGameState", stateJson });
    }

    // El anfitrion avisa de que ha muerto su jefe: los dos clientes cargan el nivel siguiente.
    private static async Task OnLevelComplete(string connectionId, string roomId, string scene)
    {
        if (!InRoom(connectionId, roomId)) return;

        RoomLevelLoaded[roomId] = (scene, new HashSet<string>());
        await SendToRoom(roomId, new { type = "LoadLevel", scene });
    }

    // Igual que PlayerReady/GameStart: con el aviso de todos los de la sala se manda LevelStart.
    private static async Task OnLevelLoaded(string connectionId, string roomId, string scene)
    {
        if (!InRoom(connectionId, roomId)) return;
        if (!RoomLevelLoaded.TryGetValue(roomId, out var level) || level.Scene != scene) return;

        lock (level.Loaded)
            level.Loaded.Add(connectionId);

        await TryStartLevel(roomId);
    }

    private static async Task TryStartLevel(string roomId)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return;
        if (!RoomLevelLoaded.TryGetValue(roomId, out var level)) return;

        List<string> members;
        lock (room) members = room.ToList();

        bool allLoaded;
        lock (level.Loaded)
            allLoaded = members.All(level.Loaded.Contains);

        if (allLoaded && RoomLevelLoaded.TryRemove(roomId, out _))
            await SendToRoom(roomId, new { type = "LevelStart", scene = level.Scene });
    }

    private static async Task OnSendFriendRequest(string connectionId, string claimedFrom, string toUsername)
    {
        var fromUsername = NameOf(connectionId, claimedFrom);
        if (UsernameToId.TryGetValue(toUsername, out var targetId))
            await Send(targetId, new { type = "FriendRequestReceived", fromUsername });
    }

    private static async Task HandleDisconnect(string connectionId)
    {
        foreach (var entry in UsernameToId.Where(x => x.Value == connectionId).ToList())
            UsernameToId.TryRemove(entry);

        foreach (var entry in Invites.Where(x => (x.Value.InviterConnection == connectionId || x.Value.InviteeConnection == connectionId)
                                                 && !Rooms.ContainsKey(x.Key)).ToList())
            Invites.TryRemove(entry);

        foreach (var room in Rooms)
        {
            List<string> remaining;
            lock (room.Value)
            {
                if (!room.Value.Remove(connectionId)) continue;
                remaining = room.Value.ToList();
            }

            foreach (var id in remaining)
                await Send(id, new { type = "PlayerLeft" });

            if (remaining.Count == 0)
            {
                Rooms.TryRemove(room.Key, out _);
                RoomReady.TryRemove(room.Key, out _);
                RoomLevelLoaded.TryRemove(room.Key, out _);
                Invites.TryRemove(room.Key, out _);
            }
            else
            {
                // Si se iba a esperar al que se ha ido, el que queda empieza el nivel.
                await TryStartLevel(room.Key);
            }
            break;
        }
    }

    private static void RemoveOldInvites()
    {
        var limit = DateTime.UtcNow - InviteLifetime;
        foreach (var entry in Invites.Where(x => x.Value.CreatedAt < limit && !Rooms.ContainsKey(x.Key)).ToList())
            Invites.TryRemove(entry);
    }

    private static async Task Send(string connectionId, object message)
    {
        if (!Connections.TryGetValue(connectionId, out var ws)) return;
        if (ws.State != WebSocketState.Open) return;

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (WebSocketException) { }
    }

    private static async Task SendToRoom(string roomId, object message)
    {
        if (!Rooms.TryGetValue(roomId, out var room)) return;

        List<string> members;
        lock (room) members = room.ToList();

        foreach (var id in members)
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
        catch (ObjectDisposedException) { }
        catch (WebSocketException) { }
    }
}
