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
    [SerializeField] private float spawnMargin = 1f;
    [SerializeField] private RectTransform[] uiExclusionZones;

    private const int MaxSpawnAttempts = 10;
    private const float ExclusionPadding = 0.5f;

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
            GameObject enemyPrefab = ChooseEnemyPrefab(currentWave);

            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            EnemyWaveMember waveMember = enemyInstance.GetComponent<EnemyWaveMember>();

            if (waveMember != null)
            {
                waveMember.Initialize(waveManager);
            }

            yield return null;
        }
    }

    public GameObject ChooseEnemyPrefab(int currentWave)
    {
        if (currentWave >= advancedEnemyFromWave && advancedEnemyPrefabs.Count > 0 && Random.value < 0.3f)
            return advancedEnemyPrefabs[Random.Range(0, advancedEnemyPrefabs.Count)];
        return basicEnemyPrefabs[Random.Range(0, basicEnemyPrefabs.Count)];
    }

    private Vector2 GetRandomPosition()
    {
        Bounds bounds = spawnArea.bounds;

        float minX = bounds.min.x + spawnMargin;
        float maxX = bounds.max.x - spawnMargin;
        float minY = bounds.min.y + spawnMargin;
        float maxY = bounds.max.y - spawnMargin;

        if (minX > maxX) minX = maxX = bounds.center.x;
        if (minY > maxY) minY = maxY = bounds.center.y;

        Vector2 candidate = Vector2.zero;
        for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
        {
            candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
            if (!IsInsideExclusionZone(candidate))
                return candidate;
        }

        return candidate;
    }

    private bool IsInsideExclusionZone(Vector2 point)
    {
        if (uiExclusionZones == null || uiExclusionZones.Length == 0) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        float z = -cam.transform.position.z;
        Vector3[] corners = new Vector3[4];

        foreach (RectTransform zone in uiExclusionZones)
        {
            if (zone == null) continue;

            zone.GetWorldCorners(corners);
            Vector3 min = cam.ScreenToWorldPoint(new Vector3(corners[0].x, corners[0].y, z));
            Vector3 max = cam.ScreenToWorldPoint(new Vector3(corners[2].x, corners[2].y, z));

            if (point.x >= min.x - ExclusionPadding && point.x <= max.x + ExclusionPadding &&
                point.y >= min.y - ExclusionPadding && point.y <= max.y + ExclusionPadding)
            {
                return true;
            }
        }

        return false;
    }
}