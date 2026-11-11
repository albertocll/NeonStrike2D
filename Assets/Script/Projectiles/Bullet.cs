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
    private bool visualOnly;

    public Vector2 Direction => dir;

    void Awake()
    {
        myCol = GetComponent<Collider2D>();
    }

    // Bala que solo se ve (cooperativo, #206): copia en el invitado de una bala enemiga del anfitrion, o
    // disparo de la nave del otro jugador. Se rompe al chocar pero no hace dano: el dano real se cuenta
    // en otro sitio (el anfitrion, o el dueño de la nave).
    public void MakeVisualOnly() => visualOnly = true;

    void Start()
    {
        // Anfitrion en cooperativo: el invitado tiene que ver las balas de los enemigos.
        if (owner == BulletOwner.Enemy && !visualOnly)
            CoopEnemySync.Instance?.ReportEnemyBullet(this);
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
        if (myCol != null && other != null)
            Physics2D.IgnoreCollision(myCol, other, true);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.name == "SpawnArea") return;

        bool isWall = other.name.StartsWith("Wall_");

        if (owner == BulletOwner.Player)
        {
            if (other.CompareTag("Enemy"))
            {
                // Disparo de la nave del otro jugador: solo se ve, su dano lo cuenta su dueño.
                if (visualOnly)
                {
                    if (destroyOnHit)
                        Destroy(gameObject);
                    return;
                }

                // Invitado en cooperativo: el enemigo es una copia; el impacto se manda al anfitrion,
                // que decide el dano (escudo de Bastion incluido).
                var net = other.GetComponentInParent<NetEnemy>();
                if (net != null && net.IsPuppet)
                {
                    if (!net.IsDead)
                    {
                        net.SendHit(damage, dir);
                        if (destroyOnHit)
                            Destroy(gameObject);
                    }
                    return;
                }

                // Escudo frontal de Bastion: la bala se pierde sin hacer dano.
                var bastion = other.GetComponentInParent<BastionAI>();
                if (bastion != null && bastion.BlocksHit(dir))
                {
                    Destroy(gameObject);
                    return;
                }

                IDamageable enemy = other.GetComponentInParent<IDamageable>();

                if (enemy != null)
                {
                    enemy.TakeDamage(damage);

                    if (destroyOnHit)
                        Destroy(gameObject);
                }

                return;
            }

            if (isWall)
            {
                if (destroyOnHit)
                    Destroy(gameObject);
            }

            return; // ignora Player, PowerUp, Collectible o cualquier otra cosa que no sea Enemy/pared
        }
        else if (owner == BulletOwner.Enemy)
        {
            if (PlayerTargets.IsPlayer(other))
            {
                PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

                if (playerHealth != null && !visualOnly)
                    playerHealth.TakeDamage(damage);

                if (destroyOnHit)
                    Destroy(gameObject);

                return;
            }

            if (isWall)
            {
                if (destroyOnHit)
                    Destroy(gameObject);
            }

            return; // ignora Enemy, PowerUp, Collectible o cualquier otra cosa que no sea Player/pared
        }
    }
}