namespace NeonStrike2D.Backend.Api.Models;

public class GameSession
{
    public int Id { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int? WinnerId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "waiting"; // waiting, playing, finished

    // Navegación
    public User Player1 { get; set; } = null!;
    public User Player2 { get; set; } = null!;
    public User? Winner { get; set; }
}