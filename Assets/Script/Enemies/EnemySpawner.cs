using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> basicEnemyPrefabs;
    [SerializeField] private List<GameObject> advancedEnemyPrefabs;
    [SerializeField] private int advancedEnemyFromWave = 3;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider2D spawnArea;

    public void SpawnWave(int enemyCount, WaveManager waveManager, int currentWave)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnPosition = GetRandomPosition();
            GameObject enemyPrefab;

            if (currentWave >= advancedEnemyFromWave && advancedEnemyPrefabs.Count > 0 && Random.value < 0.3f)
                enemyPrefab = advancedEnemyPrefabs[Random.Range(0, advancedEnemyPrefabs.Count)];
            else
                enemyPrefab = basicEnemyPrefabs[Random.Range(0, basicEnemyPrefabs.Count)];

            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            EnemyWaveMember waveMember = enemyInstance.GetComponent<EnemyWaveMember>();

            if (waveMember != null)
            {
                waveMember.Initialize(waveManager);
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