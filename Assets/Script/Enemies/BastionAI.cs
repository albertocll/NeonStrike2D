using UnityEngine;

// Bastion (issue #192): avanza despacio hacia el jugador con el escudo delante. Las balas que
// le llegan de frente (dentro de shieldHalfAngle) se bloquean; por los lados y la espalda
// recibe dano normal. Gira con una velocidad limitada para que se le pueda flanquear.
// Bullet consulta BlocksHit antes de aplicar el dano.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyController))]
public class BastionAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movimiento")]
    [SerializeField] private float speed = 2f;
    [Tooltip("Grados por segundo. Bajo = mas facil rodearlo.")]
    [SerializeField] private float turnSpeed = 90f;

    [Header("Escudo")]
    [Tooltip("Medio angulo del cono frontal que bloquea (60 = 120 grados en total).")]
    [SerializeField] private float shieldHalfAngle = 60f;
    [SerializeField] private Color blockFlashColor = new Color(0.5f, 1f, 1f, 1f);
    [SerializeField] private float blockFlashDuration = 0.08f;

    [Header("Dano de contacto")]
    [SerializeField] private int contactDamage = 2;
    [SerializeField] private float damageCooldown = 1f;

    private Rigidbody2D rb;
    private EnemyController controller;
    private EnemyArenaEntry entry;
    private SpriteRenderer spriteRenderer;
    private Transform visual;
    private float facingAngle;      // grados, 0 = derecha (el sprite mira a la derecha)
    private float lastDamageTime;
    private float flashUntil;

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

        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (target) facingAngle = AngleTo(target.position);
        ApplyFacing();
    }

    private bool Busy => controller.IsDead || (entry != null && entry.IsEntering);

    void FixedUpdate()
    {
        if (!target || Busy) return;

        facingAngle = Mathf.MoveTowardsAngle(facingAngle, AngleTo(target.position), turnSpeed * Time.fixedDeltaTime);

        Vector2 toTarget = (Vector2)target.position - rb.position;
        rb.MovePosition(rb.position + toTarget.normalized * speed * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (controller.IsDead) return;
        ApplyFacing();

        if (spriteRenderer != null)
            spriteRenderer.color = Time.time < flashUntil ? blockFlashColor : Color.white;
    }

    // bulletDirection: hacia donde viaja la bala. Bloquea si viene de dentro del cono frontal.
    public bool BlocksHit(Vector2 bulletDirection)
    {
        if (controller.IsDead) return false;

        Vector2 facing = new Vector2(Mathf.Cos(facingAngle * Mathf.Deg2Rad), Mathf.Sin(facingAngle * Mathf.Deg2Rad));
        bool blocked = Vector2.Angle(facing, -bulletDirection) <= shieldHalfAngle;
        if (blocked) flashUntil = Time.time + blockFlashDuration;
        return blocked;
    }

    private void ApplyFacing()
    {
        visual.localRotation = Quaternion.Euler(0f, 0f, facingAngle);
        if (spriteRenderer != null) spriteRenderer.flipY = Mathf.Abs(Mathf.DeltaAngle(0f, facingAngle)) > 90f;
    }

    private float AngleTo(Vector3 worldPos)
    {
        Vector2 d = (Vector2)worldPos - (Vector2)transform.position;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
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
