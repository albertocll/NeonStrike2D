using UnityEngine;

// Flak (issue #194): artillero. Se queda a rango medio-largo del jugador (mas cerca que Echo,
// mas lejos que Warden) sin salir de la arena y, con cadencia lenta, lanza un FlakShell a la
// posicion del jugador. El proyectil marca la zona de impacto antes de explotar.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyController))]
public class FlakAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movimiento")]
    [SerializeField] private float speed = 2.5f;
    [Tooltip("Entre la del Warden (stopDistance ~0.5, shootRange 5) y la de Echo (9).")]
    [SerializeField] private float preferredDistance = 6.5f;
    [SerializeField] private float arenaMargin = 0.6f;

    [Header("Disparo")]
    [SerializeField] private FlakShell shellPrefab;
    [SerializeField] private float shootRange = 10f;
    [SerializeField] private float cooldown = 3.5f;

    private Rigidbody2D rb;
    private EnemyController controller;
    private EnemyArenaEntry entry;
    private Transform visual;
    private SpriteRenderer spriteRenderer;
    private Bounds? arena;
    private float nextShotTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<EnemyController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        visual = spriteRenderer != null ? spriteRenderer.transform : transform;
    }

    void Start()
    {
        entry = GetComponent<EnemyArenaEntry>();
        arena = FindArena();
        nextShotTime = Time.time + cooldown * 0.5f;

        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    private bool Busy => controller.IsDead || (entry != null && entry.IsEntering);

    void FixedUpdate()
    {
        target = PlayerTargets.Nearest(transform.position, target); // el jugador mas cercano (#206)
        if (!target || Busy) return;

        Vector2 pos = rb.position;
        Vector2 away = pos - (Vector2)target.position;
        float dist = away.magnitude;

        Vector2 dir = Vector2.zero;
        if (dist < preferredDistance - 0.5f) dir = away.normalized;
        else if (dist > preferredDistance + 0.5f) dir = -away.normalized;

        if (dir != Vector2.zero)
            rb.MovePosition(ClampToArena(pos + dir * speed * Time.fixedDeltaTime));
    }

    void Update()
    {
        if (!target || Busy) return;

        Vector2 d = (Vector2)target.position - (Vector2)transform.position;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (spriteRenderer != null) spriteRenderer.flipY = Mathf.Abs(angle) > 90f;

        if (shellPrefab == null || Time.time < nextShotTime || d.magnitude > shootRange) return;
        nextShotTime = Time.time + cooldown;

        FlakShell shell = Instantiate(shellPrefab, transform.position, Quaternion.identity);
        shell.Init(target.position);
    }

    private Vector2 ClampToArena(Vector2 p)
    {
        if (!arena.HasValue) return p;
        Vector2 min = (Vector2)arena.Value.min + Vector2.one * arenaMargin;
        Vector2 max = (Vector2)arena.Value.max - Vector2.one * arenaMargin;
        return new Vector2(Mathf.Clamp(p.x, min.x, max.x), Mathf.Clamp(p.y, min.y, max.y));
    }

    private static Bounds? FindArena()
    {
        foreach (var box in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            if (box.name == "SpawnArea") return box.bounds;
        return null;
    }
}
