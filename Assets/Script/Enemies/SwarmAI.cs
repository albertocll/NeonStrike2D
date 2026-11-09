using System.Collections;
using UnityEngine;

// Swarm (issue #193): unidad pequena, de un golpe, que persigue al jugador. El spawner crea
// una sola (la lider); al terminar de entrar en la arena genera a su lado al resto del racimo
// (groupSize en total), una por frame, y las apunta en la misma oleada. Cada unidad persigue
// al jugador con un desplazamiento propio para que lo rodeen en vez de amontonarse.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyController))]
public class SwarmAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Racimo")]
    [Tooltip("Unidades por aparicion, contando la lider.")]
    [SerializeField] private int groupSize = 5;
    [SerializeField] private float groupSpawnRadius = 0.6f;

    [Header("Movimiento")]
    [SerializeField] private float speed = 4.5f;
    [Tooltip("Radio del punto alrededor del jugador al que va cada unidad.")]
    [SerializeField] private float surroundRadius = 0.7f;

    [Header("Dano de contacto")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float damageCooldown = 1f;

    private Rigidbody2D rb;
    private EnemyController controller;
    private EnemyArenaEntry entry;
    private SpriteRenderer spriteRenderer;
    private Vector2 surroundOffset;
    private float lastDamageTime;
    private bool isFollower; // no serializado: las copias nacen como lider hasta que se marcan

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<EnemyController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        surroundOffset = Random.insideUnitCircle * surroundRadius;
    }

    void Start()
    {
        entry = GetComponent<EnemyArenaEntry>();

        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        if (!isFollower && groupSize > 1)
            StartCoroutine(SpawnGroup());
    }

    private bool Busy => controller.IsDead || (entry != null && entry.IsEntering);

    private IEnumerator SpawnGroup()
    {
        while (entry != null && entry.IsEntering)
        {
            if (controller.IsDead) yield break;
            yield return null;
        }

        WaveManager manager = GetComponent<EnemyWaveMember>()?.Manager;

        // ponytail: una por frame, como EnemySpawner (#170), para no meter un tiron al aparecer.
        for (int i = 1; i < groupSize; i++)
        {
            if (controller.IsDead) yield break;

            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * groupSpawnRadius;
            GameObject unit = Instantiate(gameObject, pos, Quaternion.identity);
            unit.GetComponent<SwarmAI>().isFollower = true;
            unit.GetComponent<EnemyWaveMember>()?.Initialize(manager);

            yield return null;
        }
    }

    void FixedUpdate()
    {
        if (!target || Busy) return;

        Vector2 goal = (Vector2)target.position + surroundOffset;
        Vector2 next = Vector2.MoveTowards(rb.position, goal, speed * Time.fixedDeltaTime);
        if (spriteRenderer != null && Mathf.Abs(next.x - rb.position.x) > 0.001f)
            spriteRenderer.flipX = next.x < rb.position.x;
        rb.MovePosition(next);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamagePlayer(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamagePlayer(other);

    private void TryDamagePlayer(Collider2D other)
    {
        if (!enabled || Busy || !other.CompareTag("Player")) return;
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && !health.IsDead)
            health.TakeDamage(contactDamage);
    }
}
