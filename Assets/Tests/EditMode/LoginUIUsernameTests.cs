using NUnit.Framework;

public class LoginUIUsernameTests
{
    [TestCase("abc")]
    [TestCase("Neon_Strike2D")]
    [TestCase("abcdefghijklmnop")]
    public void IsValidUsername_AceptaLetrasNumerosYGuionBajo(string username)
    {
        Assert.IsTrue(LoginUI.IsValidUsername(username));
    }

    [TestCase("ab")]
    [TestCase("abcdefghijklmnopq")]
    [TestCase("con espacio")]
    [TestCase("ñandú")]
    [TestCase("emoji😀")]
    [TestCase("guion-medio")]
    public void IsValidUsername_RechazaLongitudOCaracteresFueraDeLaRegla(string username)
    {
        Assert.IsFalse(LoginUI.IsValidUsername(username));
    }
}
