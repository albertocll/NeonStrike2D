using System;

// Lo que BossHealthBarUI necesita de un jefe, sea el Sentinel o uno de BossBase.
public interface IBoss
{
    string DisplayName { get; }
    event Action<float> OnHealthChanged;
    event Action OnDefeated;
    event Action OnCombatStarted;
}

public static class BossEvents
{
    public static event Action<IBoss> OnBossSpawned;

    public static void RaiseSpawned(IBoss boss) => OnBossSpawned?.Invoke(boss);
}
