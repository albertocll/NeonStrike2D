using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> enemyPrefabs;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider2D spawnArea;

    [Header("Legacy Spawn Settings (desactivado para Wave System)")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxEnemies = 5;

    private float spawnTimer;

    private void Update()
    {
        // SISTEMA VIEJO DESACTIVADO
        // spawnTimer += Time.deltaTime;
        //
        // if (spawnTimer >= spawnInterval)
        // {
        //     spawnTimer = 0f;
        //     TrySpawnEnemy();
        // }
    }

    private void TrySpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (currentEnemies >= maxEnemies) return;

        Vector2 spawnPosition = GetRandomPosition();
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }

    public void SpawnWave(int enemyCount, WaveManager waveManager)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No hay prefabs asignados.");
            return;
        }

        if (spawnArea == null)
        {
            Debug.LogError("[EnemySpawner] No hay spawnArea asignada.");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnPosition = GetRandomPosition();
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            EnemyWaveMember waveMember = enemyInstance.GetComponent<EnemyWaveMember>();

            if (waveMember != null)
            {
                waveMember.Initialize(waveManager);
            }
            else
            {
                Debug.LogError("[EnemySpawner] El enemigo instanciado no tiene EnemyWaveMember.");
            }
        }
    }

    private Vector2 GetRandomPosition()
    {
        Bounds bounds = spawnArea.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        return new Vector2(x, y);
    }
}