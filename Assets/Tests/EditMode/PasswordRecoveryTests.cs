using System.Text.RegularExpressions;
using NUnit.Framework;

// Recuperar la contraseña con un código (issue #199): validación del panel 2, errores del backend
// y textos en los dos idiomas.
public class PasswordRecoveryTests
{
    [Test]
    public void DatosValidos_NoDanError()
    {
        Assert.IsNull(PasswordRecoveryUI.ValidateReset("042137", "contrasena_nueva", "contrasena_nueva"));
    }

    [Test]
    public void CamposVacios_PidenRellenarlos()
    {
        Assert.AreEqual("msg.fillFields", PasswordRecoveryUI.ValidateReset("", "contrasena_nueva", "contrasena_nueva"));
        Assert.AreEqual("msg.fillFields", PasswordRecoveryUI.ValidateReset("042137", "", "contrasena_nueva"));
        Assert.AreEqual("msg.fillFields", PasswordRecoveryUI.ValidateReset("042137", "contrasena_nueva", ""));
    }

    [TestCase("12345")]
    [TestCase("1234567")]
    [TestCase("12a456")]
    [TestCase("12 456")]
    public void CodigoMalFormado_Avisa(string code)
    {
        Assert.AreEqual("reset.codeRule", PasswordRecoveryUI.ValidateReset(code, "contrasena_nueva", "contrasena_nueva"));
    }

    [Test]
    public void ContrasenaFueraDeLaRegla_Avisa()
    {
        Assert.AreEqual("server.passwordRule", PasswordRecoveryUI.ValidateReset("042137", "corta", "corta"));
        string larga = new string('a', 129);
        Assert.AreEqual("server.passwordRule", PasswordRecoveryUI.ValidateReset("042137", larga, larga));
        string justa = new string('a', 128);
        Assert.IsNull(PasswordRecoveryUI.ValidateReset("042137", justa, justa));
    }

    [Test]
    public void ContrasenasDistintas_Avisa()
    {
        Assert.AreEqual("reset.mismatch", PasswordRecoveryUI.ValidateReset("042137", "contrasena_nueva", "contrasena_otra"));
    }

    [TestCase("INVALID_CODE", "server.invalidCode")]
    [TestCase("CODE_EXPIRED", "server.codeExpired")]
    [TestCase("CODE_USED", "server.codeUsed")]
    [TestCase("TOO_MANY_ATTEMPTS", "server.tooManyAttempts")]
    [TestCase("CODE_REQUIRED", "server.codeRequired")]
    [TestCase("RATE_LIMITED", "server.rateLimited")]
    [TestCase("INVALID_PASSWORD", "server.passwordRule")]
    public void ErroresDelBackend_SeTraducenPorSuCodigo(string code, string key)
    {
        Assert.AreEqual(LanguageManager.Get(key), LanguageManager.ServerError(code, "texto cualquiera", "reset.error"));
    }

    [Test]
    public void TextosNuevos_EnLosDosIdiomas()
    {
        string[] keys =
        {
            "forgot.title", "forgot.send", "reset.title", "reset.confirm", "login.forgotPassword",
            "reset.codeLabel", "reset.newPasswordLabel", "reset.repeatLabel", "reset.noCode",
            "placeholder.code", "placeholder.newPassword", "placeholder.repeatPassword",
            "forgot.hint", "forgot.sending", "forgot.error", "reset.sent", "reset.codeRule",
            "reset.mismatch", "reset.changing", "reset.error", "reset.done"
        };
        foreach (var key in keys)
        {
            Assert.IsTrue(LocalizedTexts.TryGet(key, LanguageManager.Language.Spanish, out var es) && !string.IsNullOrEmpty(es), key);
            Assert.IsTrue(LocalizedTexts.TryGet(key, LanguageManager.Language.English, out var en) && !string.IsNullOrEmpty(en), key);
        }
    }

    // Textos en Orbitronio: minúsculas, sin puntuación y sin ñ ni tildes.
    [Test]
    public void TextosEnOrbitronio_SinMayusculasNiPuntuacionNiEnie()
    {
        var allowed = new Regex("^[a-z0-9 ]+$");
        foreach (var key in new[] { "forgot.title", "forgot.send", "reset.title", "reset.confirm" })
        {
            LocalizedTexts.TryGet(key, LanguageManager.Language.Spanish, out var es);
            LocalizedTexts.TryGet(key, LanguageManager.Language.English, out var en);
            StringAssert.IsMatch(allowed.ToString(), es, key);
            StringAssert.IsMatch(allowed.ToString(), en, key);
        }
    }
}
