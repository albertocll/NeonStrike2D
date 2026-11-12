using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float deathFreezeDelay = 0.6f;
    [SerializeField] private GameOverUI gameOverUI;

    private int currentHealth;
    private bool isDead = false;
    private bool isInvulnerable = false;
    private Animator anim;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsInvulnerable => isInvulnerable;

    // Copia del otro jugador en cooperativo (#206): el dano que recibe en este cliente se reenvia
    // a su dueño, que lo aplica a su vida real (con sus escudos y su invulnerabilidad).
    public bool IsRemoteProxy { get; private set; }

    public void MakeRemoteProxy() => IsRemoteProxy = true;

    public void Init(int maxHealth)
    {
        this.maxHealth = maxHealth;
        // Viniendo de otro nivel se conserva la vida que se tenia.
        currentHealth = RunState.Active && RunState.Health > 0 ? Mathf.Min(RunState.Health, maxHealth) : maxHealth;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (gameOverUI == null)
            gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
    }

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    public void Heal(int amount)
    {
        if (IsRemoteProxy) return;
        if (isDead) return;
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (DamageOverlay.Instance != null)
            DamageOverlay.Instance.SetPulsing((float)currentHealth / maxHealth <= 0.25f);
    }

    public void TakeDamage(int amount)
    {
        if (IsRemoteProxy)
        {
            CoopEnemySync.Instance?.SendPlayerDamage(amount);
            return;
        }
        if (isDead) return;
        if (isInvulnerable) return;

        SFXManager.Instance?.PlayPlayerDamage();

        currentHealth -= amount;
        if (anim != null) anim.SetTrigger("Hit");

#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif

        if (DamageOverlay.Instance != null)
        {
            DamageOverlay.Instance.Flash();
            DamageOverlay.Instance.SetPulsing((float)currentHealth / maxHealth <= 0.25f);
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        SFXManager.Instance?.PlayPlayerDeath();
        if (anim != null) anim.SetTrigger("Dead");

#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif

        // Cooperativo (#296): si el otro sigue vivo, este queda fuera de combate hasta el final de la oleada.
        if (CoopEnemySync.Instance != null && CoopEnemySync.Instance.OnLocalDowned())
        {
            IsDowned = true;
            SetCombatActive(false);
            return;
        }

        StartCoroutine(HandleDeath());
    }

    // ── Cooperativo: fuera de combate (#296) ─────────────────────────────────

    private const float DownedAlpha = 0.35f;
    private readonly System.Collections.Generic.List<Collider2D> downedColliders = new System.Collections.Generic.List<Collider2D>();

    // Muerto en cooperativo mientras el otro jugador sigue vivo: mira la partida sin moverse ni disparar.
    public bool IsDowned { get; private set; }

    // Final de la oleada: vuelve con la mitad de la vida.
    public void Revive()
    {
        if (!IsDowned) return;
        IsDowned = false;
        isDead = false;
        currentHealth = Mathf.Max(1, maxHealth / 2);
        SetCombatActive(true);

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (DamageOverlay.Instance != null)
            DamageOverlay.Instance.SetPulsing((float)currentHealth / maxHealth <= 0.25f);
    }

    // El otro jugador tambien ha caido: fin de la partida.
    public void EndDowned()
    {
        if (!IsDowned) return;
        IsDowned = false;
        StartCoroutine(HandleDeath());
    }

    // Copia del otro jugador: se ve transparente y sin disparar mientras esta fuera de combate.
    public void ShowRemoteDowned(bool downed)
    {
        var weapon = GetComponent<WeaponController>();
        if (weapon != null) weapon.enabled = !downed;
        SetAlpha(downed ? DownedAlpha : 1f);
    }

    private void SetCombatActive(bool active)
    {
        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = active;
        var weapon = GetComponent<WeaponController>();
        if (weapon != null) weapon.enabled = active;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        // Sin colliders no recoge consumibles ni potenciadores mientras esta caido; al volver se
        // reactivan solo los que se apagaron aqui.
        if (!active)
        {
            downedColliders.Clear();
            foreach (var col in GetComponentsInChildren<Collider2D>())
                if (col.enabled) { col.enabled = false; downedColliders.Add(col); }
        }
        else
        {
            foreach (var col in downedColliders)
                if (col != null) col.enabled = true;
            downedColliders.Clear();
        }
        SetAlpha(active ? 1f : DownedAlpha);
    }

    private void SetAlpha(float alpha)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(deathFreezeDelay);
        _ = SaveMatchResult();
        if (gameOverUI != null) gameOverUI.Show();
        Time.timeScale = 0f;
    }

    private async System.Threading.Tasks.Task SaveMatchResult()
    {
        if (!string.IsNullOrEmpty(GameData.RoomId)) return;

        if (NetworkManager.Instance == null || NetworkManager.Instance.IsGuest)
        {
            Debug.Log("[PlayerHealth] Resultado no guardado: no hay sesion iniciada (invitado o prueba sin login).");
            return;
        }

        try
        {
            int userId = NetworkManager.Instance.UserId;
            await ApiManager.Instance.SaveMatchResultAsync(userId, RunState.Wave, RunState.Score);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerHealth] Error guardando resultado: {e.Message}");
        }
    }

    private void Update()
    {
        if (IsRemoteProxy) return;
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T)) TakeDamage(1);
#endif
        if (isDead && !IsDowned && Input.GetKeyDown(KeyCode.R)) RestartScene();
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        LevelManager.StartNewRun();
    }
}