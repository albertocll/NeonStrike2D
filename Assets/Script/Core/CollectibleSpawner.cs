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
    [SerializeField] private float spawnInterval = 8f;
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
    Vector2 pos = GetRandomPosition();
    GameObject prefab;

    if (healthPrefab != null && Random.value < healthSpawnChance)
        prefab = healthPrefab;
    else
        prefab = scorePrefabs[Random.Range(0, scorePrefabs.Count)];

    GameObject item = Instantiate(prefab, pos, Quaternion.identity);
    activeCollectibles.Add(item);
    Debug.Log($"[CollectibleSpawner] Spawneado {prefab.name} en {pos}");
}

    private Vector2 GetRandomPosition()
    {
        Bounds bounds = spawnArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(x, y);
    }

    private void CleanDestroyedFromList()
    {
        activeCollectibles.RemoveAll(item => item == null);
    }
}