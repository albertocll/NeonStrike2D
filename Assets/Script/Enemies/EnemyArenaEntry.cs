using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Entrada desde fuera de la arena (issue #225). EnemySpawner lo anade al enemigo recien creado
// fuera de los Wall_*: con su IA apagada, camina hacia su punto de destino dentro de la arena.
// Los colliders de los enemigos son trigger, asi que atraviesan el muro sin chocar.
// Los que persiguen al jugador recuperan su IA en cuanto estan dentro; los que no se mueven
// solos (Fuse) siguen hasta su punto de destino y se quedan ahi, como antes.
public class EnemyArenaEntry : MonoBehaviour
{
    private const string MovingParam = "Moving";

    public bool IsEntering { get; private set; }

    public void Begin(Vector2 destination, Bounds arena, float halfExtent, float speed)
    {
        StartCoroutine(Enter(destination, arena, halfExtent, speed));
    }

    private IEnumerator Enter(Vector2 destination, Bounds arena, float halfExtent, float speed)
    {
        IsEntering = true;

        // IA apagada durante la entrada. FuseAI tambien, para que no detone mientras entra.
        var paused = new List<Behaviour>();
        Pause<StrikerAI>(paused);
        Pause<WardenAI>(paused);
        Pause<EnemyMovement>(paused);
        Pause<FuseAI>(paused);

        bool chases = GetComponent<StrikerAI>() != null || GetComponent<WardenAI>() != null;
        var controller = GetComponent<EnemyController>();
        var rb = GetComponent<Rigidbody2D>();
        Animator anim = GetComponentInChildren<Animator>();
        bool hasMovingParam = HasBoolParam(anim, MovingParam);
        if (hasMovingParam) anim.SetBool(MovingParam, true);

        // Zona en la que el enemigo ya esta entero dentro de la arena.
        Vector2 innerMin = (Vector2)arena.min + Vector2.one * halfExtent;
        Vector2 innerMax = (Vector2)arena.max - Vector2.one * halfExtent;

        while (true)
        {
            if (controller != null && controller.IsDead) yield break; // muerto: no se le devuelve la IA

            Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
            if (pos == destination) break;
            if (chases && IsInside(pos, innerMin, innerMax)) break;

            Vector2 next = Vector2.MoveTowards(pos, destination, speed * Time.fixedDeltaTime);
            if (rb != null) rb.MovePosition(next);
            else transform.position = next;

            yield return new WaitForFixedUpdate();
        }

        if (rb != null && rb.bodyType != RigidbodyType2D.Static) rb.linearVelocity = Vector2.zero;
        if (hasMovingParam && !chases) anim.SetBool(MovingParam, false);

        foreach (var b in paused)
            if (b != null) b.enabled = true;

        IsEntering = false;
    }

    private void Pause<T>(List<Behaviour> paused) where T : Behaviour
    {
        var b = GetComponent<T>();
        if (b == null || !b.enabled) return;
        b.enabled = false;
        paused.Add(b);
    }

    private static bool IsInside(Vector2 p, Vector2 min, Vector2 max) =>
        p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;

    private static bool HasBoolParam(Animator anim, string name)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (var p in anim.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool) return true;
        return false;
    }
}
