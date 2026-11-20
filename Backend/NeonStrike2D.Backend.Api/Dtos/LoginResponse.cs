namespace NeonStrike2D.Backend.Api.Dtos;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    // Solo en /auth/google: la cuenta se acaba de crear y el cliente debe ofrecer elegir nombre (issue #75).
    public bool IsNewAccount { get; set; }
}