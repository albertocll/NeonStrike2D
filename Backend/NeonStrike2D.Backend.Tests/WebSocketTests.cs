using Microsoft.Extensions.DependencyInjection;
using NeonStrike2D.Backend.Api.Data;
using NeonStrike2D.Backend.Api.Hubs;

namespace NeonStrike2D.Backend.Tests;

// Hallazgo 1: WebSocket con y sin token en los dos modos del ajuste, y salas ajenas.
public class WebSocketTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public WebSocketTests(ApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task SinToken_ConAjusteApagado_FuncionaComoLa100()
    {
        await using var ws = await WsClient.ConnectAsync(factory);
        await ws.SendAsync("Register", "Jugador100");
        await ws.ReceiveTypeAsync("Registered");
        Assert.True(WebSocketHandler.IsUserOnline("Jugador100"));
    }

    [Fact]
    public async Task ConToken_ConAjusteApagado_ElNombreSaleDelToken()
    {
        var token = await factory.NewUserTokenAsync("Titular1");

        await using var ws = await WsClient.ConnectAsync(factory, token);
        await ws.SendAsync("Register", "Suplantado1");
        await ws.ReceiveTypeAsync("Registered");

        Assert.True(WebSocketHandler.IsUserOnline("Titular1"));
        Assert.False(WebSocketHandler.IsUserOnline("Suplantado1"));
    }

    [Fact]
    public async Task TokenInvalido_SeRechazaAunqueElAjusteEsteApagado()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => WsClient.ConnectAsync(factory, "token.que.no.vale"));
    }

    [Fact]
    public async Task SinToken_NoPuedeQuitarleElNombreAUnaConexionConToken()
    {
        var token = await factory.NewUserTokenAsync("Titular2");
        await using var owner = await WsClient.ConnectAsync(factory, token);
        await owner.SendAsync("Register", "x");
        await owner.ReceiveTypeAsync("Registered");

        await using var intruder = await WsClient.ConnectAsync(factory);
        await intruder.SendAsync("Register", "Titular2");
        await intruder.ReceiveTypeAsync("RegisterRejected");

        // Las invitaciones a Titular2 le siguen llegando al titular.
        await using var friend = await WsClient.ConnectAsync(factory);
        await friend.SendAsync("Register", "Amigo2");
        await friend.ReceiveTypeAsync("Registered");
        await friend.SendAsync("SendInvite", "Amigo2", "Titular2");
        var invite = await owner.ReceiveTypeAsync("InviteReceived");
        Assert.Equal("Amigo2", invite.GetProperty("fromUsername").GetString());
    }

    // Invitación y sala de dos jugadores como en la 1.0.0 (sin token).
    private async Task<(WsClient Host, WsClient Guest, string RoomId)> CreateRoomAsync(string hostName, string guestName)
    {
        var host = await WsClient.ConnectAsync(factory);
        await host.SendAsync("Register", hostName);
        await host.ReceiveTypeAsync("Registered");
        var guest = await WsClient.ConnectAsync(factory);
        await guest.SendAsync("Register", guestName);
        await guest.ReceiveTypeAsync("Registered");

        await host.SendAsync("SendInvite", hostName, guestName);
        var received = await guest.ReceiveTypeAsync("InviteReceived");
        var waiting = await host.ReceiveTypeAsync("InviteWaiting");
        var roomId = waiting.GetProperty("roomId").GetString()!;
        Assert.Equal(roomId, received.GetProperty("roomId").GetString());

        await host.SendAsync("JoinRoom", roomId, hostName, "Violet");
        await host.ReceiveTypeAsync("PlayerJoined");
        await guest.SendAsync("AcceptInvite", guestName, roomId, "Violet");
        var joined = await guest.ReceiveTypeAsync("PlayerJoined");
        Assert.Equal(2, joined.GetProperty("count").GetInt32());
        await host.ReceiveTypeAsync("PlayerJoined");
        return (host, guest, roomId);
    }

    [Fact]
    public async Task FlujoDeInvitacionDeLa100_SigueFuncionando()
    {
        var (host, guest, roomId) = await CreateRoomAsync("Anfitrion3", "Invitado3");
        await using var _ = host;
        await using var __ = guest;

        await host.SendAsync("PlayerReady", roomId, "Anfitrion3", "Violet");
        await guest.SendAsync("PlayerReady", roomId, "Invitado3", "Violet");
        await host.ReceiveTypeAsync("GameStart");
        await guest.ReceiveTypeAsync("GameStart");

        await host.SendAsync("SendGameState", roomId, "{\"x\":1}");
        var state = await guest.ReceiveTypeAsync("ReceiveGameState");
        Assert.Equal("{\"x\":1}", state.GetProperty("stateJson").GetString());

        await host.SendAsync("LevelComplete", roomId, "TheGrid");
        await host.ReceiveTypeAsync("LoadLevel");
        await guest.ReceiveTypeAsync("LoadLevel");
        await host.SendAsync("LevelLoaded", roomId, "TheGrid");
        await guest.SendAsync("LevelLoaded", roomId, "TheGrid");
        await host.ReceiveTypeAsync("LevelStart");
        await guest.ReceiveTypeAsync("LevelStart");
    }

    [Fact]
    public async Task SalaAjena_NoSePuedeOcuparNiEscribirEnElla()
    {
        var (host, guest, roomId) = await CreateRoomAsync("Anfitrion4", "Invitado4");
        await using var _ = host;
        await using var __ = guest;

        await using var intruder = await WsClient.ConnectAsync(factory);
        await intruder.SendAsync("Register", "Intruso4");
        await intruder.ReceiveTypeAsync("Registered");

        await intruder.SendAsync("JoinRoom", roomId, "Intruso4", "Violet");
        await intruder.ReceiveTypeAsync("JoinRejected");
        await intruder.SendAsync("AcceptInvite", "Intruso4", roomId, "Violet");
        await intruder.ReceiveTypeAsync("JoinRejected");

        // Nada de lo que mande el intruso llega a la sala: lo primero que recibe el invitado es lo del anfitrión.
        await intruder.SendAsync("SendGameState", roomId, "{\"trampa\":1}");
        await intruder.SendAsync("LevelComplete", roomId, "Sector7");
        await intruder.SendAsync("PlayerReady", roomId, "Intruso4", "Violet");
        await host.SendAsync("SendGameState", roomId, "{\"bueno\":1}");
        var state = await guest.ReceiveTypeAsync("ReceiveGameState");
        Assert.Equal("{\"bueno\":1}", state.GetProperty("stateJson").GetString());
    }

    [Fact]
    public async Task SalaInventada_NoSePuedeCrear()
    {
        await using var ws = await WsClient.ConnectAsync(factory);
        await ws.SendAsync("Register", "Solitario5");
        await ws.ReceiveTypeAsync("Registered");
        await ws.SendAsync("JoinRoom", "sala_inventada", "Solitario5", "Violet");
        await ws.ReceiveTypeAsync("JoinRejected");
    }

    [Fact]
    public async Task RechazarInvitacion_SoloElInvitado()
    {
        await using var host = await WsClient.ConnectAsync(factory);
        await host.SendAsync("Register", "Anfitrion6");
        await host.ReceiveTypeAsync("Registered");
        await using var guest = await WsClient.ConnectAsync(factory);
        await guest.SendAsync("Register", "Invitado6");
        await guest.ReceiveTypeAsync("Registered");
        await using var other = await WsClient.ConnectAsync(factory);
        await other.SendAsync("Register", "Otro6");
        await other.ReceiveTypeAsync("Registered");

        await host.SendAsync("SendInvite", "Anfitrion6", "Invitado6");
        await guest.ReceiveTypeAsync("InviteReceived");
        await host.ReceiveTypeAsync("InviteWaiting");

        // Un tercero no puede rechazar por el invitado: lo siguiente que recibe el anfitrión es el
        // error de su segunda invitación, y después el rechazo del invitado.
        await other.SendAsync("DeclineInvite", "Anfitrion6");
        await Task.Delay(200);
        await host.SendAsync("SendInvite", "Anfitrion6", "NadieConectado6");
        await host.ReceiveTypeAsync("InviteError");
        await guest.SendAsync("DeclineInvite", "Anfitrion6");
        await host.ReceiveTypeAsync("InviteDeclined");
    }

    [Fact]
    public async Task InvitacionAJugadorDesconectado_DevuelveCodigoYMensaje()
    {
        await using var ws = await WsClient.ConnectAsync(factory);
        await ws.SendAsync("Register", "Solitario7");
        await ws.ReceiveTypeAsync("Registered");
        await ws.SendAsync("SendInvite", "Solitario7", "NadieConectado7");
        var error = await ws.ReceiveTypeAsync("InviteError");
        Assert.Equal("PLAYER_OFFLINE", error.GetProperty("code").GetString());
        Assert.Equal("El jugador no está conectado.", error.GetProperty("message").GetString());
    }
}

// Ajuste Security__RequireWebSocketAuth encendido.
public class WebSocketAuthRequiredTests : IClassFixture<ApiFactoryWithWebSocketAuth>
{
    private readonly ApiFactoryWithWebSocketAuth factory;

    public WebSocketAuthRequiredTests(ApiFactoryWithWebSocketAuth factory) => this.factory = factory;

    [Fact]
    public async Task SinToken_SeRechaza()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => WsClient.ConnectAsync(factory));
    }

    [Fact]
    public async Task TokenInvalido_SeRechaza()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => WsClient.ConnectAsync(factory, "token.que.no.vale"));
    }

    [Fact]
    public async Task ConToken_Conecta()
    {
        var token = await factory.NewUserTokenAsync("ConToken8");
        await using var ws = await WsClient.ConnectAsync(factory, token);
        await ws.SendAsync("Register", "ConToken8");
        await ws.ReceiveTypeAsync("Registered");
        Assert.True(WebSocketHandler.IsUserOnline("ConToken8"));
    }

    [Fact]
    public async Task TokenDeCuentaBorrada_SeRechaza()
    {
        var token = await factory.NewUserTokenAsync("Borrada9");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Remove(db.Users.Single(u => u.Username == "Borrada9"));
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAnyAsync<Exception>(() => WsClient.ConnectAsync(factory, token));
    }
}
