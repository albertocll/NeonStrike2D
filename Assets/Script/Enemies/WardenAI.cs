using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WardenAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // si está vacío lo busca por tag "Player"

    [Header("Movement")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float stopDistance = 2.0f;

    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float fireRate = 1.5f;   // balas por segundo
    [SerializeField] private float shootRange = 8f;

    [Header("Animator (optional)")]
    [SerializeField] private Animator anim;
    [SerializeField] private string movingParam = "Moving";
    [SerializeField] private string shootParam = "Shoot";

    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextShotTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        
        if (!anim)
            Debug.LogError("WardenAI: No Animator found on Warden prefab or children.", this);
    }

    void Start()
    {
        if (!target)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }

        if (!firePoint) firePoint = transform; // fallback
    }

    void FixedUpdate()
    {
        if (!target) return;

        Vector2 toTarget = (Vector2)target.position - rb.position;
        float dist = toTarget.magnitude;

        // Movimiento
        Vector2 vel = Vector2.zero;
        bool moving = false;

        if (dist > stopDistance)
        {
            moving = true;
            vel = toTarget.normalized * speed;
        }

        rb.linearVelocity = vel;

        if (anim) anim.SetBool(movingParam, moving);
    }

    void Update()
    {
        if (!target || !bulletPrefab) return;

        float dist = Vector2.Distance(transform.position, target.position);
        bool canShoot = dist <= shootRange;

        if (anim) anim.SetBool(shootParam, canShoot);

        if (!canShoot) return;

        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));

        Vector2 dir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;

        Bullet b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        b.Init(dir);

        // Evitar autocolisión con el enemigo
        if (myCol) b.IgnoreCollider(myCol);
    }
}
