using UnityEngine;

// Lista de niveles: todos los LevelData de Resources/Levels, ordenados por primera oleada.
public static class LevelManager
{
    private const string LevelsFolder = "Levels";

    private static LevelData[] levels;

    public static LevelData[] Levels => levels ??= LoadLevels();

    public static LevelData FirstLevel => Levels.Length > 0 ? Levels[0] : null;

    public static LevelData LevelForWave(int wave) => LevelForWave(Levels, wave);

    // El ultimo nivel cuya primera oleada ya se ha alcanzado; pasado el ultimo, se queda en el.
    public static LevelData LevelForWave(LevelData[] levels, int wave)
    {
        LevelData result = levels.Length > 0 ? levels[0] : null;
        foreach (LevelData level in levels)
        {
            if (level != null && wave >= level.firstWave)
                result = level;
        }
        return result;
    }

    public static LevelData ForScene(string sceneName)
    {
        foreach (LevelData level in Levels)
        {
            if (level != null && level.sceneName == sceneName)
                return level;
        }
        return null;
    }

    public static bool IsLevelScene(string sceneName) => ForScene(sceneName) != null;

    // Partida nueva o reinicio: se borra el estado de la partida y se empieza en el primer nivel.
    public static void StartNewRun()
    {
        RunState.Clear();
        SceneLoader.Load(FirstLevel.sceneName);
    }

    private static LevelData[] LoadLevels()
    {
        LevelData[] loaded = Resources.LoadAll<LevelData>(LevelsFolder);
        System.Array.Sort(loaded, (a, b) => a.firstWave.CompareTo(b.firstWave));
        return loaded;
    }
}
