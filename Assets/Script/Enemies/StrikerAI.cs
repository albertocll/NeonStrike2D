using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StrikerAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Damage")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float damageCooldown = 1f;

    [Header("Animator")]
    [SerializeField] private Animator anim;
    [SerializeField] private string movingParam = "Moving";

    private Rigidbody2D rb;
    private float lastDamageTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!anim) anim = GetComponentInChildren<Animator>(true);
    }

    void Start()
    {
        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
    }

    void FixedUpdate()
    {
        if (!target) return;

        Vector2 toTarget = (Vector2)target.position - rb.position;
        Vector2 vel = toTarget.normalized * speed;
        rb.linearVelocity = vel;

        if (anim) anim.SetBool(movingParam, true);
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
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null && !health.IsDead)
            health.TakeDamage(contactDamage);
    }
}