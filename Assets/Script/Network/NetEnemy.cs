using System.Globalization;
using System.Text;
using UnityEngine;

// Enemigo o jefe compartido en cooperativo (issue #206). En el anfitrion es el enemigo real y solo
// escribe su estado; en el invitado es una copia sin IA que sigue ese estado (IsPuppet).
public class NetEnemy : MonoBehaviour
{
    private const float LerpSpeed = 12f;
    private const float MovingThreshold = 0.05f;
    private const string MovingParam = "Moving";

    private const int FlagFlipX = 1;
    private const int FlagVisualMirrored = 2;
    private const int FlagInvulnerable = 4;

    public int Id { get; private set; }
    public bool IsPuppet { get; private set; }
    public string Key { get; private set; }

    private EnemyController controller;
    private IRemoteBoss boss;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool hasMovingParam;

    private Vector2 targetPosition;
    private bool hasState;
    private bool combatStarted;

    public bool IsDead => controller != null ? controller.IsDead : boss != null && boss.IsDead;

    private void Cache()
    {
        controller = GetComponent<EnemyController>();
        boss = GetComponent<IRemoteBoss>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (var p in animator.parameters)
                if (p.name == MovingParam && p.type == AnimatorControllerParameterType.Bool) hasMovingParam = true;
    }

    // ── Anfitrion ────────────────────────────────────────────────────────────

    public void InitHost(int id)
    {
        Id = id;
        Key = PrefabKey(gameObject.name);
        Cache();
    }

    // id,clave,x,y,vida,banderas,color
    public void AppendState(StringBuilder sb)
    {
        Vector2 p = transform.position;
        int health = controller != null ? controller.CurrentHealth : boss != null ? boss.CurrentHealth : 0;
        int flags = 0;
        if (spriteRenderer != null)
        {
            if (spriteRenderer.flipX) flags |= FlagFlipX;
            if (spriteRenderer.transform.localScale.x < 0f) flags |= FlagVisualMirrored;
        }
        if (boss != null && boss.IsInvulnerable) flags |= FlagInvulnerable;
        string color = spriteRenderer != null ? ColorUtility.ToHtmlStringRGBA(spriteRenderer.color) : "FFFFFFFF";

        sb.Append(Id).Append(',').Append(Key).Append(',')
          .Append(F(p.x)).Append(',').Append(F(p.y)).Append(',')
          .Append(health).Append(',').Append(flags).Append(',').Append(color);
    }

    // Impacto de una bala del invitado: mismo filtro que Bullet (escudo frontal de Bastion).
    public void ApplyGuestHit(int damage, Vector2 direction)
    {
        if (IsDead) return;
        var bastion = GetComponent<BastionAI>();
        if (bastion != null && bastion.BlocksHit(direction)) return;
        var target = GetComponent<IDamageable>();
        if (target == null) return;

        // Si este impacto lo mata, los puntos son del invitado, no del anfitrion.
        SetAwardScore(false);
        target.TakeDamage(damage);
        if (IsDead) KilledByGuest = true;
        else SetAwardScore(true);
    }

    public bool KilledByGuest { get; private set; }

    private void SetAwardScore(bool award)
    {
        if (controller != null) controller.AwardScore = award;
        else if (boss != null) boss.AwardScore = award;
    }

    // ── Invitado ─────────────────────────────────────────────────────────────

    // Quita la IA (que haria dano, dispararia o invocaria por su cuenta) y deja lo visual, la vida
    // y el collider para que las balas del invitado le den.
    public void InitPuppet(int id)
    {
        Id = id;
        IsPuppet = true;
        Key = PrefabKey(gameObject.name);
        Cache();

        foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == this || mb is EnemyController || mb is EnemyWaveMember) continue;

            if (mb is IRemoteBoss)
            {
                // Se queda (la barra de vida lo necesita) pero sin ejecutar su Start ni su Update.
                mb.enabled = false;
                continue;
            }

            if (mb.GetType().Name.EndsWith("AI") || mb is EnemySeparation || mb is EnemyArenaEntry
                || mb is EnemyOrientation2D || mb is EnemyMovement)
            {
                mb.enabled = false;
                Destroy(mb);
            }
        }

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        targetPosition = transform.position;
        boss?.BeginRemote();
    }

    public void ApplyRemoteState(Vector2 position, int health, int flags, Color color)
    {
        if (IsDead) return;

        targetPosition = position;
        if (!hasState)
        {
            transform.position = position;
            hasState = true;
        }

        if (controller != null) controller.ApplyRemoteHealth(health);
        if (boss != null)
        {
            boss.ApplyRemoteHealth(health);
            if (!combatStarted && (flags & FlagInvulnerable) == 0)
            {
                combatStarted = true;
                boss.RemoteCombatStart();
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (flags & FlagFlipX) != 0;
            Transform visual = spriteRenderer.transform;
            if (visual != transform)
            {
                Vector3 s = visual.localScale;
                s.x = Mathf.Abs(s.x) * ((flags & FlagVisualMirrored) != 0 ? -1f : 1f);
                visual.localScale = s;
            }
            spriteRenderer.color = color;
        }
    }

    // En el invitado solo suma puntos si lo ha matado el invitado.
    public void RemoteDie(bool killedByGuest)
    {
        if (IsDead) return;
        SetAwardScore(killedByGuest);
        if (controller != null) controller.RemoteDie();
        else boss?.RemoteDie();
    }

    // Bala del invitado contra esta copia: el dano lo decide el anfitrion; aqui solo el golpe visual.
    public void SendHit(int damage, Vector2 direction)
    {
        CoopEnemySync.Instance?.SendHit(Id, damage, direction);
        SFXManager.Instance?.PlayEnemyHit();
        if (controller != null) controller.PlayHitFeedback();
    }

    private void Update()
    {
        if (!IsPuppet || !hasState || IsDead) return;

        Vector2 pos = transform.position;
        transform.position = Vector2.Lerp(pos, targetPosition, Time.deltaTime * LerpSpeed);

        if (hasMovingParam)
            animator.SetBool(MovingParam, Vector2.Distance(pos, targetPosition) > MovingThreshold);
    }

    // "EnemyStriker(Clone)" y las copias de copias (racimo de Swarm) → "EnemyStriker".
    public static string PrefabKey(string objectName) => objectName.Replace("(Clone)", "").Trim();

    public static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
