using UnityEngine;

public class EnemyWaveMember : MonoBehaviour
{
    private WaveManager waveManager;
    private bool hasNotifiedDeath;

    // Para que lo que invoca o genera un enemigo cuente en su misma oleada.
    public WaveManager Manager => waveManager;

    public void Initialize(WaveManager manager)
    {
        waveManager = manager;

        if (waveManager != null)
        {
            waveManager.RegisterEnemy();
        }
    }

    public void NotifyDeath()
    {
        if (hasNotifiedDeath) return;

        hasNotifiedDeath = true;

        if (waveManager != null)
        {
            waveManager.OnEnemyKilled();
        }
    }
}