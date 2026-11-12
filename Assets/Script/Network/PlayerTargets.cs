using UnityEngine;

// Jugadores a los que pueden atacar los enemigos (issue #206). En solitario solo esta el local;
// en cooperativo el anfitrion simula los enemigos y estos atacan tambien a la copia del invitado,
// cuyo PlayerHealth reenvia el dano por la red (PlayerHealth.MakeRemoteProxy).
public static class PlayerTargets
{
    public static Transform Local { get; set; }
    public static Transform Remote { get; set; }

    // Fuera de combate en cooperativo (#296): los enemigos no los persiguen ni les hacen dano.
    public static bool LocalDown { get; set; }
    public static bool RemoteDown { get; set; }

    // Sustituye a CompareTag("Player") donde se hace dano: la copia del invitado no lleva la etiqueta
    // (si la llevara recogeria consumibles y FindGameObjectWithTag podria devolverla).
    public static bool IsPlayer(Component c) =>
        c.CompareTag("Player") || (Remote != null && !RemoteDown && c.transform.IsChildOf(Remote));

    // El jugador mas cercano a from que no este fuera de combate; si no hay ninguno, el objetivo que ya tenia.
    public static Transform Nearest(Vector2 from, Transform current)
    {
        Transform best = Local != null && !LocalDown ? Local : null;
        if (Remote != null && !RemoteDown && (best == null || SqrDist(Remote, from) < SqrDist(best, from)))
            best = Remote;
        return best != null ? best : current;
    }

    private static float SqrDist(Transform t, Vector2 from) => ((Vector2)t.position - from).sqrMagnitude;
}
