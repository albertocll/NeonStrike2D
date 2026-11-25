using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NeonStrike2D.Backend.Api.Email;

// Envío del correo con el código de recuperación (issue #199).
public interface IPasswordResetMailer
{
    Task SendCodeAsync(string toEmail, string code, bool english, CancellationToken cancellationToken = default);
}

// Envío con la API transaccional de Brevo. La clave (Brevo__ApiKey) y el remitente
// (Brevo__SenderEmail) salen de la configuración del servidor, nunca del repositorio. Si faltan,
// se registra el error y no se envía nada; la petición del jugador no falla por eso.
public sealed class BrevoPasswordResetMailer : IPasswordResetMailer
{
    public const string HttpClientName = "brevo";
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";

    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;
    private readonly ILogger<BrevoPasswordResetMailer> logger;

    public BrevoPasswordResetMailer(IHttpClientFactory httpClientFactory, IConfiguration configuration,
        ILogger<BrevoPasswordResetMailer> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task SendCodeAsync(string toEmail, string code, bool english, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["Brevo:ApiKey"];
        var senderEmail = configuration["Brevo:SenderEmail"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(senderEmail))
        {
            logger.LogError("Falta Brevo:ApiKey o Brevo:SenderEmail en la configuración: no se envía el correo de recuperación.");
            return;
        }

        var senderName = configuration["Brevo:SenderName"];
        if (string.IsNullOrWhiteSpace(senderName)) senderName = "NeonStrike2D";

        var mail = PasswordResetEmail.Build(code, english);
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent.Create(new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = toEmail } },
            subject = mail.Subject,
            htmlContent = mail.Html,
            textContent = mail.Text
        });

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        // Ni el correo ni el código van al registro.
        if (!response.IsSuccessStatusCode)
            logger.LogError("Brevo respondió {Status} al enviar un correo de recuperación.", (int)response.StatusCode);
    }
}
