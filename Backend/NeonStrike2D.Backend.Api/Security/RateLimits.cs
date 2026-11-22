using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace NeonStrike2D.Backend.Api.Security;

// Límite de peticiones por IP: /login 5 por minuto, /register 3 por hora y el resto de
// endpoints públicos 60 por minuto. Al pasarse, 429 con el código RATE_LIMITED.
public static class RateLimits
{
    public const string Login = "login";
    public const string Register = "register";
    public const string Public = "publico";

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
            options.AddPolicy(Public, context => Fixed(context, 60, TimeSpan.FromMinutes(1)));
        });
    }

    // IP del jugador: Railway la pone en X-Real-IP (la conexión llega desde su proxy).
    // Sin esa cabecera (en local), la de la conexión.
    public static string ClientIp(HttpContext context)
    {
        var realIp = context.Request.Headers["X-Real-IP"].ToString().Trim();
        if (!string.IsNullOrEmpty(realIp)) return realIp;
        return context.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    }

    private static RateLimitPartition<string> Fixed(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = window,
            QueueLimit = 0
        });
}
