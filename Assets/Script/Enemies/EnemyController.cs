using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 3;
    [SerializeField] private int currentHealth;
    [SerializeField] private int scoreValue = 50;

    private Animator animator;
    [SerializeField] private bool isDead;

    [SerializeField] private float deathDestroyDelay = 0.8f;

    [Header("Power-Up Drop")]
    [SerializeField] private List<GameObject> powerUpPrefabs;
    [SerializeField, Range(0f, 1f)] private float powerUpDropChance = 0.05f;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfxPrefab;
    private static readonly Color WardenDeathVfxColor = new Color(1f, 0.2f, 0.85f);
    private static readonly Color StrikerDeathVfxColor = new Color(0f, 0.95f, 1f);

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
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

        SpawnDeathVfx(wardenAI != null, strikerAI != null);

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

    private void SpawnDeathVfx(bool isWarden, bool isStriker)
    {
        if (deathVfxPrefab == null) return;

        GameObject vfx = Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps == null) return;

        var main = ps.main;
        if (isWarden) main.startColor = WardenDeathVfxColor;
        else if (isStriker) main.startColor = StrikerDeathVfxColor;
    }

    private void TryDropPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0) return;
        if (Random.value > powerUpDropChance) return;

        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}