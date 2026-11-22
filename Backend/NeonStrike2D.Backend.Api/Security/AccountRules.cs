using System.Text.RegularExpressions;

namespace NeonStrike2D.Backend.Api.Security;

// Reglas de los datos de una cuenta nueva. Solo se aplican al registrarse: las cuentas que ya
// existen con contraseñas más cortas siguen pudiendo entrar.
public static class AccountRules
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int EmailMaxLength = 254;

    // Misma regla que LoginUI.IsValidUsername en el cliente (#208).
    private static readonly Regex UsernamePattern = new("^[A-Za-z0-9_]{3,16}$", RegexOptions.Compiled);

    // Algo antes y después de una sola @, un punto en el dominio y sin espacios.
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public static bool IsValidUsername(string username) => UsernamePattern.IsMatch(username);

    public static bool IsValidEmail(string email) =>
        email.Length <= EmailMaxLength && EmailPattern.IsMatch(email) && !email.Contains("..");

    public static bool IsValidPassword(string password) =>
        password.Length >= PasswordMinLength && password.Length <= PasswordMaxLength;
}
