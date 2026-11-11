using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    private LevelData level;
    private int currentWave;
    private int enemiesAlive;
    private bool waveInProgress;

    public int CurrentWave => currentWave;

    private void Start()
    {
        level = LevelManager.ForScene(SceneManager.GetActiveScene().name);

        // Viniendo de otro nivel se sigue por donde iba la partida; si no, por startingWave
        // (o por la primera oleada del nivel, si se abre su escena directamente).
        if (RunState.Active)
            currentWave = RunState.Wave;
        else
            currentWave = Mathf.Max(startingWave, level != null ? level.firstWave : 1) - 1;

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnLoadLevel += HandleLoadLevel;

        StartCoroutine(StartWithDelay());
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnLoadLevel -= HandleLoadLevel;
    }

    public void StartNextWave()
    {
        if (!spawnEnemies) return;

        SFXManager.Instance?.PlayWaveStart();

        currentWave++;
        RunState.Wave = currentWave;
        waveInProgress = true;
        enemiesAlive = 0;

        if (waveText != null)
            waveText.text = $"WAVE: {currentWave}";

        if (level != null && currentWave == level.bossWave && level.bossPrefab != null)
            enemySpawner.SpawnBoss(level.bossPrefab, this);
        else
            enemySpawner.SpawnWave(EnemiesThisWave(currentWave), this, currentWave);
    }

    public int EnemiesForWave(int wave) => baseEnemiesPerWave + ((wave - 1) * enemiesAddedPerWave);

    // Lo que define el LevelData del nivel (primeras oleadas, subida y tope) o, si no, la formula.
    public int EnemiesThisWave(int wave) =>
        level != null ? level.EnemiesForWave(wave, EnemiesForWave(wave)) : EnemiesForWave(wave);

    public void RegisterEnemy() => enemiesAlive++;

    // Lo que invoca enemigos (Herald) no pasa del tope de la oleada actual.
    public bool WaveIsFull => enemiesAlive >= EnemiesThisWave(currentWave);

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

        // La siguiente oleada es de otro nivel: se acaba de limpiar la ultima de este.
        LevelData nextLevel = LevelManager.LevelForWave(currentWave + 1);
        if (nextLevel != null && nextLevel != level)
        {
            if (!GameData.IsCoop)
                LevelTransition.Begin(nextLevel);
            else if (GameData.IsHost)
                _ = NetworkManager.Instance.SendLevelCompleteAsync(GameData.RoomId, nextLevel.sceneName);
            // El invitado se queda sin enemigos nuevos hasta que el anfitrion de la orden (HandleLoadLevel).
            yield break;
        }

        StartNextWave();
    }

    // Orden del servidor en cooperativo: los dos jugadores cargan el nivel siguiente.
    private void HandleLoadLevel(string sceneName)
    {
        LevelTransition.Begin(LevelManager.ForScene(sceneName));
    }

    private IEnumerator StartWithDelay()
    {
        // Al llegar de otro nivel, se espera a que termine la transicion (en cooperativo, a los dos jugadores).
        while (LevelTransition.IsRunning)
            yield return null;

        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }
}
