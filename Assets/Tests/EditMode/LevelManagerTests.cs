using NUnit.Framework;
using UnityEngine;

public class LevelManagerTests
{
    [Test]
    public void LevelForWave_PicksLevelByFirstWave_AndStaysOnLastLevel()
    {
        var level1 = ScriptableObject.CreateInstance<LevelData>();
        level1.firstWave = 1;
        var level2 = ScriptableObject.CreateInstance<LevelData>();
        level2.firstWave = 11;

        var go = new GameObject("LevelManagerUnderTest");
        var lm = go.AddComponent<LevelManager>();
        typeof(LevelManager)
            .GetField("levels", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(lm, new[] { level1, level2 });

        Assert.AreSame(level1, lm.LevelForWave(1));
        Assert.AreSame(level1, lm.LevelForWave(10));
        Assert.AreSame(level2, lm.LevelForWave(11));
        Assert.AreSame(level2, lm.LevelForWave(45));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(level1);
        Object.DestroyImmediate(level2);
    }
}
