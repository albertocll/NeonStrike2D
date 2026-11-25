using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NeonStrike2D.Backend.Api.Data;
using NeonStrike2D.Backend.Api.Email;
using NeonStrike2D.Backend.Api.Security;

namespace NeonStrike2D.Backend.Tests;

// Recuperación de contraseña por código (issue #199): /password/forgot y /password/reset.
public class PasswordResetTests : IClassFixture<ApiFactory>
{
    private const string OldPassword = "contrasena_vieja";
    private const string NewPassword = "contrasena_nueva";

    private readonly ApiFactory factory;

    public PasswordResetTests(ApiFactory factory) => this.factory = factory;

    private static string NewName(string prefix) => (prefix + Guid.NewGuid().ToString("N"))[..16];

    private HttpClient Client() => factory.ClientWithIp(ApiFactory.NewIp());

    private async Task<string> NewUserEmailAsync(string prefix)
    {
        var user = await factory.AddUserAsync(NewName(prefix), OldPassword);
        return user.Email;
    }

    private Task<HttpResponseMessage> ForgotAsync(string email, string? language = null) =>
        Client().PostAsJsonAsync("/password/forgot", new { email, language });

    private Task<HttpResponseMessage> ResetAsync(string email, string code, string newPassword = NewPassword) =>
        Client().PostAsJsonAsync("/password/reset", new { email, code, newPassword });

    private async Task<string> RequestCodeAsync(string email)
    {
        Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(email)).StatusCode);
        return (await factory.Mailer.NextAsync(email)).Code;
    }

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await BodyAsync(response)).GetProperty("code").GetString());
    }

    private async Task<bool> CanLoginAsync(string email, string password)
    {
        var response = await Client().PostAsJsonAsync("/login", new { email, password });
        return response.StatusCode == HttpStatusCode.OK;
    }

    private static string WrongCode(string code) => code == "000000" ? "111111" : "000000";

    // ── Código correcto ──────────────────────────────────────────────────────

    [Fact]
    public async Task CodigoCorrecto_CambiaLaContrasena()
    {
        var email = await NewUserEmailAsync("Correcto");
        var code = await RequestCodeAsync(email);
        Assert.Matches("^[0-9]{6}$", code);

        var response = await ResetAsync(email, code);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await BodyAsync(response)).GetProperty("success").GetBoolean());

        Assert.True(await CanLoginAsync(email, NewPassword));
        Assert.False(await CanLoginAsync(email, OldPassword));
    }

    [Fact]
    public async Task CodigoCorrecto_SoloSirveUnaVez()
    {
        var email = await NewUserEmailAsync("UnaVez");
        var code = await RequestCodeAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, code)).StatusCode);

        await AssertErrorAsync(await ResetAsync(email, code, "otra_contrasena"), HttpStatusCode.BadRequest, ErrorCodes.CodeUsed);
        Assert.True(await CanLoginAsync(email, NewPassword));
    }

    [Fact]
    public async Task CorreoEnMayusculas_FuncionaIgual()
    {
        var email = await NewUserEmailAsync("Mayus");
        var code = await RequestCodeAsync(email.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(" " + email.ToUpperInvariant() + " ", code)).StatusCode);
        Assert.True(await CanLoginAsync(email, NewPassword));
    }

    [Fact]
    public async Task CodigoNuevo_InvalidaElAnterior()
    {
        var email = await NewUserEmailAsync("Nuevo");
        var first = await RequestCodeAsync(email);
        var second = await RequestCodeAsync(email);

        if (first != second)
            await AssertErrorAsync(await ResetAsync(email, first), HttpStatusCode.BadRequest, ErrorCodes.InvalidCode);
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, second)).StatusCode);
    }

    [Fact]
    public async Task CodigoGuardado_SoloComoHash()
    {
        var email = await NewUserEmailAsync("Hash");
        var code = await RequestCodeAsync(email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hash = PasswordResetRules.HashEmail(email.ToLowerInvariant());
        var entry = await db.PasswordResetCodes.SingleAsync(c => c.EmailHash == hash);
        Assert.NotEqual(code, entry.CodeHash);
        Assert.True(PasswordResetRules.VerifyCode(entry.CodeHash, code));
        Assert.DoesNotContain('@', entry.EmailHash);
    }

    [Fact]
    public async Task IdiomaIngles_EnviaElCorreoEnIngles()
    {
        var email = await NewUserEmailAsync("Ingles");
        Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(email, "en")).StatusCode);
        Assert.True((await factory.Mailer.NextAsync(email)).English);

        Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(email, "es")).StatusCode);
        Assert.False((await factory.Mailer.NextAsync(email)).English);
    }

    // ── Código incorrecto, caducado y demasiados intentos ────────────────────

    [Fact]
    public async Task CodigoIncorrecto_Devuelve400YNoCambiaLaContrasena()
    {
        var email = await NewUserEmailAsync("Incorrecto");
        var code = await RequestCodeAsync(email);

        await AssertErrorAsync(await ResetAsync(email, WrongCode(code)), HttpStatusCode.BadRequest, ErrorCodes.InvalidCode);
        await AssertErrorAsync(await ResetAsync(email, "12ab56"), HttpStatusCode.BadRequest, ErrorCodes.InvalidCode);
        Assert.True(await CanLoginAsync(email, OldPassword));

        // Tras dos fallos el código bueno sigue valiendo.
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, code)).StatusCode);
    }

    [Fact]
    public async Task CodigoCaducado_Devuelve400()
    {
        var email = await NewUserEmailAsync("Caducado");
        var code = await RequestCodeAsync(email);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hash = PasswordResetRules.HashEmail(email.ToLowerInvariant());
            var entry = await db.PasswordResetCodes.SingleAsync(c => c.EmailHash == hash);
            entry.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        await AssertErrorAsync(await ResetAsync(email, code), HttpStatusCode.BadRequest, ErrorCodes.CodeExpired);
        Assert.True(await CanLoginAsync(email, OldPassword));
    }

    [Fact]
    public async Task CodigoCaduca_A_Los15Minutos()
    {
        var email = await NewUserEmailAsync("Quince");
        var before = DateTime.UtcNow;
        await RequestCodeAsync(email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hash = PasswordResetRules.HashEmail(email.ToLowerInvariant());
        var entry = await db.PasswordResetCodes.SingleAsync(c => c.EmailHash == hash);
        Assert.Equal(TimeSpan.FromMinutes(15), entry.ExpiresAt - entry.CreatedAt);
        Assert.True(entry.CreatedAt >= before.AddSeconds(-1));
    }

    [Fact]
    public async Task DemasiadosIntentos_InvalidaElCodigo()
    {
        var email = await NewUserEmailAsync("Intentos");
        var code = await RequestCodeAsync(email);
        var wrong = WrongCode(code);

        for (int i = 1; i < PasswordResetRules.MaxAttempts; i++)
            await AssertErrorAsync(await ResetAsync(email, wrong), HttpStatusCode.BadRequest, ErrorCodes.InvalidCode);
        await AssertErrorAsync(await ResetAsync(email, wrong), HttpStatusCode.BadRequest, ErrorCodes.TooManyAttempts);

        // Ni el código bueno vale ya: hay que pedir otro.
        await AssertErrorAsync(await ResetAsync(email, code), HttpStatusCode.BadRequest, ErrorCodes.TooManyAttempts);
        Assert.True(await CanLoginAsync(email, OldPassword));

        var newCode = await RequestCodeAsync(email);
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, newCode)).StatusCode);
    }

    // ── Email inexistente: no se delata qué correos tienen cuenta ────────────

    [Fact]
    public async Task EmailInexistente_MismaRespuestaYSinCorreo()
    {
        var existing = await NewUserEmailAsync("Existe");
        var missing = NewName("NoExiste").ToLowerInvariant() + "@prueba.com";

        var withAccount = await ForgotAsync(existing);
        var withoutAccount = await ForgotAsync(missing);

        Assert.Equal(HttpStatusCode.OK, withAccount.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withoutAccount.StatusCode);
        Assert.Equal(await withAccount.Content.ReadAsStringAsync(), await withoutAccount.Content.ReadAsStringAsync());

        Assert.True(await factory.Mailer.ReceivesAnyAsync(existing, TimeSpan.FromSeconds(5)));
        Assert.False(await factory.Mailer.ReceivesAnyAsync(missing, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task EmailInexistente_ResetResponderIgualQueConCuenta()
    {
        var missing = NewName("NoExiste").ToLowerInvariant() + "@prueba.com";
        Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(missing)).StatusCode);

        // Mismos errores que una cuenta real con código equivocado: incorrecto y, al quinto, demasiados intentos.
        for (int i = 1; i < PasswordResetRules.MaxAttempts; i++)
            await AssertErrorAsync(await ResetAsync(missing, "123456"), HttpStatusCode.BadRequest, ErrorCodes.InvalidCode);
        await AssertErrorAsync(await ResetAsync(missing, "123456"), HttpStatusCode.BadRequest, ErrorCodes.TooManyAttempts);
    }

    [Fact]
    public async Task SinCodigoPedido_DevuelveCodigoIncorrecto()
    {
        var email = await NewUserEmailAsync("SinPedir");
        await AssertErrorAsync(await ResetAsync(email, "123456"), HttpStatusCode.BadRequest, ErrorCodes.InvalidCode);
    }

    // ── Validación de los datos ──────────────────────────────────────────────

    [Fact]
    public async Task ContrasenaNueva_ConLasReglasDelRegistro()
    {
        var email = await NewUserEmailAsync("Reglas");
        var code = await RequestCodeAsync(email);

        await AssertErrorAsync(await ResetAsync(email, code, "corta"), HttpStatusCode.BadRequest, ErrorCodes.InvalidPassword);
        await AssertErrorAsync(await ResetAsync(email, code, new string('a', 129)), HttpStatusCode.BadRequest, ErrorCodes.InvalidPassword);
        await AssertErrorAsync(await ResetAsync(email, code, ""), HttpStatusCode.BadRequest, ErrorCodes.PasswordRequired);

        // Una contraseña que no cumple no gasta intentos: el código sigue valiendo.
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, code)).StatusCode);
    }

    [Fact]
    public async Task DatosVacios_Devuelven400ConSuCodigo()
    {
        await AssertErrorAsync(await ForgotAsync(""), HttpStatusCode.BadRequest, ErrorCodes.EmailRequired);
        await AssertErrorAsync(await ForgotAsync("no_es_un_correo"), HttpStatusCode.BadRequest, ErrorCodes.InvalidEmail);
        await AssertErrorAsync(await ResetAsync("", "123456"), HttpStatusCode.BadRequest, ErrorCodes.EmailRequired);
        await AssertErrorAsync(await ResetAsync("a@prueba.com", ""), HttpStatusCode.BadRequest, ErrorCodes.CodeRequired);
    }

    // ── Límites de peticiones ────────────────────────────────────────────────

    [Fact]
    public async Task Forgot_CuartoEnUnaHoraParaElMismoCorreo_Devuelve429()
    {
        var email = NewName("Limite").ToLowerInvariant() + "@prueba.com";
        for (int i = 0; i < PasswordResetLimiter.ForgotPerEmail; i++)
            Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(email)).StatusCode);

        // Desde otra IP también: el límite es por correo.
        await AssertErrorAsync(await ForgotAsync(email), HttpStatusCode.TooManyRequests, ErrorCodes.RateLimited);
        Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(NewName("Otro").ToLowerInvariant() + "@prueba.com")).StatusCode);
    }

    [Fact]
    public async Task Forgot_UndecimoEnUnaHoraDesdeLaMismaIp_Devuelve429()
    {
        var client = Client();
        for (int i = 0; i < 10; i++)
        {
            var email = NewName("Ip").ToLowerInvariant() + "@prueba.com";
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/password/forgot", new { email })).StatusCode);
        }
        await AssertErrorAsync(await client.PostAsJsonAsync("/password/forgot", new { email = "otro@prueba.com" }),
            HttpStatusCode.TooManyRequests, ErrorCodes.RateLimited);
    }

    [Fact]
    public async Task Reset_UndecimoEnUnaHoraParaElMismoCorreo_Devuelve429()
    {
        var email = NewName("LimiteReset").ToLowerInvariant() + "@prueba.com";
        for (int i = 0; i < PasswordResetLimiter.ResetPerEmail; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(email, "123456")).StatusCode);
        await AssertErrorAsync(await ResetAsync(email, "123456"), HttpStatusCode.TooManyRequests, ErrorCodes.RateLimited);
    }

    // ── Brevo sin configurar ─────────────────────────────────────────────────

    [Fact]
    public async Task Brevo_SinClave_NoFallaYNoLlamaALaApi()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var mailer = new BrevoPasswordResetMailer(new ThrowingHttpClientFactory(), config, NullLogger<BrevoPasswordResetMailer>.Instance);
        await mailer.SendCodeAsync("alguien@prueba.com", "123456", english: false);
    }

    [Fact]
    public void Correo_LlevaElCodigoYLaCaducidadEnSuIdioma()
    {
        var es = PasswordResetEmail.Build("042137", english: false);
        var en = PasswordResetEmail.Build("042137", english: true);

        Assert.Contains("042137", es.Text);
        Assert.Contains("042137", es.Html);
        Assert.Contains("15 minutos", es.Text);
        Assert.Contains("042137", en.Text);
        Assert.Contains("15 minutes", en.Text);
        Assert.NotEqual(es.Subject, en.Subject);
    }

    private sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("No debería llamar a Brevo sin clave.");
    }
}
