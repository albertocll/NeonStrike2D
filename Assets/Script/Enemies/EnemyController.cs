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

    [Header("Golpe (enemigos sin Animator)")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float hitFlashDuration = 0.1f;
    [Tooltip("Cuanto crece el sprite en el golpe (0.15 = un 15%).")]
    [SerializeField] private float hitPunchScale = 0.15f;
    private Coroutine hitFlashRoutine;
    private Vector3 visualBaseScale;
    private bool visualScaleCaptured;

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!TryGetComponent(out EnemySeparation _)) gameObject.AddComponent<EnemySeparation>();
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

        PlayHitFeedback();
    }

    // Copia en el invitado (cooperativo, #206): la vida y la muerte llegan del anfitrion.
    public int CurrentHealth => currentHealth;
    public void ApplyRemoteHealth(int health) => currentHealth = health;
    public void RemoteDie() => Die();

    // En cooperativo cada jugador suma solo lo que mata el (#206): quien no lo ha matado no suma.
    public bool AwardScore { get; set; } = true;

    // Golpe visual sin tocar la vida (tambien para el impacto que el invitado manda al anfitrion).
    public void PlayHitFeedback()
    {
        if (isDead) return;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Hit");
        }
        else
        {
            // Sin Animator (enemigos de un solo sprite): destello y pequeno golpe de escala.
            if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = StartCoroutine(HitFlash());
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        SFXManager.Instance?.PlayEnemyDeath();

        if (AwardScore && ScoreManager.Instance != null)
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

    // Tine el sprite (respetando su alfa, p. ej. Wisp intangible) y lo agranda un instante.
    // Las IA que fijan el color cada frame (Bastion) se quedan con el golpe de escala.
    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        Transform visual = spriteRenderer.transform;
        if (!visualScaleCaptured)
        {
            // En valor absoluto: EnemyOrientation2D voltea el sprite con el signo de la escala X.
            Vector3 s = visual.localScale;
            visualBaseScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);
            visualScaleCaptured = true;
        }

        for (float t = 0f; t < hitFlashDuration && !isDead; t += Time.deltaTime)
        {
            float k = 1f - t / hitFlashDuration;
            Color c = hitFlashColor;
            c.a = spriteRenderer.color.a;
            spriteRenderer.color = Color.Lerp(new Color(1f, 1f, 1f, c.a), c, k);
            SetVisualScale(visual, 1f + hitPunchScale * k);
            yield return null;
        }

        spriteRenderer.color = new Color(1f, 1f, 1f, spriteRenderer.color.a);
        SetVisualScale(visual, 1f);
        hitFlashRoutine = null;
    }

    private void SetVisualScale(Transform visual, float factor)
    {
        float signX = Mathf.Sign(visual.localScale.x);
        visual.localScale = new Vector3(signX * visualBaseScale.x * factor, visualBaseScale.y * factor, visualBaseScale.z);
    }

    private void TryDropPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0) return;
        if (Random.value > powerUpDropChance) return;

        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}