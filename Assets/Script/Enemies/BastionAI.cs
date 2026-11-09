using UnityEngine;

// Bastion (issue #192): avanza despacio hacia el jugador con el escudo delante. Las balas que
// le llegan de frente (dentro de shieldHalfAngle) se bloquean; por los lados y la espalda
// recibe dano normal. Gira con una velocidad limitada para que se le pueda flanquear.
// Cada cierto tiempo el escudo se le cae unos segundos (tenido en rojo) y se le puede danar
// de frente; antes de caer parpadea para avisar.
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

    [Header("Caida del escudo")]
    [Tooltip("Segundos con el escudo levantado (incluye el aviso).")]
    [SerializeField] private float shieldUpDuration = 6f;
    [Tooltip("Parpadeo de aviso al final del tiempo con escudo.")]
    [SerializeField] private float shieldWarningDuration = 0.8f;
    [Tooltip("Segundos sin escudo: vulnerable tambien de frente.")]
    [SerializeField] private float shieldDownDuration = 2.5f;
    [SerializeField] private Color shieldWarningColor = new Color(1f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color shieldDownColor = new Color(1f, 0.55f, 0.55f, 1f);

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
    private float cycleStart = -1f; // empieza al terminar de entrar en la arena

    public bool ShieldUp => cycleStart < 0f || CycleTime < shieldUpDuration;
    private float CycleTime => Mathf.Repeat(Time.time - cycleStart, shieldUpDuration + shieldDownDuration);

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

        if (cycleStart < 0f && !Busy) cycleStart = Time.time;
        if (spriteRenderer != null) spriteRenderer.color = CurrentColor();
    }

    private Color CurrentColor()
    {
        if (Time.time < flashUntil) return blockFlashColor;
        if (cycleStart < 0f) return Color.white;
        if (!ShieldUp) return shieldDownColor;

        float t = CycleTime;
        bool warning = t >= shieldUpDuration - shieldWarningDuration;
        return warning && Mathf.Repeat(t, 0.16f) < 0.08f ? shieldWarningColor : Color.white;
    }

    // bulletDirection: hacia donde viaja la bala. Bloquea si viene de dentro del cono frontal.
    public bool BlocksHit(Vector2 bulletDirection)
    {
        if (controller.IsDead || !ShieldUp) return false;

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
