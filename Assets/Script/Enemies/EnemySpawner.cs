using System.Collections;
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
        StartCoroutine(SpawnWaveRoutine(enemyCount, waveManager, currentWave));
    }

    // ponytail: 1 Instantiate por frame evita el hitch de spawnear toda la oleada
    // de golpe. Si una oleada gigante (p.ej. 30+) tardara demasiado en completarse,
    // subir a 2-3 por frame en vez de 1.
    private IEnumerator SpawnWaveRoutine(int enemyCount, WaveManager waveManager, int currentWave)
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

            yield return null;
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