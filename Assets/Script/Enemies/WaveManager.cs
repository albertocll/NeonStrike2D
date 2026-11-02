using System.Collections;
using UnityEngine;
using TMPro;

public class WaveManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool spawnEnemies = true;
    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private LevelManager levelManager;
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

    private void Awake()
    {
        levelManager.ShowLevel(levelManager.LevelForWave(startingWave));
    }

    private void Start()
    {
        currentWave = startingWave - 1;
        StartCoroutine(StartWithDelay());
    }

    public void StartNextWave()
    {
        if (!spawnEnemies) return;

        SFXManager.Instance?.PlayWaveStart();

        currentWave++;
        waveInProgress = true;
        enemiesAlive = 0;

        if (waveText != null)
            waveText.text = $"WAVE: {currentWave}";

        LevelData level = levelManager.CurrentLevel;
        if (level != null && currentWave == level.bossWave && level.bossPrefab != null)
            enemySpawner.SpawnBoss(level.bossPrefab, this);
        else
            enemySpawner.SpawnWave(EnemiesForWave(currentWave), this, currentWave);
    }

    public int EnemiesForWave(int wave) => baseEnemiesPerWave + ((wave - 1) * enemiesAddedPerWave);

    public void RegisterEnemy() => enemiesAlive++;

    public void OnEnemyKilled()
    {
        if (!waveInProgress) return;
        enemiesAlive--;
        if (enemiesAlive <= 0)
        {
            waveInProgress = false;
            SFXManager.Instance?.PlayWaveBonus();
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(50 * currentWave);
            StartCoroutine(StartNextWaveWithDelay());
        }
    }

    private IEnumerator StartNextWaveWithDelay()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        // Si la siguiente oleada es de otro nivel (se acaba de matar al jefe), se cambia de mapa antes.
        LevelData nextLevel = levelManager.LevelForWave(currentWave + 1);
        if (nextLevel != levelManager.CurrentLevel)
            yield return levelManager.TransitionTo(nextLevel);

        StartNextWave();
    }

    private IEnumerator StartWithDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }
}
