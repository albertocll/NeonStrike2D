using Microsoft.AspNetCore.SignalR;

namespace NeonStrike2D.Backend.Api.Hubs;

public class GameHub : Hub
{
    private static readonly Dictionary<string, List<string>> Rooms = new();
    private static readonly Dictionary<string, Dictionary<string, string>> RoomReady = new();
    public static readonly Dictionary<string, string> ConnectedUsers = new();

    public async Task Register(string username)
    {
        ConnectedUsers[username] = Context.ConnectionId;
        await Clients.Caller.SendAsync("Registered");
    }

    public async Task JoinRoom(string roomId, string username, string character = "Violet")
    {
        if (!Rooms.ContainsKey(roomId))
            Rooms[roomId] = new List<string>();

        var room = Rooms[roomId];

        if (room.Count >= 2)
        {
            await Clients.Caller.SendAsync("RoomFull");
            return;
        }

        room.Add(Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync("PlayerJoined", username, room.Count, character);
    }

    public async Task PlayerReady(string roomId, string username, string character)
    {
        if (!RoomReady.ContainsKey(roomId))
            RoomReady[roomId] = new Dictionary<string, string>();

        RoomReady[roomId][username] = character;

        if (RoomReady[roomId].Count == 2)
        {
            var players = RoomReady[roomId].ToList();
            await Clients.Group(roomId).SendAsync(
                "GameStart",
                players[0].Key, players[0].Value,
                players[1].Key, players[1].Value
            );
            RoomReady.Remove(roomId);
        }
    }

    public async Task SendFriendRequest(string fromUsername, string toUsername)
    {
        if (!ConnectedUsers.TryGetValue(toUsername, out var targetConnectionId))
            return;

        await Clients.Client(targetConnectionId).SendAsync("FriendRequestReceived", fromUsername);
    }

    public async Task SendInvite(string fromUsername, string toUsername)
    {
        if (!ConnectedUsers.TryGetValue(toUsername, out var targetConnectionId))
        {
            await Clients.Caller.SendAsync("InviteError", "El jugador no está conectado.");
            return;
        }

        string roomId = $"{fromUsername}_{toUsername}_{DateTime.UtcNow.Ticks}";
        await Clients.Client(targetConnectionId).SendAsync("InviteReceived", fromUsername, roomId);
        await Clients.Caller.SendAsync("InviteWaiting", roomId);
    }

    public async Task AcceptInvite(string username, string roomId, string character = "Violet")
    {
        await JoinRoom(roomId, username, character);
    }

    public async Task DeclineInvite(string fromUsername)
    {
        if (ConnectedUsers.TryGetValue(fromUsername, out var connectionId))
            await Clients.Client(connectionId).SendAsync("InviteDeclined");
    }

    public async Task SendGameState(string roomId, string stateJson)
    {
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveGameState", stateJson);
    }

    public async Task EndRound(string roomId, string winnerUsername)
    {
        await Clients.Group(roomId).SendAsync("RoundEnded", winnerUsername);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userToRemove = ConnectedUsers.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (userToRemove != null)
            ConnectedUsers.Remove(userToRemove);

        foreach (var room in Rooms)
        {
            if (room.Value.Contains(Context.ConnectionId))
            {
                room.Value.Remove(Context.ConnectionId);
                await Clients.Group(room.Key).SendAsync("PlayerLeft");

                if (room.Value.Count == 0)
                {
                    Rooms.Remove(room.Key);
                    RoomReady.Remove(room.Key);
                }
                break;
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}