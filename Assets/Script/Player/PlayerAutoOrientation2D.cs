using UnityEngine;

public class PlayerAutoOrientation2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float searchInterval = 0.1f;

    private Transform currentTarget;
    private float searchTimer;
    private Vector2 lastAimDirection = Vector2.right;

    public Vector2 CurrentAimDirection => lastAimDirection;
    public Transform CurrentTarget => currentTarget;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            searchTimer = searchInterval;
            currentTarget = FindClosestTarget();
        }

        UpdateOrientation();
    }

    private Transform FindClosestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            detectionRadius,
            enemyLayer
        );

        Transform closest = null;
        float closestSqrDistance = float.MaxValue;
        Vector2 playerPosition = transform.position;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            float sqrDistance = ((Vector2)hit.transform.position - playerPosition).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = hit.transform;
            }
        }

        return closest;
    }

    private void UpdateOrientation()
    {
        if (spriteRenderer == null) return;

        if (currentTarget != null)
        {
            Vector2 direction = currentTarget.position - transform.position;

            if (direction.sqrMagnitude > 0.001f)
                lastAimDirection = direction.normalized;
        }

        spriteRenderer.flipX = lastAimDirection.x < 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}