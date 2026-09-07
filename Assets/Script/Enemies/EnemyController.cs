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

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

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

    private void TryDropPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0) return;
        if (Random.value > powerUpDropChance) return;

        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
        Instantiate(prefab, transform.position, Quaternion.identity);
    }
}