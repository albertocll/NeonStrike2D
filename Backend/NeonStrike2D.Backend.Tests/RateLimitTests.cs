using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NeonStrike2D.Backend.Api.Security;

namespace NeonStrike2D.Backend.Tests;

// Límites de peticiones: por usuario en los endpoints con sesión y por IP (la última de
// X-Forwarded-For) en los públicos.
public class RateLimitTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public RateLimitTests(ApiFactory factory) => this.factory = factory;

    private static string NewName(string prefix) => (prefix + Guid.NewGuid().ToString("N"))[..16];

    private HttpClient ClientWithToken(string token)
    {
        var client = factory.ClientWithIp(ApiFactory.NewIp());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task AssertRateLimitedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("RATE_LIMITED", body.GetProperty("code").GetString());
    }

    private static Task<HttpResponseMessage> SendResultAsync(HttpClient client) =>
        client.PostAsJsonAsync("/match/result", new { bestWave = 3, score = 500 });

    // ── Endpoints con sesión: por usuario ────────────────────────────────────

    [Fact]
    public async Task ResultadoDePartida_UndecimoEnUnMinuto_Devuelve429()
    {
        var client = ClientWithToken(await factory.NewUserTokenAsync(NewName("Partida")));
        for (int i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.OK, (await SendResultAsync(client)).StatusCode);
        await AssertRateLimitedAsync(await SendResultAsync(client));

        // Otro jugador no se ve afectado.
        var other = ClientWithToken(await factory.NewUserTokenAsync(NewName("Otro")));
        Assert.Equal(HttpStatusCode.OK, (await SendResultAsync(other)).StatusCode);
    }

    [Fact]
    public async Task ResultadoDePartida_ElLimiteEsPorUsuarioNoPorIp()
    {
        // El mismo jugador cambiando de IP en cada petición sigue limitado.
        var token = await factory.NewUserTokenAsync(NewName("CambiaIp"));
        for (int i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.OK, (await SendResultAsync(ClientWithToken(token))).StatusCode);
        await AssertRateLimitedAsync(await SendResultAsync(ClientWithToken(token)));
    }

    [Fact]
    public async Task EndpointConSesion_121EnUnMinuto_Devuelve429()
    {
        var client = ClientWithToken(await factory.NewUserTokenAsync(NewName("Sesion")));
        for (int i = 0; i < 120; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/me")).StatusCode);
        await AssertRateLimitedAsync(await client.GetAsync("/friends/online"));

        var other = ClientWithToken(await factory.NewUserTokenAsync(NewName("Otro")));
        Assert.Equal(HttpStatusCode.OK, (await other.GetAsync("/me")).StatusCode);
    }

    [Fact]
    public async Task EndpointConSesion_SinToken_Devuelve401()
    {
        var response = await factory.ClientWithIp(ApiFactory.NewIp()).GetAsync("/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── IP del cliente ───────────────────────────────────────────────────────

    private static HttpContext Context(string? forwardedFor, string? realIp)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("100.64.0.1");
        if (forwardedFor != null) context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        if (realIp != null) context.Request.Headers["X-Real-IP"] = realIp;
        return context;
    }

    [Theory]
    [InlineData("1.1.1.1", null, "1.1.1.1")]
    [InlineData("6.6.6.6, 7.7.7.7, 203.0.113.9", null, "203.0.113.9")]
    [InlineData("6.6.6.6,203.0.113.9 ", "8.8.8.8", "203.0.113.9")]
    [InlineData(null, "198.51.100.4", "198.51.100.4")]
    [InlineData("", "198.51.100.4", "198.51.100.4")]
    [InlineData(null, null, "100.64.0.1")]
    public void IpDelCliente(string? forwardedFor, string? realIp, string expected)
    {
        Assert.Equal(expected, RateLimits.ClientIp(Context(forwardedFor, realIp)));
    }

    private HttpClient ClientWithHeaders(string forwardedFor, string? realIp)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        if (realIp != null) client.DefaultRequestHeaders.Add("X-Real-IP", realIp);
        return client;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client) =>
        client.PostAsJsonAsync("/login", new { email = "NoExiste", password = "x" });

    [Fact]
    public async Task VariasIpsEnXForwardedFor_CuentaLaUltima()
    {
        // El atacante cambia las IPs que pone delante; la que añade el proxy (la última) es la misma.
        var realIp = ApiFactory.NewIp();
        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.BadRequest,
                (await LoginAsync(ClientWithHeaders($"{ApiFactory.NewIp()}, {ApiFactory.NewIp()}, {realIp}", null))).StatusCode);
        await AssertRateLimitedAsync(await LoginAsync(ClientWithHeaders($"{ApiFactory.NewIp()}, {realIp}", null)));
    }

    [Fact]
    public async Task XRealIpFalsa_NoSaltaElLimite()
    {
        // Con X-Forwarded-For presente, una X-Real-IP distinta en cada petición no cuenta.
        var realIp = ApiFactory.NewIp();
        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.BadRequest,
                (await LoginAsync(ClientWithHeaders(realIp, ApiFactory.NewIp()))).StatusCode);
        await AssertRateLimitedAsync(await LoginAsync(ClientWithHeaders(realIp, ApiFactory.NewIp())));
    }
}
