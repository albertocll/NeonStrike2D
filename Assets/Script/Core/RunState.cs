using System.Collections.Generic;
using UnityEngine;

// Estado de la partida que sobrevive al cambio de escena entre niveles.
// Puntuacion y oleada se mantienen al dia durante la partida; vida y power-ups
// se guardan al salir del nivel (Capture).
public static class RunState
{
    // True desde que se pasa de nivel hasta que empieza una partida nueva.
    public static bool Active { get; private set; }
    public static int Wave { get; set; }
    public static int Score { get; set; }
    public static int Health { get; private set; }
    public static readonly Dictionary<PowerUp.PowerUpType, float> PowerUpTimes = new Dictionary<PowerUp.PowerUpType, float>();

    public static void Clear()
    {
        Active = false;
        Wave = 0;
        Score = 0;
        Health = 0;
        PowerUpTimes.Clear();
    }

    // Guarda lo que vive en el jugador local antes de cargar el nivel siguiente.
    // wave es la ultima oleada completada: el nivel siguiente empieza en wave + 1.
    public static void Capture(GameObject player, int wave)
    {
        Active = true;
        Wave = wave;
        PowerUpTimes.Clear();

        if (player == null) return;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) Health = health.CurrentHealth;

        PlayerPowerUps powerUps = player.GetComponent<PlayerPowerUps>();
        if (powerUps == null) return;

        foreach (PowerUp.PowerUpType type in System.Enum.GetValues(typeof(PowerUp.PowerUpType)))
        {
            float remaining = powerUps.RemainingTime(type);
            if (remaining > 0f) PowerUpTimes[type] = remaining;
        }
    }
}
