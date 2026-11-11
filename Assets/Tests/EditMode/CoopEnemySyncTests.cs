using System.Text;
using NUnit.Framework;
using UnityEngine;

public class CoopEnemySyncTests
{
    [TearDown]
    public void TearDown()
    {
        if (PlayerTargets.Local != null) Object.DestroyImmediate(PlayerTargets.Local.gameObject);
        if (PlayerTargets.Remote != null) Object.DestroyImmediate(PlayerTargets.Remote.gameObject);
        PlayerTargets.Local = null;
        PlayerTargets.Remote = null;
    }

    [Test]
    public void PrefabKey_QuitaLosClone()
    {
        Assert.AreEqual("EnemyStriker", NetEnemy.PrefabKey("EnemyStriker(Clone)"));
        Assert.AreEqual("EnemySwarmUnit", NetEnemy.PrefabKey("EnemySwarmUnit(Clone)(Clone)"));
        Assert.AreEqual("BossBulwark", NetEnemy.PrefabKey("BossBulwark"));
    }

    [Test]
    public void Nearest_EligeAlJugadorMasCercano()
    {
        var fallback = new GameObject("Otro").transform;
        Assert.AreSame(fallback, PlayerTargets.Nearest(Vector2.zero, fallback), "Sin jugadores registrados se queda con el que tenia");

        PlayerTargets.Local = new GameObject("Local").transform;
        PlayerTargets.Local.position = new Vector2(5f, 0f);
        Assert.AreSame(PlayerTargets.Local, PlayerTargets.Nearest(Vector2.zero, fallback), "En solitario siempre el local");

        PlayerTargets.Remote = new GameObject("Remote").transform;
        PlayerTargets.Remote.position = new Vector2(-2f, 0f);
        Assert.AreSame(PlayerTargets.Remote, PlayerTargets.Nearest(Vector2.zero, fallback));
        Assert.AreSame(PlayerTargets.Local, PlayerTargets.Nearest(new Vector2(4f, 0f), fallback));

        Object.DestroyImmediate(fallback.gameObject);
    }

    [Test]
    public void IsPlayer_CuentaLaCopiaDelOtroJugadorYSusHijos()
    {
        PlayerTargets.Remote = new GameObject("Remote").transform;
        var child = new GameObject("Hijo").transform;
        child.SetParent(PlayerTargets.Remote);
        var stranger = new GameObject("Enemigo");

        Assert.IsTrue(PlayerTargets.IsPlayer(PlayerTargets.Remote));
        Assert.IsTrue(PlayerTargets.IsPlayer(child));
        Assert.IsFalse(PlayerTargets.IsPlayer(stranger.transform));

        Object.DestroyImmediate(stranger);
    }

    [Test]
    public void AppendState_FormatoCompactoSinComillas()
    {
        var go = new GameObject("EnemyWarden(Clone)");
        go.transform.position = new Vector2(1.234f, -5f);
        var sr = new GameObject("Visual").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(go.transform);
        sr.transform.localScale = new Vector3(-1f, 1f, 1f);
        sr.flipX = true;

        var net = go.AddComponent<NetEnemy>();
        net.InitHost(7);
        var sb = new StringBuilder();
        net.AppendState(sb);

        Assert.AreEqual("7,EnemyWarden,1.23,-5,0,3,FFFFFFFF", sb.ToString());
        Object.DestroyImmediate(go);
    }
}
