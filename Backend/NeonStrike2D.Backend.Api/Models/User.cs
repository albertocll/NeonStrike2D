namespace NeonStrike2D.Backend.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Nulo en las cuentas creadas con Google, que no tienen contraseña (issue #75).
    public string? PasswordHash { get; set; }

    // El "sub" del id_token de Google; nulo en las cuentas que nunca han entrado con Google (issue #75).
    public string? GoogleSubject { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
