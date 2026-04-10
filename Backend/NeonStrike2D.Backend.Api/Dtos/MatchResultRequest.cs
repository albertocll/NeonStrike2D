namespace NeonStrike2D.Backend.Api.Dtos;

public class MatchResultRequest
{
    public string RoomId { get; set; } = string.Empty;
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int? WinnerId { get; set; }
    public int Player1RoundsWon { get; set; }
    public int Player2RoundsWon { get; set; }
    public DateTime StartedAt { get; set; }
}