using System.Threading.RateLimiting;

namespace NeonStrike2D.Backend.Api.Security;

// Límite por correo de los endpoints de recuperación de contraseña (issue #199). El de por IP va
// en RateLimits; este se comprueba dentro del endpoint porque el correo viene en el cuerpo.
// /password/forgot: 3 por hora y correo. /password/reset: 10 por hora y correo. Se aplica exista
// o no la cuenta, para que el límite no delate qué correos están registrados.
public sealed class PasswordResetLimiter : IDisposable
{
    public const int ForgotPerEmail = 3;
    public const int ResetPerEmail = 10;

    private readonly PartitionedRateLimiter<string> forgot = PerEmail(ForgotPerEmail, TimeSpan.FromHours(1));
    private readonly PartitionedRateLimiter<string> reset = PerEmail(ResetPerEmail, TimeSpan.FromHours(1));

    public bool TryForgot(string normalizedEmail) => Acquire(forgot, normalizedEmail);

    public bool TryReset(string normalizedEmail) => Acquire(reset, normalizedEmail);

    private static bool Acquire(PartitionedRateLimiter<string> limiter, string key)
    {
        using var lease = limiter.AttemptAcquire(key);
        return lease.IsAcquired;
    }

    private static PartitionedRateLimiter<string> PerEmail(int permits, TimeSpan window) =>
        PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(email, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = 0
            }));

    public void Dispose()
    {
        forgot.Dispose();
        reset.Dispose();
    }
}
