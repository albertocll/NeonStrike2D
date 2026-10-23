using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class FuseAI : MonoBehaviour
{
    [Header("Detonation")]
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private int explosionDamage = 2;

    private EnemyController controller;
    private bool hasDetonated;

    void Awake()
    {
        controller = GetComponent<EnemyController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDetonated) return;
        if (!other.CompareTag("Player")) return;

        controller.TakeDamage(controller.maxHealth);
    }

    // Llamado por EnemyController.Die(): se dispara tanto si Fuse muere por
    // contacto del jugador como si muere por un disparo, para que ambas
    // formas de detonación causen el mismo daño de área.
    public void Detonate()
    {
        if (hasDetonated) return;
        hasDetonated = true;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null && !health.IsDead)
                health.TakeDamage(explosionDamage);
        }
    }
}
