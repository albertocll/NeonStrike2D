using Microsoft.AspNetCore.SignalR;

namespace NeonStrike2D.Backend.Api.Hubs;

public class GameHub : Hub
{
    // Diccionario estático que guarda las salas activas
    // Clave: roomId | Valor: lista de connectionIds de los jugadores
    private static readonly Dictionary<string, List<string>> Rooms = new();

    // Un jugador solicita unirse o crear una sala
    public async Task JoinRoom(string roomId, string username)
    {
        if (!Rooms.ContainsKey(roomId))
        {
            Rooms[roomId] = new List<string>();
        }

        var room = Rooms[roomId];

        // Máximo 2 jugadores por sala
        if (room.Count >= 2)
        {
            await Clients.Caller.SendAsync("RoomFull");
            return;
        }

        room.Add(Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Avisamos a todos en la sala de que alguien entró
        await Clients.Group(roomId).SendAsync("PlayerJoined", username, room.Count);

        // Si ya hay 2 jugadores, arrancamos la partida
        if (room.Count == 2)
        {
            await Clients.Group(roomId).SendAsync("GameStart");
        }
    }

    // Un jugador manda su estado (posición, etc.)
    public async Task SendGameState(string roomId, string stateJson)
    {
        // Reenviamos el estado a los OTROS jugadores de la sala (no al que lo mandó)
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveGameState", stateJson);
    }

    // Un jugador notifica fin de ronda
    public async Task EndRound(string roomId, string winnerUsername)
    {
        await Clients.Group(roomId).SendAsync("RoundEnded", winnerUsername);
    }

    // Gestión de desconexión
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var room in Rooms)
        {
            if (room.Value.Contains(Context.ConnectionId))
            {
                room.Value.Remove(Context.ConnectionId);
                await Clients.Group(room.Key).SendAsync("PlayerLeft");

                if (room.Value.Count == 0)
                {
                    Rooms.Remove(room.Key);
                }
                break;
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}