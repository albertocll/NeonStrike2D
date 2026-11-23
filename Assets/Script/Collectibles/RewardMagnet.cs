using System.Collections.Generic;
using UnityEngine;

// Recompensas que suelta un jefe al morir: tras un instante van solas hacia la nave (imán) y se
// recogen al llegar (el trigger de Collectible o PowerUp). WaveManager espera a que no quede
// ninguna antes de cambiar de zona, con un máximo de segundos.
public class RewardMagnet : MonoBehaviour
{
    private static readonly HashSet<RewardMagnet> Active = new HashSet<RewardMagnet>();

    public static int PendingCount
    {
        get
        {
            Active.RemoveWhere(m => m == null);
            return Active.Count;
        }
    }

    [SerializeField] private float startDelay = 0.4f;
    [SerializeField] private float startSpeed = 4f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float maxSpeed = 30f;

    private float timer;
    private float speed;
    private Vector3 position;

    // Lo añade el jefe a cada recompensa que suelta.
    public static RewardMagnet Attach(GameObject reward)
    {
        if (reward == null) return null;
        var magnet = reward.GetComponent<RewardMagnet>();
        return magnet != null ? magnet : reward.AddComponent<RewardMagnet>();
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Start()
    {
        position = transform.position;
        speed = startSpeed;
    }

    // Después del Update de Collectible y PowerUp, que hacen flotar el objeto: aquí manda el imán.
    private void LateUpdate()
    {
        timer += Time.deltaTime;
        if (timer < startDelay)
        {
            position = transform.position;
            return;
        }

        Transform target = Target();
        if (target == null) return;

        speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);
        position = Vector3.MoveTowards(position, target.position, speed * Time.deltaTime);
        position.z = transform.position.z;
        transform.position = position;
    }

    // La nave de este jugador; si está fuera de combate (cooperativo), la del otro.
    private static Transform Target()
    {
        if (PlayerTargets.Local != null && !PlayerTargets.LocalDown) return PlayerTargets.Local;
        if (PlayerTargets.Remote != null && !PlayerTargets.RemoteDown) return PlayerTargets.Remote;
        var player = GameObject.FindWithTag("Player");
        return player != null ? player.transform : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();
}
