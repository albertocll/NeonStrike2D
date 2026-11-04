using NUnit.Framework;
using UnityEngine;

public class RunStateTests
{
    [TearDown]
    public void TearDown() => RunState.Clear();

    [Test]
    public void Capture_SavesHealthWaveAndActivePowerUps()
    {
        var player = new GameObject("PlayerUnderTest");
        var health = player.AddComponent<PlayerHealth>();
        var powerUps = player.AddComponent<PlayerPowerUps>();
        health.Init(100);
        health.TakeDamage(30);
        powerUps.ApplyPowerUp(PowerUp.PowerUpType.TripleShot, 5f);
        RunState.Score = 1200;

        RunState.Capture(player, 10);

        Assert.IsTrue(RunState.Active);
        Assert.AreEqual(10, RunState.Wave);
        Assert.AreEqual(1200, RunState.Score);
        Assert.AreEqual(70, RunState.Health);
        Assert.AreEqual(5f, RunState.PowerUpTimes[PowerUp.PowerUpType.TripleShot]);
        Assert.IsFalse(RunState.PowerUpTimes.ContainsKey(PowerUp.PowerUpType.Shield));

        Object.DestroyImmediate(player);
    }

    [Test]
    public void Clear_ResetsEverything()
    {
        RunState.Score = 500;
        RunState.Capture(null, 20);

        RunState.Clear();

        Assert.IsFalse(RunState.Active);
        Assert.AreEqual(0, RunState.Wave);
        Assert.AreEqual(0, RunState.Score);
        Assert.AreEqual(0, RunState.Health);
        Assert.AreEqual(0, RunState.PowerUpTimes.Count);
    }

    [Test]
    public void PlayerHealthInit_KeepsHealthOnlyWhenComingFromAnotherLevel()
    {
        var player = new GameObject("PlayerUnderTest");
        var health = player.AddComponent<PlayerHealth>();

        health.Init(100);
        Assert.AreEqual(100, health.CurrentHealth);

        var donor = new GameObject("Donor");
        var donorHealth = donor.AddComponent<PlayerHealth>();
        donorHealth.Init(100);
        donorHealth.TakeDamage(40);
        RunState.Capture(donor, 10);

        health.Init(100);
        Assert.AreEqual(60, health.CurrentHealth);

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(donor);
    }
}
