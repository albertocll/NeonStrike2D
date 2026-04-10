namespace NeonStrike2D.Backend.Api.Models;

public class GameResult
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int GameSessionId { get; set; }
    public bool Won { get; set; }
    public int RoundsWon { get; set; }
    public int RoundsLost { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    // Navegación
    public User User { get; set; } = null!;
    public GameSession GameSession { get; set; } = null!;
}