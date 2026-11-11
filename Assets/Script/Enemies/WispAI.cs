using System.Collections;
using UnityEngine;

// Wisp (issue #196): persigue al jugador alternando dos fases. Tangible: opaco, hace dano por
// contacto y se le puede disparar. Intangible: semitransparente y con el collider apagado, asi
// que las balas lo atraviesan y no dana. Antes de cada cambio de fase parpadea para avisar.
// Los enemigos ya cruzan los muros (colliders trigger), asi que no hace falta nada mas para
// que atraviese obstaculos.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyController))]
public class WispAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movimiento")]
    [SerializeField] private float speed = 3f;

    [Header("Fases")]
    [SerializeField] private float tangibleDuration = 3f;
    [SerializeField] private float intangibleDuration = 2f;
    [Tooltip("Parpadeo de aviso al final de cada fase (incluido en su duracion).")]
    [SerializeField] private float warningDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float intangibleAlpha = 0.3f;

    [Header("Dano de contacto")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float damageCooldown = 1f;

    private Rigidbody2D rb;
    private Collider2D myCol;
    private EnemyController controller;
    private EnemyArenaEntry entry;
    private SpriteRenderer spriteRenderer;
    private float lastDamageTime;

    public bool IsTangible { get; private set; } = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        controller = GetComponent<EnemyController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        entry = GetComponent<EnemyArenaEntry>();

        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        StartCoroutine(PhaseLoop());
    }

    private bool Busy => controller.IsDead || (entry != null && entry.IsEntering);

    private IEnumerator PhaseLoop()
    {
        // Entra en la arena tangible; el ciclo empieza cuando ya esta dentro.
        while (entry != null && entry.IsEntering) yield return null;

        while (!controller.IsDead)
        {
            yield return Phase(true, tangibleDuration);
            if (controller.IsDead) yield break;
            yield return Phase(false, intangibleDuration);
        }
    }

    private IEnumerator Phase(bool tangible, float duration)
    {
        SetTangible(tangible);
        float steady = Mathf.Max(0f, duration - warningDuration);
        yield return new WaitForSeconds(steady);

        // Aviso: alterna entre el alfa de esta fase y el de la siguiente.
        float a = tangible ? 1f : intangibleAlpha;
        float b = tangible ? intangibleAlpha : 1f;
        for (float t = 0f; t < duration - steady && !controller.IsDead; t += Time.deltaTime)
        {
            SetAlpha(Mathf.Repeat(t, 0.12f) < 0.06f ? b : a);
            yield return null;
        }
    }

    private void SetTangible(bool tangible)
    {
        if (controller.IsDead) return;
        IsTangible = tangible;
        if (myCol != null) myCol.enabled = tangible;
        SetAlpha(tangible ? 1f : intangibleAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    void FixedUpdate()
    {
        target = PlayerTargets.Nearest(transform.position, target); // el jugador mas cercano (#206)
        if (!target || Busy) return;

        Vector2 next = Vector2.MoveTowards(rb.position, target.position, speed * Time.fixedDeltaTime);
        if (spriteRenderer != null && Mathf.Abs(next.x - rb.position.x) > 0.001f)
            spriteRenderer.flipX = next.x < rb.position.x;
        rb.MovePosition(next);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamagePlayer(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamagePlayer(other);

    private void TryDamagePlayer(Collider2D other)
    {
        if (!enabled || Busy || !IsTangible || !PlayerTargets.IsPlayer(other)) return;
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && !health.IsDead)
            health.TakeDamage(contactDamage);
    }
}
