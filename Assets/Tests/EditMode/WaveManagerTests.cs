using NUnit.Framework;
using UnityEngine;

public class WaveManagerTests
{
    private WaveManager CreateWaveManager(int basePerWave, int addedPerWave)
    {
        var go = new GameObject("WaveManagerUnderTest");
        var wm = go.AddComponent<WaveManager>();
        typeof(WaveManager)
            .GetField("baseEnemiesPerWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(wm, basePerWave);
        typeof(WaveManager)
            .GetField("enemiesAddedPerWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(wm, addedPerWave);
        return wm;
    }

    [TestCase(1, ExpectedResult = 3)]
    [TestCase(5, ExpectedResult = 7)]
    [TestCase(9, ExpectedResult = 11)]
    public int EnemiesForWave_UsesLevel1DefaultValues(int wave)
    {
        // base=3, added=1: los valores reales configurados en Level1.unity (ver #165).
        var wm = CreateWaveManager(basePerWave: 3, addedPerWave: 1);
        int result = wm.EnemiesForWave(wave);
        Object.DestroyImmediate(wm.gameObject);
        return result;
    }

    [Test]
    public void EnemiesForWave_GrowsLinearlyWithWave()
    {
        var wm = CreateWaveManager(basePerWave: 2, addedPerWave: 3);

        Assert.AreEqual(2, wm.EnemiesForWave(1));
        Assert.AreEqual(5, wm.EnemiesForWave(2));
        Assert.AreEqual(8, wm.EnemiesForWave(3));

        Object.DestroyImmediate(wm.gameObject);
    }
}
