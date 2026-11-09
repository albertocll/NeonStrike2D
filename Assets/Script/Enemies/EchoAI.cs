using System.Collections;
using UnityEngine;

// Echo (issue #191): francotirador. Se aleja del jugador hasta su distancia preferida sin
// salir de la arena, se para a apuntar con una linea laser visible y dispara una bala lenta
// de cadencia pero de mucho dano. El ultimo tramo del apuntado la linea queda fija y parpadea:
// es el aviso para esquivar.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyController))]
public class EchoAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movimiento")]
    [SerializeField] private float speed = 3f;
    [Tooltip("Distancia a la que intenta quedarse del jugador.")]
    [SerializeField] private float preferredDistance = 9f;
    [Tooltip("Separacion minima con el borde de la arena al retroceder.")]
    [SerializeField] private float arenaMargin = 0.6f;

    [Header("Disparo")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int bulletDamage = 25;
    [SerializeField] private float bulletSpeed = 16f;
    [Tooltip("Mayor que el shootRange del Warden (5).")]
    [SerializeField] private float shootRange = 14f;
    [SerializeField] private float cooldown = 2.5f;
    [Tooltip("Tiempo apuntando (siguiendo al jugador) antes de fijar el tiro.")]
    [SerializeField] private float aimDuration = 1f;
    [Tooltip("Tiempo con la linea fija y parpadeando antes de disparar.")]
    [SerializeField] private float lockDuration = 0.3f;

    [Header("Linea de apuntado")]
    [SerializeField] private Color aimColor = new Color(1f, 0.2f, 0.6f, 0.35f);
    [SerializeField] private Color lockColor = new Color(1f, 0.2f, 0.6f, 0.9f);
    [SerializeField] private float aimWidth = 0.03f;
    [SerializeField] private float lockWidth = 0.07f;
    [Tooltip("Sprite-Unlit-Default: la linea no depende de las luces 2D.")]
    [SerializeField] private Material lineMaterial;

    private Rigidbody2D rb;
    private Collider2D myCol;
    private EnemyController controller;
    private EnemyArenaEntry entry;
    private Transform visual;
    private SpriteRenderer spriteRenderer;
    private LineRenderer aimLine;
    private Bounds? arena;
    private bool aiming;
    private bool cornered;
    private Coroutine shootRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        controller = GetComponent<EnemyController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        visual = spriteRenderer != null ? spriteRenderer.transform : transform;
        aimLine = CreateAimLine();
    }

    void Start()
    {
        entry = GetComponent<EnemyArenaEntry>();
        arena = FindArena();

        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    void OnDisable()
    {
        if (shootRoutine != null) StopCoroutine(shootRoutine);
        shootRoutine = null;
        aiming = false;
        if (aimLine != null) aimLine.enabled = false;
    }

    private bool Busy => controller.IsDead || (entry != null && entry.IsEntering);

    void FixedUpdate()
    {
        if (!target || Busy) return;

        if (aiming) return; // quieto mientras apunta

        Vector2 pos = rb.position;
        Vector2 away = pos - (Vector2)target.position;
        float dist = away.magnitude;

        Vector2 dir = Vector2.zero;
        if (dist < preferredDistance - 0.5f) dir = away.normalized;          // retrocede
        else if (dist > preferredDistance + 0.5f) dir = -away.normalized;    // se acerca a rango

        Vector2 step = dir * speed * Time.fixedDeltaTime;
        Vector2 next = ClampToArena(pos + step);
        // Acorralado contra el borde: ya no puede alejarse mas, asi que dispara desde ahi.
        cornered = dist < preferredDistance - 0.5f && (next - pos).magnitude < step.magnitude * 0.25f;
        if (dir != Vector2.zero) rb.MovePosition(next);
    }

    void Update()
    {
        if (controller.IsDead)
        {
            if (aimLine.enabled) aimLine.enabled = false;
            return;
        }
        if (!target || Busy) return;

        FaceTarget();

        // Solo apunta cuando ya esta a su distancia (o no puede alejarse mas): no se queda
        // quieto disparando con el jugador encima.
        float dist = Vector2.Distance(transform.position, target.position);
        bool inPosition = dist >= preferredDistance - 1.5f || cornered;
        if (shootRoutine == null && bulletPrefab != null && dist <= shootRange && inPosition)
            shootRoutine = StartCoroutine(AimAndShoot());
    }

    private IEnumerator AimAndShoot()
    {
        aiming = true;
        aimLine.enabled = true;

        // Apuntado: la linea sigue al jugador.
        for (float t = 0f; t < aimDuration; t += Time.deltaTime)
        {
            if (controller.IsDead || !target) { EndShot(); shootRoutine = null; yield break; }
            DrawLine(((Vector2)target.position - (Vector2)transform.position).normalized, aimColor, aimWidth);
            yield return null;
        }

        // Fijado: la direccion ya no cambia y la linea parpadea.
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        for (float t = 0f; t < lockDuration; t += Time.deltaTime)
        {
            if (controller.IsDead) { EndShot(); shootRoutine = null; yield break; }
            bool on = Mathf.Repeat(t, 0.1f) < 0.05f;
            DrawLine(dir, on ? lockColor : aimColor, on ? lockWidth : aimWidth);
            yield return null;
        }

        Bullet b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        b.damage = bulletDamage;
        b.speed = bulletSpeed;
        b.Init(dir);
        if (myCol != null) b.IgnoreCollider(myCol);

        EndShot();
        yield return new WaitForSeconds(cooldown);
        shootRoutine = null;
    }

    private void EndShot()
    {
        aiming = false;
        aimLine.enabled = false;
    }

    private void DrawLine(Vector2 dir, Color color, float width)
    {
        Vector3 from = transform.position;
        aimLine.SetPosition(0, from);
        aimLine.SetPosition(1, from + (Vector3)(dir * shootRange));
        aimLine.startColor = aimLine.endColor = color;
        aimLine.startWidth = aimLine.endWidth = width;
    }

    // El sprite mira a la derecha: se gira hacia el jugador y se voltea en vertical
    // cuando apunta a la izquierda para que no quede boca abajo.
    private void FaceTarget()
    {
        Vector2 d = (Vector2)target.position - (Vector2)transform.position;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        if (spriteRenderer != null) spriteRenderer.flipY = Mathf.Abs(angle) > 90f;
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

    private LineRenderer CreateAimLine()
    {
        var go = new GameObject("AimLine");
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.sharedMaterial = lineMaterial;
        line.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        line.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : 0;
        line.enabled = false;
        return line;
    }
}
