using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Separacion entre enemigos (issue #274): sus colliders son trigger y cada IA se mueve por su
// cuenta, asi que se apilaban. Corre antes que las IA (que parten de rb.position) y aparta un
// poco al enemigo de los vecinos que se le solapan. El empuje maximo (pushSpeed) es menor que
// la velocidad de persecucion, asi que siguen yendo a por el jugador. No empuja hacia fuera de
// los muros Wall_*. EnemyController lo anade a todos los enemigos normales; los jefes no.
[DefaultExecutionOrder(-50)]
public class EnemySeparation : MonoBehaviour
{
    private const float PushSpeed = 1.5f;     // unidades por segundo con solape total
    private const float RadiusFactor = 0.8f;  // radio respecto al collider (deja algo de roce)

    private static readonly List<Collider2D> hits = new List<Collider2D>();
    private static ContactFilter2D filter = new ContactFilter2D { useTriggers = true };
    private static Scene arenaScene;
    private static Rect? arena;

    private Rigidbody2D rb;
    private Collider2D col;
    private EnemyController controller;
    private EnemyArenaEntry entry;
    private float radius;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        controller = GetComponent<EnemyController>();
    }

    private void Start()
    {
        entry = GetComponent<EnemyArenaEntry>();
        if (col != null) radius = (col.bounds.extents.x + col.bounds.extents.y) * 0.5f * RadiusFactor;
    }

    private void FixedUpdate()
    {
        if (rb == null || radius <= 0f || controller == null || controller.IsDead) return;
        if (entry != null && entry.IsEntering) return;

        Vector2 pos = rb.position;
        hits.Clear();
        Physics2D.OverlapCircle(pos, radius * 2f, filter, hits);

        Vector2 push = Vector2.zero;
        foreach (var h in hits)
        {
            var other = h.attachedRigidbody != null ? h.attachedRigidbody.GetComponent<EnemySeparation>() : null;
            if (other == null || other == this || other.controller == null || other.controller.IsDead) continue;
            push += Push(pos, other.rb.position, radius + other.radius, Random.insideUnitCircle.normalized);
        }
        if (push == Vector2.zero) return;

        Vector2 step = Vector2.ClampMagnitude(push, 1f) * PushSpeed * Time.fixedDeltaTime;
        rb.position = KeepInside(pos, step, Arena(gameObject.scene), radius);
    }

    // Empuje para alejarse de 'other': 0 si estan a minDist o mas, hasta 1 si estan encima.
    // 'fallback' es la direccion si estan exactamente en el mismo punto.
    public static Vector2 Push(Vector2 self, Vector2 other, float minDist, Vector2 fallback)
    {
        Vector2 d = self - other;
        float dist = d.magnitude;
        if (minDist <= 0f || dist >= minDist) return Vector2.zero;
        Vector2 dir = dist > 0.0001f ? d / dist : fallback;
        return dir * (1f - dist / minDist);
    }

    // Aplica el paso salvo en el eje que lo sacaria de la arena (o lo meteria mas en el muro).
    public static Vector2 KeepInside(Vector2 pos, Vector2 step, Rect? arena, float margin)
    {
        if (arena.HasValue)
        {
            Rect a = arena.Value;
            if ((step.x < 0f && pos.x + step.x < a.xMin + margin) || (step.x > 0f && pos.x + step.x > a.xMax - margin)) step.x = 0f;
            if ((step.y < 0f && pos.y + step.y < a.yMin + margin) || (step.y > 0f && pos.y + step.y > a.yMax - margin)) step.y = 0f;
        }
        return pos + step;
    }

    private static Rect? Arena(Scene scene)
    {
        if (scene != arenaScene)
        {
            arenaScene = scene;
            arena = PlayerController.FindArenaInner(scene);
        }
        return arena;
    }
}
