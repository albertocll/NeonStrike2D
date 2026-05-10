using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float deathFreezeDelay = 0.6f;
    [SerializeField] private GameOverUI gameOverUI;

    [Header("Refs")]
    [SerializeField] private WaveManager waveManager;

    private int currentHealth;
    private bool isDead = false;
    private Animator anim;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public void Init(int maxHealth)
    {
        this.maxHealth = maxHealth;
        currentHealth = maxHealth;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        if (gameOverUI == null)
            gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
    }


    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        if (anim != null) anim.SetTrigger("Hit");
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        if (anim != null) anim.SetTrigger("Dead");
        StartCoroutine(HandleDeath());
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
        try
        {
            int wave = waveManager != null ? waveManager.CurrentWave : 0;
            int userId = NetworkManager.Instance.UserId;
            await ApiManager.Instance.SaveMatchResultAsync(userId, wave);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerHealth] Error guardando resultado: {e.Message}");
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T)) TakeDamage(1);
#endif
        if (isDead && Input.GetKeyDown(KeyCode.R)) RestartScene();
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}