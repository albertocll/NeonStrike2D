using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NeonStrike2D.Backend.Api.Data;
using NeonStrike2D.Backend.Api.Models;

namespace NeonStrike2D.Backend.Tests;

// Servidor de pruebas con base de datos en memoria y una clave JWT de prueba.
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool requireWebSocketAuth;
    private readonly string databaseName = "pruebas_" + Guid.NewGuid();
    private static int nextIp;

    public ApiFactory() : this(false) { }

    protected ApiFactory(bool requireWebSocketAuth)
    {
        this.requireWebSocketAuth = requireWebSocketAuth;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", "clave_de_prueba_solo_para_tests_0123456789abcdef");
        builder.UseSetting("Jwt:Issuer", "NeonStrike2DPruebas");
        builder.UseSetting("Jwt:Audience", "NeonStrike2DPruebas");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=no_se_usa");
        builder.UseSetting("Security:RequireWebSocketAuth", requireWebSocketAuth ? "true" : "false");

        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(d =>
                         d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                         d.ServiceType == typeof(DbContextOptions) ||
                         (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)))
                         .ToList())
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        });
    }

    // Cada petición con una IP distinta, para que los límites de intentos no se mezclen entre pruebas.
    public static string NewIp()
    {
        int n = Interlocked.Increment(ref nextIp);
        return $"10.{n / 62500 % 250}.{n / 250 % 250}.{n % 250 + 1}";
    }

    public HttpClient ClientWithIp(string ip)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Real-IP", ip);
        return client;
    }

    public async Task<User> AddUserAsync(string username, string password, string? passwordHash = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User { Username = username, Email = $"{username.ToLower()}@prueba.com" };
        user.PasswordHash = passwordHash ?? new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<string> LoginAsync(string login, string password)
    {
        var response = await ClientWithIp(NewIp()).PostAsJsonAsync("/login", new { email = login, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        return body!.Token;
    }

    public async Task<string> NewUserTokenAsync(string username)
    {
        await AddUserAsync(username, "contrasena_larga");
        return await LoginAsync(username, "contrasena_larga");
    }

    private sealed record LoginBody(bool Success, string Token);
}

public class ApiFactoryWithWebSocketAuth : ApiFactory
{
    public ApiFactoryWithWebSocketAuth() : base(true) { }
}
