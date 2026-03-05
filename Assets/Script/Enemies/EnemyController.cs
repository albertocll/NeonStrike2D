using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    private Animator animator;
    private bool isDead;

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

        if (animator != null)
            animator.SetTrigger("Hit");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // Desactivar comportamiento del enemigo
        var ai = GetComponent<WardenAI>();
        if (ai) ai.enabled = false;

        var movement = GetComponent<EnemyMovement>();
        if (movement) movement.enabled = false;

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
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }
}