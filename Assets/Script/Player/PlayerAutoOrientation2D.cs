using UnityEngine;

// Dos cosas de la nave:
// - Orientacion: la nave mira hacia donde se mueve (la ultima direccion del joystick o el teclado;
//   al soltar, conserva la ultima). En cooperativo, la del otro jugador llega por la red (PlayerSync).
//   El dibujo es de perfil mirando a la derecha: se gira hacia la direccion y, en la mitad izquierda,
//   se refleja en vertical para que no vaya boca abajo.
// - Auto apuntado: el enemigo vivo mas cercano dentro de la arena y sin muro en medio. Las balas
//   salen hacia el aunque la nave mire a otro lado (WeaponController). Sin objetivo, no se dispara.
public class PlayerAutoOrientation2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float searchInterval = 0.1f;
    [Tooltip("Capas que tapan la linea de vision (los Wall_* estan en Default). Los triggers no cuentan.")]
    [SerializeField] private LayerMask obstacleLayers = 1;

    [Header("Orientacion")]
    [Tooltip("Grados por segundo que gira la nave hacia la direccion del joystick. Alto: casi inmediato, pero sin saltos.")]
    [SerializeField] private float turnSpeed = 1440f;
    [Tooltip("Hijo que no gira con la nave (el nombre del jugador).")]
    [SerializeField] private string uprightChildName = "NameTagCanvas";

    private Transform currentTarget;
    private float searchTimer;
    private Vector2 lastAimDirection = Vector2.right;

    private PlayerController controller;
    private Rigidbody2D rb;
    private Transform uprightChild;
    private Rect? arenaInner;
    private bool arenaSearched;
    private Vector2 remoteFacing = Vector2.right;
    private float currentAngle;
    private bool mirrored;

    private static readonly Collider2D[] OverlapBuffer = new Collider2D[64];
    private static readonly RaycastHit2D[] LineBuffer = new RaycastHit2D[16];

    public Vector2 CurrentAimDirection => lastAimDirection;
    public Transform CurrentTarget => currentTarget;
    // Hacia donde mira la nave (lo que se manda al otro jugador en cooperativo).
    public Vector2 FacingDirection => IsLocal ? controller.FacingDirection : remoteFacing;

    private bool IsLocal => controller != null && controller.enabled;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        // El prefab tenia la rotacion bloqueada; ahora la nave gira (cinematica: nada la empuja a girar).
        if (rb != null) rb.freezeRotation = false;
        uprightChild = transform.Find(uprightChildName);
    }

    private void Start()
    {
        if (spriteRenderer != null) spriteRenderer.flipX = false;
        currentAngle = transform.eulerAngles.z;
    }

    private void Update()
    {
        searchTimer -= Time.deltaTime;

        // El objetivo puede morir o salir de la vista entre busquedas: se comprueba cada fotograma.
        if (currentTarget != null && !IsValidTarget(currentTarget, transform.position))
            currentTarget = null;

        if (searchTimer <= 0f || currentTarget == null)
        {
            searchTimer = searchInterval;
            currentTarget = FindClosestTarget();
        }

        if (currentTarget != null)
        {
            Vector2 direction = currentTarget.position - transform.position;
            if (direction.sqrMagnitude > 0.001f)
                lastAimDirection = direction.normalized;
        }

        // La nave del otro jugador no tiene fisica propia: se gira aqui. La local, en FixedUpdate.
        if (!IsLocal) ApplyFacing(Time.deltaTime, false);
    }

    private void FixedUpdate()
    {
        if (IsLocal) ApplyFacing(Time.fixedDeltaTime, true);
    }

    private void LateUpdate()
    {
        // El nombre del jugador se lee siempre derecho.
        if (uprightChild != null)
        {
            uprightChild.rotation = Quaternion.identity;
            Vector3 s = uprightChild.localScale;
            s.y = Mathf.Abs(s.y) * Mathf.Sign(transform.localScale.y);
            uprightChild.localScale = s;
        }
    }

    // En cooperativo: la direccion que manda el otro jugador.
    public void SetRemoteFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.0001f) remoteFacing = direction.normalized;
    }

    private void ApplyFacing(float deltaTime, bool usePhysics)
    {
        Vector2 facing = FacingDirection;
        if (facing.sqrMagnitude < 0.0001f) return;

        float targetAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        currentAngle = turnSpeed > 0f
            ? Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * deltaTime)
            : targetAngle;

        if (usePhysics && rb != null) rb.MoveRotation(currentAngle);
        else transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

        // Mitad izquierda: reflejada en vertical (con un margen para que no parpadee al ir en vertical).
        float cos = Mathf.Cos(currentAngle * Mathf.Deg2Rad);
        if (cos < -0.1f) mirrored = true;
        else if (cos > 0.1f) mirrored = false;

        Vector3 scale = transform.localScale;
        float wantedY = Mathf.Abs(scale.y) * (mirrored ? -1f : 1f);
        if (!Mathf.Approximately(scale.y, wantedY))
        {
            scale.y = wantedY;
            transform.localScale = scale;
        }
    }

    private Transform FindClosestTarget()
    {
        Vector2 playerPosition = transform.position;
        int count = Physics2D.OverlapCircle(playerPosition, detectionRadius,
            new ContactFilter2D { useLayerMask = true, layerMask = enemyLayer, useTriggers = true }, OverlapBuffer);

        Transform closest = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = OverlapBuffer[i];
            if (hit == null) continue;

            Transform candidate = TargetRoot(hit);
            float sqrDistance = ((Vector2)candidate.position - playerPosition).sqrMagnitude;
            if (sqrDistance >= closestSqrDistance) continue;
            if (!IsValidTarget(candidate, playerPosition)) continue;

            closestSqrDistance = sqrDistance;
            closest = candidate;
        }

        return closest;
    }

    // El enemigo al que pertenece el collider (puede estar en un hijo).
    private static Transform TargetRoot(Collider2D hit)
    {
        if (hit.attachedRigidbody != null) return hit.attachedRigidbody.transform;
        var enemy = hit.GetComponentInParent<EnemyController>();
        if (enemy != null) return enemy.transform;
        var boss = hit.GetComponentInParent<IRemoteBoss>() as Component;
        return boss != null ? boss.transform : hit.transform;
    }

    private bool IsValidTarget(Transform target, Vector2 from)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        if (((Vector2)target.position - from).sqrMagnitude > detectionRadius * detectionRadius) return false;
        if (IsDead(target)) return false;
        if (!IsInsideArena(target.position)) return false;
        return HasLineOfSight(from, target);
    }

    public static bool IsDead(Transform target)
    {
        var enemy = target.GetComponentInParent<EnemyController>();
        if (enemy != null) return enemy.IsDead;
        var boss = target.GetComponentInParent<IRemoteBoss>();
        return boss != null && boss.IsDead;
    }

    private bool IsInsideArena(Vector2 position)
    {
        if (!arenaSearched)
        {
            arenaSearched = true;
            arenaInner = PlayerController.FindArenaInner(gameObject.scene);
        }
        return !arenaInner.HasValue || arenaInner.Value.Contains(position);
    }

    // Sin un collider solido (muro u obstaculo) entre la nave y el objetivo. Se ignoran los triggers,
    // la propia nave y los jugadores.
    private bool HasLineOfSight(Vector2 from, Transform target)
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = obstacleLayers, useTriggers = false };
        int count = Physics2D.Linecast(from, target.position, filter, LineBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = LineBuffer[i].collider;
            if (col == null) continue;
            if (col.transform.IsChildOf(transform) || col.transform.IsChildOf(target)) continue;
            if (col.GetComponentInParent<PlayerController>() != null) continue;
            return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
