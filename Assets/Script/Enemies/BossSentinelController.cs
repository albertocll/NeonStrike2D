using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossSentinelController : MonoBehaviour, IDamageable
{
    public static event Action<BossSentinelController> OnAnyBossSpawned;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Identidad")]
    [SerializeField] private string displayName = "Sentinel";

    [Header("Vida")]
    [SerializeField] private int maxHealth = 750; // ~10x EnemyWarden (enemigo basico, 75 HP)
    [SerializeField] private int scoreValue = 500;
    [SerializeField, Range(0f, 1f)] private float phase2HealthThreshold = 0.5f;

    [Header("Embestida")]
    [SerializeField] private float idleDuration = 1.5f;
    [SerializeField] private float telegraphDuration = 0.6f;
    [SerializeField] private float telegraphBlinkInterval = 0.1f;
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float chargeDuration = 0.8f;

    [Header("Rafaga circular (fase 2, vida <50%)")]
    [SerializeField] private Bullet burstBulletPrefab;
    [SerializeField] private int burstBulletCount = 12;
    [SerializeField] private float burstAnimDuration = 0.3f;

    [Header("Dano de contacto")]
    [SerializeField] private int contactDamage = 2;
    [SerializeField] private float contactDamageCooldown = 1f;

    [Header("Recompensa al morir")]
    [SerializeField] private List<GameObject> guaranteedDropPrefabs;
    [SerializeField] private float deathDestroyDelay = 1f;

    [Header("Animator")]
    [SerializeField] private Animator anim;
    [SerializeField] private string movingParam = "Moving";

    public event Action<float> OnHealthChanged;
    public event Action OnDefeated;

    public string DisplayName => displayName;
    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    private bool IsPhase2 => CurrentHealth <= maxHealth * phase2HealthThreshold;

    private Rigidbody2D rb;
    private Collider2D myCol;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float lastContactDamageTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!anim) anim = GetComponentInChildren<Animator>(true);
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) baseColor = spriteRenderer.color;

        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        SFXManager.Instance?.PlayBossAppear();
        OnAnyBossSpawned?.Invoke(this);

        StartCoroutine(AttackLoop());
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        SFXManager.Instance?.PlayEnemyHit();

        CurrentHealth -= damage;
        OnHealthChanged?.Invoke((float)Mathf.Max(CurrentHealth, 0) / maxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
            return;
        }

        if (anim != null) anim.SetTrigger("Hit");
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        StopAllCoroutines();

        SFXManager.Instance?.PlayEnemyDeath();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(scoreValue);

        DropRewards();

        GetComponent<EnemyWaveMember>()?.NotifyDeath();
        OnDefeated?.Invoke();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (myCol != null) myCol.enabled = false;

        if (anim != null)
        {
            anim.SetBool(movingParam, false);
            anim.ResetTrigger("Hit");
            anim.SetTrigger("Dead");
        }

        StartCoroutine(DestroyAfterDeath());
    }

    private void DropRewards()
    {
        foreach (var prefab in guaranteedDropPrefabs)
        {
            if (prefab == null) continue;
            Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.5f);
            Instantiate(prefab, transform.position + offset, Quaternion.identity);
        }
    }

    private IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(deathDestroyDelay);
        Destroy(gameObject);
    }

    private IEnumerator AttackLoop()
    {
        while (!IsDead)
        {
            SetMoving(false);
            yield return new WaitForSeconds(idleDuration);
            if (IsDead) yield break;

            yield return Telegraph();
            if (IsDead) yield break;

            Vector2 chargeDir = target != null
                ? ((Vector2)target.position - (Vector2)transform.position).normalized
                : Vector2.right;
            yield return Charge(chargeDir);
            if (IsDead) yield break;

            if (IsPhase2)
                yield return FireCircularBurst();
        }
    }

    private IEnumerator Telegraph()
    {
        float elapsed = 0f;
        bool flashOn = false;

        while (elapsed < telegraphDuration)
        {
            if (spriteRenderer != null)
            {
                flashOn = !flashOn;
                spriteRenderer.color = flashOn ? Color.white : baseColor;
            }

            yield return new WaitForSeconds(telegraphBlinkInterval);
            elapsed += telegraphBlinkInterval;
        }

        if (spriteRenderer != null) spriteRenderer.color = baseColor;
    }

    private IEnumerator Charge(Vector2 direction)
    {
        SetMoving(true);
        if (rb != null) rb.linearVelocity = direction * chargeSpeed;

        yield return new WaitForSeconds(chargeDuration);

        if (rb != null) rb.linearVelocity = Vector2.zero;
        SetMoving(false);
    }

    private IEnumerator FireCircularBurst()
    {
        if (burstBulletPrefab != null)
        {
            SetMoving(true);

            for (int i = 0; i < burstBulletCount; i++)
            {
                float angle = i * (360f / burstBulletCount) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                Bullet b = Instantiate(burstBulletPrefab, transform.position, Quaternion.identity);
                b.Init(dir);
                if (myCol != null) b.IgnoreCollider(myCol);
            }
        }

        yield return new WaitForSeconds(burstAnimDuration);
        SetMoving(false);
    }

    private void SetMoving(bool moving)
    {
        if (anim != null) anim.SetBool(movingParam, moving);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (Time.time - lastContactDamageTime < contactDamageCooldown) return;
        lastContactDamageTime = Time.time;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && !health.IsDead)
            health.TakeDamage(contactDamage);
    }
}
