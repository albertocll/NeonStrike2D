using NUnit.Framework;
using UnityEngine;

// 1.0.2: respuesta del joystick. Zona muerta, velocidad proporcional a la inclinacion hasta
// fullSpeedInput y a tope a partir de ahi; el teclado siempre a tope.
public class PlayerMovementInputTests
{
    private const float Tolerance = 0.0001f;

    [Test]
    public void DentroDeLaZonaMuerta_NoSeMueve()
    {
        Assert.AreEqual(Vector2.zero, Joystick.ApplyDeadZone(new Vector2(0.1f, 0.05f), 0.15f));
    }

    [Test]
    public void FueraDeLaZonaMuerta_SinSalto()
    {
        Vector2 justOutside = Joystick.ApplyDeadZone(new Vector2(0.16f, 0f), 0.15f);
        Assert.Less(justOutside.magnitude, 0.02f);

        Vector2 full = Joystick.ApplyDeadZone(new Vector2(0f, 1f), 0.15f);
        Assert.AreEqual(1f, full.magnitude, Tolerance);
        Assert.AreEqual(0f, full.x, Tolerance);
    }

    [Test]
    public void Joystick_ProporcionalHastaFullSpeedInput()
    {
        Vector2 half = PlayerController.CombineInput(Vector2.zero, new Vector2(0.25f, 0f), 0.5f);
        Assert.AreEqual(0.5f, half.x, Tolerance);

        Vector2 full = PlayerController.CombineInput(Vector2.zero, new Vector2(-0.8f, 0f), 0.5f);
        Assert.AreEqual(-1f, full.x, Tolerance);
    }

    [Test]
    public void Joystick_ConservaLaDireccion()
    {
        Vector2 raw = new Vector2(0.3f, -0.4f);
        Vector2 result = PlayerController.CombineInput(Vector2.zero, raw, 0.5f);
        Assert.AreEqual(1f, result.magnitude, Tolerance);
        Assert.AreEqual(raw.normalized.x, result.x, Tolerance);
        Assert.AreEqual(raw.normalized.y, result.y, Tolerance);
    }

    [Test]
    public void Teclado_SiempreATope()
    {
        Assert.AreEqual(1f, PlayerController.CombineInput(new Vector2(1f, 1f), Vector2.zero, 0.5f).magnitude, Tolerance);
    }

    [Test]
    public void SinEntrada_SeParaEnSeco()
    {
        Assert.AreEqual(Vector2.zero, PlayerController.CombineInput(Vector2.zero, Vector2.zero, 0.5f));
    }
}
