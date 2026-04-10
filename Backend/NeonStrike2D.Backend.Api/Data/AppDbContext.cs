using Microsoft.EntityFrameworkCore;
using NeonStrike2D.Backend.Api.Models;

namespace NeonStrike2D.Backend.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameResult> GameResults => Set<GameResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // GameSession — relaciones con User
        modelBuilder.Entity<GameSession>()
            .HasOne(g => g.Player1)
            .WithMany()
            .HasForeignKey(g => g.Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameSession>()
            .HasOne(g => g.Player2)
            .WithMany()
            .HasForeignKey(g => g.Player2Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameSession>()
            .HasOne(g => g.Winner)
            .WithMany()
            .HasForeignKey(g => g.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // GameResult — relaciones
        modelBuilder.Entity<GameResult>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameResult>()
            .HasOne(r => r.GameSession)
            .WithMany()
            .HasForeignKey(r => r.GameSessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}