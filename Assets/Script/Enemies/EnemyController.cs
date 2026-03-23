using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 3;
    [SerializeField] private int currentHealth;

    private Animator animator;
    [SerializeField] private bool isDead;

    [SerializeField] private float deathDestroyDelay = 0.8f;

    void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("[EnemyController] Animator no encontrado en el enemigo.");
    }
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        Debug.Log($"[Enemy] {gameObject.name} vida: {currentHealth}");

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

        Debug.Log($"[EnemyController] Die() llamado en {gameObject.name}");

        EnemyWaveMember waveMember = GetComponent<EnemyWaveMember>();
        if (waveMember != null)
        {
            waveMember.NotifyDeath();
        }

        var ai = GetComponent<WardenAI>();
        if (ai) ai.enabled = false;

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
            animator.SetBool("Shoot", false);
            animator.SetTrigger("Dead");
        }

        StartCoroutine(DestroyAfterDeath());
    }

    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(deathDestroyDelay);
        Destroy(gameObject);
    }


}