using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using NeonStrike2D.Backend.Api.Models;

namespace NeonStrike2D.Backend.Api.Security;

// Reglas de los códigos de recuperación de contraseña (issue #199): 6 dígitos, 15 minutos de
// validez y como mucho 5 intentos fallidos. El código se guarda con el mismo hash que las
// contraseñas (PasswordHasher), nunca en claro.
public static class PasswordResetRules
{
    public const int CodeLength = 6;
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    // Las filas caducadas se borran pasado este tiempo, al pedir códigos nuevos.
    public static readonly TimeSpan KeepExpired = TimeSpan.FromDays(1);

    // Mismo mensaje exista o no la cuenta.
    public const string ForgotMessage = "Si el correo está registrado, te llegará un código en unos minutos.";

    private static readonly PasswordHasher<PasswordResetCode> Hasher = new();

    public static string NewCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static bool IsWellFormed(string code) =>
        code.Length == CodeLength && code.All(char.IsAsciiDigit);

    public static string HashCode(string code) => Hasher.HashPassword(new PasswordResetCode(), code);

    public static bool VerifyCode(string hash, string code) =>
        Hasher.VerifyHashedPassword(new PasswordResetCode(), hash, code) != PasswordVerificationResult.Failed;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string HashEmail(string normalizedEmail) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));
}
