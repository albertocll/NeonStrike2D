using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Herald (issue #195): invocador. No ataca: se queda lejos del jugador sin salir de la arena y
// cada cierto tiempo, tras un aviso (parpadeo verde y pulso), invoca un enemigo basico a su
// lado. Nunca tiene mas de maxAlive invocados vivos a la vez. Los invocados cuentan en la
// oleada del Herald, asi que la oleada no termina mientras sigan vivos.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyController))]
public class HeraldAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movimiento")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float preferredDistance = 7f;
    [SerializeField] private float arenaMargin = 0.6f;

    [Header("Invocacion")]
    [Tooltip("Enemigo basico que invoca (EnemyStriker).")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private float summonInterval = 5f;
    [SerializeField] private int maxAlive = 3;
    [SerializeField] private float summonDistance = 0.9f;

    [Header("Aviso de invocacion")]
    [SerializeField] private float telegraphDuration = 0.7f;
    [SerializeField] private Color telegraphColor = new Color(0.4f, 1f, 0.5f, 1f);
    [SerializeField] private float telegraphPulse = 0.25f;

    private Rigidbody2D rb;
    private EnemyController controller;
    private EnemyArenaEntry entry;
    private SpriteRenderer spriteRenderer;
    private Bounds? arena;
    private readonly List<GameObject> summoned = new List<GameObject>();
    private bool summoning;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<EnemyController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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

        StartCoroutine(SummonLoop());
    }

    private bool Busy => controller.IsDead || (entry != null && entry.IsEntering);

    void FixedUpdate()
    {
        target = PlayerTargets.Nearest(transform.position, target); // el jugador mas cercano (#206)
        if (!target || Busy || summoning) return;

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
        if (!target || controller.IsDead || spriteRenderer == null) return;
        spriteRenderer.flipX = target.position.x < transform.position.x;
    }

    private IEnumerator SummonLoop()
    {
        while (!controller.IsDead)
        {
            yield return new WaitForSeconds(summonInterval);
            if (Busy || summonPrefab == null) continue;

            summoned.RemoveAll(s => s == null || s.GetComponent<EnemyController>()?.IsDead == true);
            if (summoned.Count >= maxAlive) continue;
            if (TryGetComponent(out EnemyWaveMember member) && member.Manager != null && member.Manager.WaveIsFull) continue;

            yield return Telegraph();
            if (controller.IsDead) yield break;

            Vector2 pos = ClampToArena((Vector2)transform.position + Random.insideUnitCircle.normalized * summonDistance);
            GameObject add = Instantiate(summonPrefab, pos, Quaternion.identity);
            add.GetComponent<EnemyWaveMember>()?.Initialize(GetComponent<EnemyWaveMember>()?.Manager);
            summoned.Add(add);
        }
    }

    // Se para, parpadea en verde y late: aviso de que va a invocar.
    private IEnumerator Telegraph()
    {
        summoning = true;
        Vector3 baseScale = spriteRenderer != null ? spriteRenderer.transform.localScale : Vector3.one;

        for (float t = 0f; t < telegraphDuration && !controller.IsDead; t += Time.deltaTime)
        {
            if (spriteRenderer != null)
            {
                bool on = Mathf.Repeat(t, 0.15f) < 0.075f;
                spriteRenderer.color = on ? telegraphColor : Color.white;
                float k = 1f + telegraphPulse * Mathf.Sin(t / telegraphDuration * Mathf.PI);
                spriteRenderer.transform.localScale = baseScale * k;
            }
            yield return null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.transform.localScale = baseScale;
        }
        summoning = false;
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
