using NUnit.Framework;
using UnityEngine;

// #262: qué pista de nivel toca en cada oleada y qué crescendo usa cada nivel.
public class MusicManagerTests
{
    private LevelData level;
    private GameObject boss;

    [SetUp]
    public void SetUp()
    {
        boss = new GameObject("BossForTest");
        level = ScriptableObject.CreateInstance<LevelData>();
        level.firstWave = 11;
        level.bossWave = 20;
        level.bossPrefab = boss;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(level);
        Object.DestroyImmediate(boss);
    }

    [Test]
    public void TrackForWave_ZoneUntilTwoWavesBeforeBoss_ThenCrescendo_ThenBoss()
    {
        Assert.AreEqual(MusicManager.LevelTrack.Zone, MusicManager.TrackForWave(level, 10)); // llegando del nivel anterior
        Assert.AreEqual(MusicManager.LevelTrack.Zone, MusicManager.TrackForWave(level, 11));
        Assert.AreEqual(MusicManager.LevelTrack.Zone, MusicManager.TrackForWave(level, 17));
        Assert.AreEqual(MusicManager.LevelTrack.Crescendo, MusicManager.TrackForWave(level, 18));
        Assert.AreEqual(MusicManager.LevelTrack.Crescendo, MusicManager.TrackForWave(level, 19));
        Assert.AreEqual(MusicManager.LevelTrack.Boss, MusicManager.TrackForWave(level, 20));
    }

    [Test]
    public void TrackForWave_LevelWithoutBoss_AlwaysZone()
    {
        level.bossWave = 0;
        level.bossPrefab = null;
        foreach (int wave in new[] { 61, 70, 99, 150 })
            Assert.AreEqual(MusicManager.LevelTrack.Zone, MusicManager.TrackForWave(level, wave));
    }

    [Test]
    public void UsesCrescendoA_AlternatesFromLevelToLevel()
    {
        Assert.IsTrue(MusicManager.UsesCrescendoA(0));
        Assert.IsFalse(MusicManager.UsesCrescendoA(1));
        Assert.IsTrue(MusicManager.UsesCrescendoA(2));
        Assert.IsFalse(MusicManager.UsesCrescendoA(3));
        Assert.IsTrue(MusicManager.UsesCrescendoA(4));
        Assert.IsFalse(MusicManager.UsesCrescendoA(5));
    }

    [Test]
    public void RealLevels_Sector7HasNoBoss_AndTheOthersDo()
    {
        foreach (LevelData real in LevelManager.Levels)
        {
            bool hasBoss = real.bossPrefab != null && real.bossWave > 0;
            Assert.AreEqual(real.sceneName != "Sector7", hasBoss, real.sceneName);
        }
    }
}
