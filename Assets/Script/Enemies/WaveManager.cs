using System.Collections;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool spawnEnemies = true;
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private TMP_Text waveText;

    [Header("Wave Settings")]
    [SerializeField] private int startingWave = 1;
    [SerializeField] private int baseEnemiesPerWave = 3;
    [SerializeField] private int enemiesAddedPerWave = 2;
    [SerializeField] private float timeBetweenWaves = 2f;
    [SerializeField] private float initialDelay = 3f;

    private int currentWave;
    private int enemiesAlive;
    private bool waveInProgress;

    public int CurrentWave => currentWave;

    private void Start()
    {
        currentWave = startingWave - 1;
        StartCoroutine(StartWithDelay());
    }

    public void StartNextWave()
    {
        if (!spawnEnemies) return;
        if (enemySpawner == null)
        {
            Debug.LogError("[WaveManager] EnemySpawner no asignado.");
            return;
        }

        currentWave++;
        waveInProgress = true;
        enemiesAlive = 0;

        if (waveText != null)
            waveText.text = $"WAVE: {currentWave}";

        int enemiesToSpawn = baseEnemiesPerWave + ((currentWave - 1) * enemiesAddedPerWave);
        enemySpawner.SpawnWave(enemiesToSpawn, this);
    }

    public void RegisterEnemy() => enemiesAlive++;

    public void OnEnemyKilled()
    {
        if (!waveInProgress) return;
        enemiesAlive--;
        if (enemiesAlive <= 0)
        {
            waveInProgress = false;
            StartCoroutine(StartNextWaveWithDelay());
        }
    }

    private IEnumerator StartNextWaveWithDelay()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        StartNextWave();
    }

    private IEnumerator StartWithDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }
}