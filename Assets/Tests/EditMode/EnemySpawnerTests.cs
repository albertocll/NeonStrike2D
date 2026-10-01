using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EnemySpawnerTests
{
    private const int AdvancedFromWave = 3;

    private EnemySpawner spawner;
    private GameObject basicPrefab;
    private GameObject advancedPrefab;

    [SetUp]
    public void SetUp()
    {
        basicPrefab = new GameObject("BasicEnemyPrefabStub");
        advancedPrefab = new GameObject("AdvancedEnemyPrefabStub");

        var go = new GameObject("EnemySpawnerUnderTest");
        spawner = go.AddComponent<EnemySpawner>();

        SetField("basicEnemyPrefabs", new List<GameObject> { basicPrefab });
        SetField("advancedEnemyPrefabs", new List<GameObject> { advancedPrefab });
        SetField("advancedEnemyFromWave", AdvancedFromWave);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(spawner.gameObject);
        Object.DestroyImmediate(basicPrefab);
        Object.DestroyImmediate(advancedPrefab);
    }

    private void SetField(string name, object value)
    {
        typeof(EnemySpawner)
            .GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(spawner, value);
    }

    [Test]
    public void ChooseEnemyPrefab_BelowThreshold_AlwaysPicksBasic()
    {
        for (int wave = 1; wave < AdvancedFromWave; wave++)
        {
            for (int i = 0; i < 20; i++)
                Assert.AreSame(basicPrefab, spawner.ChooseEnemyPrefab(wave));
        }
    }

    [Test]
    public void ChooseEnemyPrefab_AtThreshold_WithNoAdvancedPrefabs_FallsBackToBasic()
    {
        SetField("advancedEnemyPrefabs", new List<GameObject>());

        for (int i = 0; i < 20; i++)
            Assert.AreSame(basicPrefab, spawner.ChooseEnemyPrefab(AdvancedFromWave));
    }

    [Test]
    public void ChooseEnemyPrefab_AtOrAboveThreshold_OnlyReturnsKnownPrefabs()
    {
        for (int i = 0; i < 50; i++)
        {
            GameObject result = spawner.ChooseEnemyPrefab(AdvancedFromWave + 2);
            Assert.That(result, Is.EqualTo(basicPrefab).Or.EqualTo(advancedPrefab));
        }
    }
}
