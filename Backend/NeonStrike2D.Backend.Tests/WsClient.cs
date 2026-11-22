using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

namespace NeonStrike2D.Backend.Tests;

// Cliente WebSocket de prueba que habla como NetworkManager: {"type": ..., "args": [...]}.
public sealed class WsClient : IAsyncDisposable
{
    private readonly WebSocket socket;

    private WsClient(WebSocket socket) => this.socket = socket;

    public static async Task<WsClient> ConnectAsync(ApiFactory factory, string? token = null)
    {
        var client = factory.Server.CreateWebSocketClient();
        var ip = ApiFactory.NewIp();
        client.ConfigureRequest = request =>
        {
            request.Headers["X-Real-IP"] = ip;
            if (token != null) request.Headers["Authorization"] = $"Bearer {token}";
        };
        var socket = await client.ConnectAsync(new Uri(factory.Server.BaseAddress, "ws"), CancellationToken.None);
        return new WsClient(socket);
    }

    public Task SendAsync(string type, params string[] args)
    {
        var json = JsonSerializer.Serialize(new { type, args });
        return socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task<JsonElement> ReceiveAsync()
    {
        var buffer = new byte[8192];
        var receive = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        if (await Task.WhenAny(receive, Task.Delay(TimeSpan.FromSeconds(5))) != receive)
            throw new TimeoutException("No ha llegado ningún mensaje del servidor.");
        var result = await receive;
        return JsonDocument.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count)).RootElement.Clone();
    }

    public async Task<JsonElement> ReceiveTypeAsync(string expectedType)
    {
        var message = await ReceiveAsync();
        Assert.Equal(expectedType, message.GetProperty("type").GetString());
        return message;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "fin", timeout.Token);
            }
        }
        catch (Exception) { }
        socket.Dispose();
    }
}
