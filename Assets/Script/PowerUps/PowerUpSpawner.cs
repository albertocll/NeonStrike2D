using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private List<GameObject> powerUpPrefabs;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider2D spawnArea;

    [Header("Config")]
    [SerializeField] private float spawnInterval = 22.5f; // +50% (era 15s), menos densidad de power-ups en pantalla
    [SerializeField] private int maxPowerUps = 2;

    private List<GameObject> activePowerUps = new List<GameObject>();
    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            CleanDestroyedFromList();

            if (activePowerUps.Count < maxPowerUps)
                SpawnPowerUp();
        }
    }

    private void SpawnPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0) return;

        Vector2 pos = GetRandomPosition();
        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];

        GameObject item = Instantiate(prefab, pos, Quaternion.identity);
        activePowerUps.Add(item);
        Debug.Log($"[PowerUpSpawner] Spawneado {prefab.name} en {pos}");
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
        activePowerUps.RemoveAll(item => item == null);
    }
}
