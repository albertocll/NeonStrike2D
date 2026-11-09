using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour, IDamageable
{
    public int maxHealth = 3;
    [SerializeField] private int currentHealth;
    [SerializeField] private int scoreValue = 50;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private bool isDead;
    public bool IsDead => isDead;

    [SerializeField] private float deathDestroyDelay = 0.8f;

    [Header("Power-Up Drop")]
    [SerializeField] private List<GameObject> powerUpPrefabs;
    [SerializeField, Range(0f, 1f)] private float powerUpDropChance = 0.05f;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfxPrefab;
    [Tooltip("Color de las particulas de muerte (el de su sprite). Con alfa 0 se usa el de siempre segun el tipo.")]
    [SerializeField] private Color deathVfxColor = Color.clear;
    private static readonly Color WardenDeathVfxColor = new Color32(0x80, 0x1D, 0x43, 0xFF);
    private static readonly Color StrikerDeathVfxColor = new Color(0f, 0.95f, 1f);
    private static readonly Color FuseDeathVfxColor = new Color(1f, 0.5f, 0.1f);

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Enemigos mas fuertes en cada nivel (LevelData.enemyHealthMultiplier). Se llama justo
    // despues de instanciarlo; las copias (racimo de Swarm) heredan la vida ya escalada.
    public void ScaleHealth(float multiplier)
    {
        if (multiplier <= 0f || Mathf.Approximately(multiplier, 1f)) return;
        // El -0.001 evita que el error de coma flotante redondee 50 x 1.2 a 61.
        maxHealth = Mathf.Max(1, Mathf.CeilToInt(maxHealth * multiplier - 0.001f));
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        SFXManager.Instance?.PlayEnemyHit();

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        SFXManager.Instance?.PlayEnemyDeath();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(scoreValue);

        TryDropPowerUp();

        EnemyWaveMember waveMember = GetComponent<EnemyWaveMember>();
        if (waveMember != null)
        {
            waveMember.NotifyDeath();
        }

        var wardenAI = GetComponent<WardenAI>();
        if (wardenAI) wardenAI.enabled = false;

        var strikerAI = GetComponent<StrikerAI>();
        if (strikerAI) strikerAI.enabled = false;

        var fuseAI = GetComponent<FuseAI>();
        if (fuseAI) fuseAI.Detonate();

        SpawnDeathVfx(wardenAI != null, strikerAI != null, fuseAI != null);

        if (spriteRenderer != null) spriteRenderer.enabled = false;

        var movement = GetComponent<EnemyMovement>();
        if (movement) movement.enabled = false;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.SetBool("Moving", false);

            foreach (var param in animator.parameters)
            {
                if (param.name == "Shoot" && param.type == AnimatorControllerParameterType.Bool)
                    animator.SetBool("Shoot", false);
            }

            animator.SetTrigger("Dead");
        }

        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(deathDestroyDelay);
        Destroy(gameObject);
    }

    private void SpawnDeathVfx(bool isWarden, bool isStriker, bool isFuse)
    {
        if (deathVfxPrefab == null) return;

        GameObject vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        if (deathVfxColor.a > 0f) main.startColor = deathVfxColor;
        else if (isWarden) main.startColor = WardenDeathVfxColor;
        else if (isStriker) main.startColor = StrikerDeathVfxColor;
        else if (isFuse) main.startColor = FuseDeathVfxColor;
    }

    private void TryDropPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0) return;
        if (Random.value > powerUpDropChance) return;

        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}