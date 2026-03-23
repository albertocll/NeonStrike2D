using UnityEngine;

public class EnemyWaveMember : MonoBehaviour
{
    private WaveManager waveManager;
    private bool hasNotifiedDeath;

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