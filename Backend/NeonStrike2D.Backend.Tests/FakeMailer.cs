using System.Collections.Concurrent;
using System.Threading.Channels;
using NeonStrike2D.Backend.Api.Email;

namespace NeonStrike2D.Backend.Tests;

// Sustituye a Brevo en las pruebas: guarda los códigos enviados a cada correo.
public sealed class FakeMailer : IPasswordResetMailer
{
    public sealed record Sent(string Code, bool English);

    private readonly ConcurrentDictionary<string, Channel<Sent>> inbox = new();

    public Task SendCodeAsync(string toEmail, string code, bool english, CancellationToken cancellationToken = default)
    {
        Box(toEmail).Writer.TryWrite(new Sent(code, english));
        return Task.CompletedTask;
    }

    // Siguiente correo enviado a esa dirección; falla si no llega en 5 segundos.
    public async Task<Sent> NextAsync(string email)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return await Box(email).Reader.ReadAsync(timeout.Token);
    }

    // Si llega algún correo a esa dirección en el tiempo indicado.
    public async Task<bool> ReceivesAnyAsync(string email, TimeSpan wait)
    {
        using var timeout = new CancellationTokenSource(wait);
        try
        {
            await Box(email).Reader.WaitToReadAsync(timeout.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private Channel<Sent> Box(string email) =>
        inbox.GetOrAdd(email.Trim().ToLowerInvariant(), _ => Channel.CreateUnbounded<Sent>());
}
