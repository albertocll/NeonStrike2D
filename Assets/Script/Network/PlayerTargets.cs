using UnityEngine;

// Jugadores a los que pueden atacar los enemigos (issue #206). En solitario solo esta el local;
// en cooperativo el anfitrion simula los enemigos y estos atacan tambien a la copia del invitado,
// cuyo PlayerHealth reenvia el dano por la red (PlayerHealth.MakeRemoteProxy).
public static class PlayerTargets
{
    public static Transform Local { get; set; }
    public static Transform Remote { get; set; }

    // Sustituye a CompareTag("Player") donde se hace dano: la copia del invitado no lleva la etiqueta
    // (si la llevara recogeria consumibles y FindGameObjectWithTag podria devolverla).
    public static bool IsPlayer(Component c) =>
        c.CompareTag("Player") || (Remote != null && c.transform.IsChildOf(Remote));

    // El jugador mas cercano a from; si no hay ninguno registrado, el objetivo que ya tenia.
    public static Transform Nearest(Vector2 from, Transform current)
    {
        Transform best = Local != null ? Local : current;
        if (Remote != null && (best == null || SqrDist(Remote, from) < SqrDist(best, from)))
            best = Remote;
        return best;
    }

    private static float SqrDist(Transform t, Vector2 from) => ((Vector2)t.position - from).sqrMagnitude;
}
