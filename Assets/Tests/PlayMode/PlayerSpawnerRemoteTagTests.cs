using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// #150: el avatar del jugador remoto no debe quedarse con el tag "Player",
// para no confundirse con el jugador local en logica que busca ese tag.
public class PlayerSpawnerRemoteTagTests
{
    private PlayerSpawner spawner;
    private GameObject characterPrefab;
    private CharacterData characterData;

    [SetUp]
    public void SetUp()
    {
        characterPrefab = new GameObject("RemoteCharacterPrefabStub");
        characterPrefab.tag = "Player"; // de partida lleva el tag, como el prefab real de personaje
        characterPrefab.AddComponent<PlayerController>(); // PlayerSync.Init(..., isLocal:false) lo desactiva

        characterData = ScriptableObject.CreateInstance<CharacterData>();
        characterData.characterName = "TestCharacter";
        characterData.prefab = characterPrefab;
        characterData.scale = Vector3.one;

        // El GameObject se mantiene inactivo para que Awake() no se dispare solo
        // (SpawnLocalPlayer recorreria "characters" antes de que lo rellenemos).
        // Solo nos interesa probar SpawnRemotePlayer de forma aislada, invocado
        // directamente por reflexion mas abajo.
        var go = new GameObject("PlayerSpawnerUnderTest");
        go.SetActive(false);
        spawner = go.AddComponent<PlayerSpawner>();
        typeof(PlayerSpawner)
            .GetField("characters", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(spawner, new[] { characterData });
    }

    [TearDown]
    public void TearDown()
    {
        var remotePlayer = (GameObject)typeof(PlayerSpawner)
            .GetField("_remotePlayer", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(spawner);
        if (remotePlayer != null) Object.DestroyImmediate(remotePlayer);

        Object.DestroyImmediate(spawner.gameObject);
        Object.DestroyImmediate(characterPrefab);
        Object.DestroyImmediate(characterData);
    }

    [Test]
    public void SpawnRemotePlayer_NeverKeepsThePlayerTag()
    {
        var spawnMethod = typeof(PlayerSpawner).GetMethod("SpawnRemotePlayer", BindingFlags.NonPublic | BindingFlags.Instance);
        spawnMethod.Invoke(spawner, new object[] { "TestCharacter" });

        var remotePlayer = (GameObject)typeof(PlayerSpawner)
            .GetField("_remotePlayer", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(spawner);

        Assert.IsNotNull(remotePlayer, "SpawnRemotePlayer deberia haber instanciado al avatar remoto.");
        Assert.AreNotEqual("Player", remotePlayer.tag, "El avatar remoto no debe quedarse con el tag Player (#150).");
    }
}
