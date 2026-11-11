using NUnit.Framework;
using UnityEngine;

public class EnemySeparationTests
{
    [Test]
    public void Push_AlejaSoloSiSeSolapan()
    {
        Assert.AreEqual(Vector2.zero, EnemySeparation.Push(new Vector2(2f, 0f), Vector2.zero, 1f, Vector2.up));

        Vector2 p = EnemySeparation.Push(new Vector2(0.5f, 0f), Vector2.zero, 1f, Vector2.up);
        Assert.AreEqual(0.5f, p.x, 1e-4f);
        Assert.AreEqual(0f, p.y, 1e-4f);

        Assert.AreEqual(Vector2.up, EnemySeparation.Push(Vector2.zero, Vector2.zero, 1f, Vector2.up));
    }

    [Test]
    public void KeepInside_NoEmpujaFueraDeLosMuros()
    {
        var arena = Rect.MinMaxRect(0f, 0f, 10f, 10f);

        Assert.IsTrue(new Vector2(0.5f, 5.1f) == EnemySeparation.KeepInside(new Vector2(0.5f, 5f), new Vector2(-0.1f, 0.1f), arena, 0.5f));
        Assert.IsTrue(new Vector2(0.6f, 5f) == EnemySeparation.KeepInside(new Vector2(0.5f, 5f), new Vector2(0.1f, 0f), arena, 0.5f));
        Assert.IsTrue(new Vector2(4.9f, 5f) == EnemySeparation.KeepInside(new Vector2(5f, 5f), new Vector2(-0.1f, 0f), null, 0.5f));
    }
}
