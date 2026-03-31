using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    public enum BulletOwner
    {
        Player,
        Enemy
    }

    [Header("Stats")]
    public float speed = 18f;
    public float lifeTime = 5f;
    public int damage = 1;

    [Header("Behaviour")]
    public bool destroyOnHit = true;
    [SerializeField] private BulletOwner owner = BulletOwner.Player;

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

    public void IgnoreCollider(Collider2D other)
    {
        //if (myCol != null && other != null)
            //Physics2D.IgnoreCollision(myCol, other, true);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.name == "SpawnArea") return;

        if (owner == BulletOwner.Player)
        {
            if (other.CompareTag("Player")) return;

            if (other.CompareTag("Enemy"))
            {
                EnemyController enemy = other.GetComponentInParent<EnemyController>();

                if (enemy != null)
                {
                    enemy.TakeDamage(damage);

                    if (destroyOnHit)
                        Destroy(gameObject);

                    return;
                }
                else
                {
                    Debug.Log($"[Bullet] NO se encontró EnemyController en: {other.name}");
                }
            }
        }
        else if (owner == BulletOwner.Enemy)
        {
            if (other.CompareTag("Enemy")) return;

            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }

                if (destroyOnHit)
                    Destroy(gameObject);

                return;
            }
        }

        if (destroyOnHit)
            Destroy(gameObject);
    }
}