using System;

// Lo que BossHealthBarUI necesita de un jefe, sea el Sentinel o uno de BossBase.
public interface IBoss
{
    string DisplayName { get; }
    event Action<float> OnHealthChanged;
    event Action OnDefeated;
    event Action OnCombatStarted;
}

// Opcional: jefes con fases por vida. La barra marca cada umbral (fraccion de vida 0..1).
public interface IBossPhases
{
    float[] PhaseThresholds { get; }
}

public static class BossEvents
{
    public static event Action<IBoss> OnBossSpawned;

    public static void RaiseSpawned(IBoss boss) => OnBossSpawned?.Invoke(boss);
}

// Jefe que puede ser una copia sin IA en el invitado de una partida cooperativa (#206).
public interface IRemoteBoss
{
    int CurrentHealth { get; }
    bool IsDead { get; }
    bool IsInvulnerable { get; }
    void BeginRemote();
    void RemoteCombatStart();
    void ApplyRemoteHealth(int health);
    void RemoteDie();
    bool AwardScore { get; set; }
}
