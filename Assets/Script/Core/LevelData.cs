using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "NeonStrike2D/LevelData")]
public class LevelData : ScriptableObject
{
    public string sceneName;
    public string displayName;
    public int firstWave = 1;
    public int bossWave = 10;
    public GameObject bossPrefab;
    [Tooltip("Multiplica la vida de los enemigos de las oleadas del nivel (1 = la del prefab).")]
    public float enemyHealthMultiplier = 1f;

    [Header("Enemigos por oleada")]
    [Tooltip("Enemigos de la primera oleada del nivel. 0 = la formula de WaveManager (3 + 1 por oleada desde la 1).")]
    public int firstWaveEnemies = 0;
    [Tooltip("Enemigos que se suman en cada oleada siguiente del nivel.")]
    public int enemiesAddedPerWave = 1;
    [Tooltip("Tope de enemigos por oleada. 0 = sin tope.")]
    public int maxEnemiesPerWave = 0;

    // Enemigos de la oleada 'wave' segun el nivel, o 'fallback' si el nivel no lo define.
    public int EnemiesForWave(int wave, int fallback)
    {
        int count = firstWaveEnemies > 0
            ? firstWaveEnemies + Mathf.Max(0, wave - firstWave) * enemiesAddedPerWave
            : fallback;
        return maxEnemiesPerWave > 0 ? Mathf.Min(count, maxEnemiesPerWave) : count;
    }
}
