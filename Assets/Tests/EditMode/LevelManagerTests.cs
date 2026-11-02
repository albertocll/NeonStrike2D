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
        var levels = new[] { level1, level2 };

        Assert.AreSame(level1, LevelManager.LevelForWave(levels, 1));
        Assert.AreSame(level1, LevelManager.LevelForWave(levels, 10));
        Assert.AreSame(level2, LevelManager.LevelForWave(levels, 11));
        Assert.AreSame(level2, LevelManager.LevelForWave(levels, 45));

        Object.DestroyImmediate(level1);
        Object.DestroyImmediate(level2);
    }

    [Test]
    public void Levels_StartInSector7_ThenTheGrid()
    {
        // Los LevelData reales de Resources/Levels: reiniciar siempre lleva al primero.
        Assert.AreEqual("Sector7", LevelManager.FirstLevel.sceneName);
        Assert.AreEqual("TheGrid", LevelManager.LevelForWave(11).sceneName);
        Assert.IsTrue(LevelManager.IsLevelScene("Sector7"));
        Assert.IsTrue(LevelManager.IsLevelScene("TheGrid"));
        Assert.IsFalse(LevelManager.IsLevelScene("MainMenu"));
    }
}
