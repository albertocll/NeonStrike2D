using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace NeonStrike2D.Backend.Api.Security;

// Límite de peticiones. Endpoints públicos, por IP: /login 5 por minuto, /register 3 por hora y
// el resto 60 por minuto. Endpoints con sesión, por usuario (id del token): /match/result 10 por
// minuto y el resto 120 por minuto. Recuperación de contraseña, por IP: /password/forgot 10 por
// hora y /password/reset 20 por hora (y además por correo, ver PasswordResetLimiter). Al pasarse,
// 429 con el código RATE_LIMITED.
public static class RateLimits
{
    public const string Login = "login";
    public const string Register = "register";
    public const string PasswordForgot = "contrasena-pedir";
    public const string PasswordReset = "contrasena-cambiar";
    public const string Public = "publico";
    public const string MatchResult = "partida";
    public const string Session = "sesion";

    public const string RejectedMessage = "Demasiados intentos. Espera un poco y vuelve a probar.";

    public static IServiceCollection AddGameRateLimits(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { Success = false, Message = RejectedMessage, Code = ErrorCodes.RateLimited }, token);
            };

            options.AddPolicy(Login, context => Fixed(context, 5, TimeSpan.FromMinutes(1)));
            options.AddPolicy(Register, context => Fixed(context, 3, TimeSpan.FromHours(1)));
            options.AddPolicy(PasswordForgot, context => Fixed(context, 10, TimeSpan.FromHours(1)));
            options.AddPolicy(PasswordReset, context => Fixed(context, 20, TimeSpan.FromHours(1)));
            options.AddPolicy(Public, context => Fixed(context, 60, TimeSpan.FromMinutes(1)));
            options.AddPolicy(MatchResult, context => PerUser(context, 10, TimeSpan.FromMinutes(1)));
            options.AddPolicy(Session, context => PerUser(context, 120, TimeSpan.FromMinutes(1)));
        });
    }

    // IP del jugador. El proxy de Railway añade la IP real al final de X-Forwarded-For; lo que va
    // delante lo puede inventar el cliente, así que solo vale la última. Sin esa cabecera,
    // X-Real-IP, y si tampoco viene (en local), la IP de la conexión.
    public static string ClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (forwarded.Length > 0) return forwarded[^1];

        var realIp = context.Request.Headers["X-Real-IP"].ToString().Trim();
        if (!string.IsNullOrEmpty(realIp)) return realIp;
        return context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    }

    // Jugador con sesión: el id del token. Estos endpoints piden sesión, así que siempre viene;
    // si no, se cae a la IP para no dejar la petición sin límite.
    public static string UserKey(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userId) ? "ip:" + ClientIp(context) : "usuario:" + userId;
    }

    private static RateLimitPartition<string> PerUser(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(UserKey(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = window,
            QueueLimit = 0
        });

    private static RateLimitPartition<string> Fixed(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = window,
            QueueLimit = 0
        });
}
