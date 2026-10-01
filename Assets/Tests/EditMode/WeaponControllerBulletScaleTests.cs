using NUnit.Framework;
using UnityEngine;

// #130: cada personaje debe disparar con su propio sprite/escala de bala,
// en vez de compartir un unico bulletPrefab fijo. Estos tests cubren la pieza
// de calculo real en codigo (la asignacion por personaje vía SetBulletSprite);
// el ajuste de Pixels Per Unit es una configuracion de import de cada sprite,
// no hay formula en C# que testear ahi.
public class WeaponControllerBulletScaleTests
{
    private WeaponController weapon;
    private Sprite spriteA;
    private Sprite spriteB;

    [SetUp]
    public void SetUp()
    {
        var go = new GameObject("WeaponControllerUnderTest");
        go.AddComponent<BoxCollider2D>();
        weapon = go.AddComponent<WeaponController>();

        var texture = new Texture2D(4, 4);
        spriteA = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        spriteB = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(weapon.gameObject);
        Object.DestroyImmediate(spriteA);
        Object.DestroyImmediate(spriteB);
    }

    private Sprite CurrentSprite() => (Sprite)typeof(WeaponController)
        .GetField("bulletSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        .GetValue(weapon);

    private float CurrentScale() => (float)typeof(WeaponController)
        .GetField("bulletScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        .GetValue(weapon);

    [Test]
    public void SetBulletSprite_StoresSpriteAndScaleForCharacter()
    {
        weapon.SetBulletSprite(spriteA, 0.6f);

        Assert.AreSame(spriteA, CurrentSprite());
        Assert.AreEqual(0.6f, CurrentScale());
    }

    [Test]
    public void SetBulletSprite_SwitchingCharacter_OverwritesPreviousSpriteAndScale()
    {
        weapon.SetBulletSprite(spriteA, 0.6f);
        weapon.SetBulletSprite(spriteB, 1.4f);

        Assert.AreSame(spriteB, CurrentSprite());
        Assert.AreEqual(1.4f, CurrentScale());
    }
}
