using NUnit.Framework;

// Errores del backend: el cliente 1.0.1 los traduce por código y, sin código, por el texto del mensaje.
public class ServerErrorTranslationTests
{
    [Test]
    public void CadaCodigo_TieneTextoEnLosDosIdiomas()
    {
        foreach (var key in LocalizedTexts.CodeKeys)
        {
            Assert.IsTrue(LocalizedTexts.TryGet(key, LanguageManager.Language.Spanish, out var es) && !string.IsNullOrEmpty(es), key);
            Assert.IsTrue(LocalizedTexts.TryGet(key, LanguageManager.Language.English, out var en) && !string.IsNullOrEmpty(en), key);
        }
    }

    [Test]
    public void ConCodigo_SeTraducePorElCodigo()
    {
        Assert.AreEqual(LanguageManager.Get("server.passwordRule"),
            LanguageManager.ServerError("INVALID_PASSWORD", "texto cualquiera", "msg.registerError"));
        Assert.AreEqual(LanguageManager.Get("server.rateLimited"),
            LanguageManager.ServerError("RATE_LIMITED", null, "msg.connectError"));
    }

    [Test]
    public void SinCodigo_SeTraducePorElTexto()
    {
        Assert.AreEqual(LanguageManager.Get("server.badCredentials"),
            LanguageManager.ServerError(null, "Credenciales incorrectas.", "msg.connectError"));
        Assert.AreEqual(LanguageManager.Get("server.badCredentials"),
            LanguageManager.ServerError("CODIGO_NUEVO", "Credenciales incorrectas.", "msg.connectError"));
    }

    [Test]
    public void SinNada_SeUsaElRespaldo()
    {
        Assert.AreEqual(LanguageManager.Get("msg.connectError"),
            LanguageManager.ServerError(null, null, "msg.connectError"));
    }
}
