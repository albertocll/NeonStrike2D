namespace NeonStrike2D.Backend.Api.Models;

// Código de recuperación de contraseña (issue #199). Solo se guarda el hash del código.
//
// También se guarda una fila cuando el correo no tiene cuenta, con el hash de un código que nunca
// se envía: así /password/reset responde igual (incorrecto, caducado, demasiados intentos) exista
// o no la cuenta, y no se puede averiguar qué correos están registrados. Por eso la fila va por el
// hash del correo y no solo por el usuario.
public class PasswordResetCode
{
    public int Id { get; set; }

    // SHA-256 del correo en minúsculas: no se guarda el correo de quien no tiene cuenta.
    public string EmailHash { get; set; } = string.Empty;

    // Usuario de la cuenta; null si el correo no está registrado.
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    // Intentos fallidos con este código.
    public int Attempts { get; set; }

    // Momento en que se cambió la contraseña con este código; null si aún no se ha usado.
    public DateTime? UsedAt { get; set; }
}
