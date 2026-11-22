namespace NeonStrike2D.Backend.Api.Security;

// Límites de un resultado de partida, sacados de los datos del juego:
// - Enemigos por oleada: niveles 1 y 2 con 3 + (oleada - 1); del 3 al 6, 18-24 + 1 por oleada;
//   Sector 7 (desde la 61, sin fin) con tope de 35. Para cualquier oleada: como mucho min(oleada + 2, 35).
// - El enemigo que más da son 150 puntos (Anvil).
// - Al limpiar la oleada w se suman 50 * w.
// - Jefe: como mucho 3000 (Nexus Core), uno cada 10 oleadas y 6 en total.
// - Coleccionables: uno cada 4,5 s y como mucho 150 cada uno: 2000 puntos por minuto. Junto con los
//   enemigos que invocan Herald y algunos jefes, depende del tiempo; se cuenta 1 minuto por oleada.
// El tope es el doble de la suma de todo eso.
public static class MatchResultRules
{
    public const int MaxWave = 1000;

    private const int MaxEnemiesPerWave = 35;
    private const int MaxEnemyScore = 150;
    private const int WaveBonusPerWave = 50;
    private const int MaxBossScore = 3000;
    private const int BossCount = 6;
    private const int TimedPointsPerWave = 2000;
    private const int Margin = 2;

    public static long MaxScoreFor(int wave)
    {
        int waves = Math.Max(wave, 1);
        long total = 0;
        for (int w = 1; w <= waves; w++)
        {
            total += (long)MaxEnemyScore * Math.Min(w + 2, MaxEnemiesPerWave);
            total += (long)WaveBonusPerWave * w;
            total += TimedPointsPerWave;
        }
        total += (long)MaxBossScore * Math.Min(waves / 10, BossCount);
        return total * Margin;
    }

    public static bool IsValid(int bestWave, int score) =>
        bestWave >= 0 && bestWave <= MaxWave && score >= 0 && score <= MaxScoreFor(bestWave);
}
