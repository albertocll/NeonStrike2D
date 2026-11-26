using NeonStrike2D.Backend.Api.Security;

namespace NeonStrike2D.Backend.Api.Email;

// Texto del correo de recuperación, en español o en inglés según el idioma del juego.
public static class PasswordResetEmail
{
    public sealed record Mail(string Subject, string Text, string Html);

    public static Mail Build(string code, bool english)
    {
        int minutes = (int)PasswordResetRules.Lifetime.TotalMinutes;

        string subject, intro, expiry, ignore;
        if (english)
        {
            subject = "Your NeonStrike2D password reset code";
            intro = "Use this code in the game to choose a new password:";
            expiry = $"The code expires in {minutes} minutes and can only be used once.";
            ignore = "If you didn't ask for it, ignore this email: your password stays the same.";
        }
        else
        {
            subject = "Tu código para recuperar la contraseña de NeonStrike2D";
            intro = "Escribe este código en el juego para elegir una contraseña nueva:";
            expiry = $"El código caduca en {minutes} minutos y solo sirve una vez.";
            ignore = "Si no lo has pedido tú, ignora este correo: tu contraseña no cambia.";
        }

        string text = $"{intro}\n\n{code}\n\n{expiry}\n\n{ignore}\n\nNeonStrike2D";
        string html =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:16px;color:#222\">" +
            $"<p>{intro}</p>" +
            $"<p style=\"font-size:32px;font-weight:bold;letter-spacing:6px\">{code}</p>" +
            $"<p>{expiry}</p>" +
            $"<p>{ignore}</p>" +
            "<p>NeonStrike2D</p>" +
            "</div>";

        return new Mail(subject, text, html);
    }
}
