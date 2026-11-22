namespace NeonStrike2D.Backend.Api.Dtos;

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Código del error para que el cliente lo traduzca; vacío si todo va bien.
    public string? Code { get; set; }
}
