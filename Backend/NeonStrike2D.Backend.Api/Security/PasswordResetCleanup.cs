using Microsoft.EntityFrameworkCore;
using NeonStrike2D.Backend.Api.Data;

namespace NeonStrike2D.Backend.Api.Security;

// Borra cada hora los códigos de recuperación que caducaron hace más de un día (issue #199). Así
// se cumple lo que dice la política de privacidad aunque nadie vuelva a pedir un código.
public sealed class PasswordResetCleanup : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PasswordResetCleanup> logger;

    public PasswordResetCleanup(IServiceScopeFactory scopeFactory, ILogger<PasswordResetCleanup> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await DeleteExpiredAsync(scopeFactory, DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e)
            {
                logger.LogError("No se pudieron borrar los códigos de recuperación caducados: {Error}", e.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    // Devuelve cuántas filas ha borrado.
    public static async Task<int> DeleteExpiredAsync(IServiceScopeFactory scopeFactory, DateTime now, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var limit = now - PasswordResetRules.KeepExpired;
        var expired = db.PasswordResetCodes.Where(c => c.ExpiresAt < limit);

        // La base de datos en memoria de las pruebas no admite ExecuteDelete.
        if (db.Database.IsRelational())
            return await expired.ExecuteDeleteAsync(cancellationToken);

        var rows = await expired.ToListAsync(cancellationToken);
        db.PasswordResetCodes.RemoveRange(rows);
        await db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }
}
