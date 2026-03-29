using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Wave Settings")]
    [SerializeField] private int startingWave = 1;
    [SerializeField] private int baseEnemiesPerWave = 3;
    [SerializeField] private int enemiesAddedPerWave = 2;
    [SerializeField] private float timeBetweenWaves = 2f;

    private int currentWave;
    private int enemiesAlive;
    private bool waveInProgress;

    private void Start()
    {
        currentWave = startingWave - 1;
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (enemySpawner == null)
        {
            Debug.LogError("[WaveManager] EnemySpawner no asignado.");
            return;
        }

        currentWave++;
        waveInProgress = true;

        enemiesAlive = 0; // 👈 importante

        int enemiesToSpawn = baseEnemiesPerWave + ((currentWave - 1) * enemiesAddedPerWave);

        Debug.Log($"[WaveManager] Iniciando Wave {currentWave} con {enemiesToSpawn} enemigos.");

        enemySpawner.SpawnWave(enemiesToSpawn, this);
    }
    public void OnEnemyKilled()
    {
        if (!waveInProgress) return;

        enemiesAlive--;
        Debug.Log($"[WaveManager] Enemigo eliminado. Quedan: {enemiesAlive}");

        if (enemiesAlive <= 0)
        {
            waveInProgress = false;
            Debug.Log($"[WaveManager] Wave {currentWave} completada.");
            StartCoroutine(StartNextWaveWithDelay());
        }
    }

    private IEnumerator StartNextWaveWithDelay()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        StartNextWave();
    }
    public void RegisterEnemy()
    {
        enemiesAlive++;
    }
    
    void Update()
    {
        int realEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;

        //Debug.Log($"[WaveManager] Contador: {enemiesAlive} | Reales en escena: {realEnemies}");
    }
}