using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> enemyPrefabs;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider2D spawnArea;

    public void SpawnWave(int enemyCount, WaveManager waveManager)
    {

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