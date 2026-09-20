using System.Collections.Generic;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private List<GameObject> scorePrefabs;
    [SerializeField] private GameObject healthPrefab;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider2D spawnArea;

    [Header("Config")]
    [SerializeField] private float spawnInterval = 12f; // +50% (era 8s), menos densidad de coleccionables en pantalla
    [SerializeField] private int maxCollectibles = 5;
    [SerializeField] private float healthSpawnChance = 0.25f;

    private List<GameObject> activeCollectibles = new List<GameObject>();
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            CleanDestroyedFromList();

            if (activeCollectibles.Count < maxCollectibles)
                SpawnCollectible();
        }
    }

    private void SpawnCollectible()
{
    GameObject prefab;

    if (healthPrefab != null && Random.value < healthSpawnChance)
        prefab = healthPrefab;
    else
        prefab = scorePrefabs[Random.Range(0, scorePrefabs.Count)];

    Vector2 pos = SpawnAreaUtils.GetRandomPoint(spawnArea, prefab);

    GameObject item = Instantiate(prefab, pos, Quaternion.identity);
    activeCollectibles.Add(item);
    Debug.Log($"[CollectibleSpawner] Spawneado {prefab.name} en {pos}");
}

    private void CleanDestroyedFromList()
    {
        activeCollectibles.RemoveAll(item => item == null);
    }
}