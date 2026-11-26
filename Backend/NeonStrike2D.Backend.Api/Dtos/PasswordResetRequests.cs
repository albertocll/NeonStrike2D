namespace NeonStrike2D.Backend.Api.Dtos;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;

    // Idioma del correo: "en" para inglés; cualquier otro valor, español.
    public string? Language { get; set; }
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
