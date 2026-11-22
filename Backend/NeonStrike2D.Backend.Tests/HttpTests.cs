using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NeonStrike2D.Backend.Api.Security;

namespace NeonStrike2D.Backend.Tests;

public class HttpTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public HttpTests(ApiFactory factory) => this.factory = factory;

    private static async Task<JsonElement> BodyAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    private Task<HttpResponseMessage> RegisterAsync(string username, string email, string password) =>
        factory.ClientWithIp(ApiFactory.NewIp()).PostAsJsonAsync("/register", new { username, email, password });

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await BodyAsync(response);
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("message").GetString()));
    }

    // ── Hallazgo 4: validación del registro ─────────────────────────────────

    [Fact]
    public async Task Registro_Valido()
    {
        var response = await RegisterAsync("Valido_1", "valido1@prueba.com", "12345678");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Usuario registrado correctamente.", (await BodyAsync(response)).GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("")]
    public async Task Registro_ContrasenaCorta(string password)
    {
        var response = await RegisterAsync("Corta_" + password.Length, $"corta{password.Length}@prueba.com", password);
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, password.Length == 0 ? "PASSWORD_REQUIRED" : "INVALID_PASSWORD");
    }

    [Fact]
    public async Task Registro_ContrasenaLarga()
    {
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync("Larga128", "larga128@prueba.com", new string('a', 128))).StatusCode);
        await AssertErrorAsync(await RegisterAsync("Larga129", "larga129@prueba.com", new string('a', 129)),
            HttpStatusCode.BadRequest, "INVALID_PASSWORD");
    }

    [Theory]
    [InlineData("sin_arroba.com")]
    [InlineData("dos@@prueba.com")]
    [InlineData("sin@dominio")]
    [InlineData("con espacio@prueba.com")]
    [InlineData("puntos@prueba..com")]
    public async Task Registro_CorreoNoValido(string email)
    {
        await AssertErrorAsync(await RegisterAsync("CorreoMalo", email, "12345678"), HttpStatusCode.BadRequest, "INVALID_EMAIL");
    }

    [Fact]
    public async Task Registro_CorreoDeMasDe254()
    {
        var local = new string('a', 64);
        var email255 = local + "@" + new string('b', 255 - 64 - 1 - 4) + ".com";
        Assert.Equal(255, email255.Length);
        await AssertErrorAsync(await RegisterAsync("CorreoLargo", email255, "12345678"), HttpStatusCode.BadRequest, "INVALID_EMAIL");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("diecisiete_letras")]
    [InlineData("con-guion")]
    [InlineData("eñe")]
    public async Task Registro_UsuarioNoValido(string username)
    {
        await AssertErrorAsync(await RegisterAsync(username, "usuario@prueba.com", "12345678"), HttpStatusCode.BadRequest, "INVALID_USERNAME");
    }

    [Fact]
    public async Task Registro_Repetidos()
    {
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync("Repetido", "repetido@prueba.com", "12345678")).StatusCode);
        await AssertErrorAsync(await RegisterAsync("OtroNombre", "REPETIDO@prueba.com", "12345678"), HttpStatusCode.BadRequest, "EMAIL_TAKEN");
        await AssertErrorAsync(await RegisterAsync("repetido", "otro@prueba.com", "12345678"), HttpStatusCode.BadRequest, "USERNAME_TAKEN");
    }

    // Hash con el formato de Microsoft.AspNetCore.Identity 2.x: PBKDF2 con HMAC-SHA256 y 10 000 vueltas.
    private static string OldIdentityHash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
        var output = new byte[13 + salt.Length + subkey.Length];
        output[0] = 0x01;
        BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(1), 1);
        BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(5), 10000);
        BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(9), (uint)salt.Length);
        salt.CopyTo(output, 13);
        subkey.CopyTo(output, 13 + salt.Length);
        return Convert.ToBase64String(output);
    }

    [Fact]
    public async Task CuentaAntiguaConContrasenaCorta_SigueEntrando()
    {
        await factory.AddUserAsync("Antigua1", "abc", OldIdentityHash("abc"));
        await factory.AddUserAsync("Antigua2", "abc");

        foreach (var login in new[] { "Antigua1", "antigua2@prueba.com" })
        {
            var response = await factory.ClientWithIp(ApiFactory.NewIp()).PostAsJsonAsync("/login", new { email = login, password = "abc" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrEmpty((await BodyAsync(response)).GetProperty("token").GetString()));
        }
    }

    [Fact]
    public async Task Login_Incorrecto_DevuelveCodigoYMensaje()
    {
        await factory.AddUserAsync("Login1", "contrasena_larga");
        var response = await factory.ClientWithIp(ApiFactory.NewIp()).PostAsJsonAsync("/login", new { email = "Login1", password = "otra" });
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "BAD_CREDENTIALS");
        Assert.Equal("Credenciales incorrectas.", (await BodyAsync(response)).GetProperty("message").GetString());
    }

    // ── Hallazgo 3: límite de intentos ───────────────────────────────────────

    [Fact]
    public async Task SextoLoginEnUnMinuto_Devuelve429()
    {
        var client = factory.ClientWithIp(ApiFactory.NewIp());
        for (int i = 0; i < 5; i++)
        {
            var allowed = await client.PostAsJsonAsync("/login", new { email = "NoExiste", password = "x" });
            Assert.Equal(HttpStatusCode.BadRequest, allowed.StatusCode);
        }
        await AssertErrorAsync(await client.PostAsJsonAsync("/login", new { email = "NoExiste", password = "x" }),
            HttpStatusCode.TooManyRequests, "RATE_LIMITED");

        // Desde otra IP se sigue pudiendo entrar.
        var other = await factory.ClientWithIp(ApiFactory.NewIp()).PostAsJsonAsync("/login", new { email = "NoExiste", password = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, other.StatusCode);
    }

    [Fact]
    public async Task CuartoRegistroEnUnaHora_Devuelve429()
    {
        var client = factory.ClientWithIp(ApiFactory.NewIp());
        for (int i = 0; i < 3; i++)
        {
            var allowed = await client.PostAsJsonAsync("/register", new { username = $"Limite{i}", email = $"limite{i}@prueba.com", password = "12345678" });
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        await AssertErrorAsync(await client.PostAsJsonAsync("/register", new { username = "Limite3", email = "limite3@prueba.com", password = "12345678" }),
            HttpStatusCode.TooManyRequests, "RATE_LIMITED");
    }

    [Fact]
    public async Task EndpointPublico_61PeticionesEnUnMinuto_Devuelve429()
    {
        var client = factory.ClientWithIp(ApiFactory.NewIp());
        for (int i = 0; i < 60; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/")).StatusCode);
    }

    // ── Hallazgo 2: resultado de partida ─────────────────────────────────────

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -5)]
    [InlineData(MatchResultRules.MaxWave + 1, 0)]
    [InlineData(1, 100000)]
    [InlineData(10, int.MaxValue)]
    public async Task ResultadoFueraDeLimites_400YNoSeGuarda(int bestWave, int score)
    {
        var client = factory.ClientWithIp(ApiFactory.NewIp());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            await factory.NewUserTokenAsync($"Tramposo{Guid.NewGuid():N}"[..16]));

        var response = await client.PostAsJsonAsync("/match/result", new { bestWave, score });
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "SCORE_REJECTED");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NeonStrike2D.Backend.Api.Data.AppDbContext>();
        Assert.DoesNotContain(db.GameResults, r => r.Score == score && r.RoundsWon == bestWave);
    }

    [Fact]
    public async Task ResultadoNormal_SeGuarda()
    {
        var client = factory.ClientWithIp(ApiFactory.NewIp());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await factory.NewUserTokenAsync("Normal1"));

        var response = await client.PostAsJsonAsync("/match/result", new { bestWave = 12, score = 9876 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
