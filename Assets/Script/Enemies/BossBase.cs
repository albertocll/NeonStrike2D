using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Base comun de los jefes nuevos (DataCrawler, Bulwark, Hivemind, Specter...): vida, entrada
// hasta el centro de la arena, invulnerable hasta que empieza el combate, dano de contacto,
// recompensa y muerte. Cada jefe solo implementa Fight() con su patron de ataque.
// Los jefes tienen un sprite por estado y no usan Animator: se cambia el sprite del
// SpriteRenderer (SetPose) y el golpe muestra hitSprite un instante.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class BossBase : MonoBehaviour, IDamageable, IBoss
{
    [Header("Target")]
    [SerializeField] protected Transform target;

    [Header("Identidad")]
    [SerializeField] private string displayName = "Boss";

    [Header("Vida")]
    [SerializeField] protected int maxHealth = 3000;
    [SerializeField] private int scoreValue = 500;

    [Header("Entrada")]
    [SerializeField] private float entrySpeed = 2.5f;

    [Header("Sprites")]
    [SerializeField] protected Sprite idleSprite;
    [SerializeField] protected Sprite moveSprite;
    [SerializeField] protected Sprite hitSprite;
    [SerializeField] protected Sprite dieSprite;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [Tooltip("Voltea el sprite para que mire al jugador.")]
    [SerializeField] private bool faceTarget = true;
    [Tooltip("Marcar si el arte original mira a la izquierda (p. ej. Bulwark).")]
    [SerializeField] private bool artFacesLeft;

    [Header("Dano de contacto")]
    [SerializeField] private int contactDamage = 2;
    [SerializeField] private float contactDamageCooldown = 1f;

    [Header("Arena")]
    [Tooltip("Separacion minima con el borde de la arena al moverse.")]
    [SerializeField] protected float arenaMargin = 0.8f;

    [Header("Recompensa al morir")]
    [SerializeField] private List<GameObject> guaranteedDropPrefabs = new List<GameObject>();
    [SerializeField] private float deathDestroyDelay = 1.2f;

    [Header("Efecto de muerte")]
    [SerializeField] private GameObject deathVfxPrefab;
    [Tooltip("Color de las particulas (el de su sprite).")]
    [SerializeField] private Color deathVfxColor = Color.white;
    [Tooltip("Mas grande que el de un enemigo normal.")]
    [SerializeField] private float deathVfxScale = 3f;

    public event Action<float> OnHealthChanged;
    public event Action OnDefeated;
    public event Action OnCombatStarted;

    public string DisplayName => displayName;
    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    // Invulnerable y sin dano de contacto durante la entrada; los jefes lo usan tambien
    // en sus ventanas propias (escudo, teletransporte...).
    public bool IsInvulnerable { get; protected set; }
    protected float HealthFraction => (float)CurrentHealth / maxHealth;

    protected Rigidbody2D rb;
    protected Collider2D myCol;
    protected SpriteRenderer spriteRenderer;
    protected Bounds? arena;

    private Sprite poseSprite;
    private float hitUntil;
    private float lastContactDamageTime;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        poseSprite = idleSprite;

        CurrentHealth = maxHealth;
        IsInvulnerable = true;
    }

    protected virtual void Start()
    {
        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        arena = FindArena();

        SFXManager.Instance?.PlayBossAppear();
        BossEvents.RaiseSpawned(this);

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return Entry();
        if (IsDead) yield break;

        IsInvulnerable = false;
        OnCombatStarted?.Invoke();
        yield return Fight();
    }

    protected abstract IEnumerator Fight();

    // EnemySpawner lo crea fuera de la arena; entra despacio hasta el centro (su collider es
    // trigger y atraviesa el muro).
    private IEnumerator Entry()
    {
        // EnemySpawner lo instancia en el centro y despues mueve el transform fuera: el
        // Rigidbody aun tiene la posicion del centro hasta la siguiente sincronizacion.
        rb.position = transform.position;
        Vector2 destination = arena.HasValue ? (Vector2)arena.Value.center : rb.position;
        SetPose(moveSprite);
        while (!IsDead && rb.position != destination)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, destination, entrySpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
        SetPose(idleSprite);
    }

    protected virtual void Update()
    {
        if (IsDead || spriteRenderer == null) return;

        Sprite s = Time.time < hitUntil && hitSprite != null ? hitSprite : poseSprite;
        if (s != null) spriteRenderer.sprite = s;

        if (faceTarget && target != null)
            spriteRenderer.flipX = (target.position.x < transform.position.x) != artFacesLeft;
    }

    protected void SetPose(Sprite sprite) => poseSprite = sprite != null ? sprite : idleSprite;

    public void TakeDamage(int damage)
    {
        if (IsDead || IsInvulnerable) return;

        damage = FilterDamage(damage);
        if (damage <= 0) return;

        SFXManager.Instance?.PlayEnemyHit();

        CurrentHealth -= damage;
        OnHealthChanged?.Invoke((float)Mathf.Max(CurrentHealth, 0) / maxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        hitUntil = Time.time + hitFlashDuration;
        OnDamaged();
    }

    // Para escudos: devuelve el dano que llega a la vida (0 = absorbido).
    protected virtual int FilterDamage(int damage) => damage;

    protected virtual void OnDamaged() { }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        StopAllCoroutines();

        SFXManager.Instance?.PlayEnemyDeath();
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(scoreValue);

        foreach (var prefab in guaranteedDropPrefabs)
        {
            if (prefab == null) continue;
            Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.8f);
            Instantiate(prefab, transform.position + offset, Quaternion.identity);
        }

        GetComponent<EnemyWaveMember>()?.NotifyDeath();
        OnDefeated?.Invoke();

        rb.linearVelocity = Vector2.zero;
        if (myCol != null) myCol.enabled = false;

        SpawnDeathVfx();
        StartCoroutine(DeathFade());
    }

    private void SpawnDeathVfx()
    {
        if (deathVfxPrefab == null) return;

        GameObject vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
        vfx.transform.localScale *= deathVfxScale;
        var ps = vfx.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        main.startColor = deathVfxColor;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
    }

    // Sin animacion de muerte: sprite de muerte (si hay) que parpadea y se desvanece.
    private IEnumerator DeathFade()
    {
        if (spriteRenderer != null && dieSprite != null) spriteRenderer.sprite = dieSprite;

        for (float t = 0f; t < deathDestroyDelay; t += Time.deltaTime)
        {
            if (spriteRenderer != null)
            {
                float k = t / deathDestroyDelay;
                bool flash = Mathf.Repeat(t, 0.16f) < 0.08f;
                spriteRenderer.color = new Color(1f, flash ? 0.6f : 1f, flash ? 0.6f : 1f, 1f - k);
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---- Ayudas para los patrones de ataque ----

    protected Vector2 DirectionToTarget(Vector2 from) =>
        target != null ? ((Vector2)target.position - from).normalized : Vector2.left;

    protected Vector2 ClampToArena(Vector2 p)
    {
        if (!arena.HasValue) return p;
        Vector2 half = (myCol != null ? (Vector2)myCol.bounds.extents : Vector2.zero) + Vector2.one * arenaMargin;
        Vector2 min = (Vector2)arena.Value.min + half;
        Vector2 max = (Vector2)arena.Value.max - half;
        return new Vector2(
            min.x <= max.x ? Mathf.Clamp(p.x, min.x, max.x) : arena.Value.center.x,
            min.y <= max.y ? Mathf.Clamp(p.y, min.y, max.y) : arena.Value.center.y);
    }

    protected Vector2 RandomArenaPoint()
    {
        if (!arena.HasValue) return rb.position;
        Bounds b = arena.Value;
        return ClampToArena(new Vector2(UnityEngine.Random.Range(b.min.x, b.max.x), UnityEngine.Random.Range(b.min.y, b.max.y)));
    }

    protected void Shoot(Bullet prefab, Vector2 direction, int damage)
    {
        if (prefab == null) return;
        Bullet b = Instantiate(prefab, transform.position, Quaternion.identity);
        b.damage = damage;
        b.Init(direction);
        if (myCol != null) b.IgnoreCollider(myCol);
    }

    // Abanico de 'count' balas repartidas en 'spreadDegrees' alrededor de 'center'.
    protected void ShootFan(Bullet prefab, Vector2 center, int count, float spreadDegrees, int damage)
    {
        float baseAngle = Mathf.Atan2(center.y, center.x) * Mathf.Rad2Deg;
        for (int i = 0; i < count; i++)
        {
            float k = count == 1 ? 0.5f : (float)i / (count - 1);
            float a = (baseAngle - spreadDegrees * 0.5f + spreadDegrees * k) * Mathf.Deg2Rad;
            Shoot(prefab, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), damage);
        }
    }

    // Invoca un enemigo que cuenta en la oleada del jefe (la oleada no acaba mientras viva).
    protected GameObject Summon(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return null;
        GameObject add = Instantiate(prefab, ClampToArena(position), Quaternion.identity);
        add.GetComponent<EnemyWaveMember>()?.Initialize(GetComponent<EnemyWaveMember>()?.Manager);
        return add;
    }

    protected static int CountAlive(List<GameObject> list)
    {
        list.RemoveAll(g => g == null || g.GetComponent<EnemyController>()?.IsDead == true);
        return list.Count;
    }

    // Parpadeo de aviso antes de un ataque.
    protected IEnumerator Blink(Color color, float duration, float interval = 0.1f)
    {
        for (float t = 0f; t < duration && !IsDead; t += Time.deltaTime)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Mathf.Repeat(t, interval * 2f) < interval ? color : Color.white;
            yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }

    private static Bounds? FindArena()
    {
        foreach (var box in FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            if (box.name == "SpawnArea") return box.bounds;
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDamagePlayer(other);
    private void OnTriggerStay2D(Collider2D other) => TryDamagePlayer(other);

    private void TryDamagePlayer(Collider2D other)
    {
        if (IsInvulnerable || IsDead || !other.CompareTag("Player")) return;
        if (Time.time - lastContactDamageTime < contactDamageCooldown) return;
        lastContactDamageTime = Time.time;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && !health.IsDead)
            health.TakeDamage(contactDamage);
    }
}
