using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 18f;
    public float lifeTime = 2f;
    public int damage = 1;

    [Header("Behaviour")]
    public bool destroyOnHit = true;

    [Header("Visual")]
    [SerializeField] private float rotationOffset = 0f;

    private Vector2 dir = Vector2.right;
    private Collider2D myCol;

    void Awake()
    {
        myCol = GetComponent<Collider2D>();
    }

    public void Init(Vector2 direction)
    {
        dir = direction.normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);

        Destroy(gameObject, lifeTime);
    }

    // Llamar justo al instanciar para evitar autocolisión
    public void IgnoreCollider(Collider2D other)
    {
        if (myCol && other)
            Physics2D.IgnoreCollision(myCol, other, true);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora al Player
        if (other.CompareTag("Player")) return;

        // Si golpea enemigo
        if (other.CompareTag("Enemy"))
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        if (destroyOnHit)
            Destroy(gameObject);
    }
}