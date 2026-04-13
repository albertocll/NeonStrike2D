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

    private void Awake()
    {
        Time.timeScale = 1f;
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
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
            Debug.Log($"Guardando resultado — userId: {userId}, wave: {wave}");
            await ApiManager.Instance.SaveMatchResultAsync(userId, wave);
            Debug.Log("Resultado guardado correctamente");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error guardando resultado: {e.Message}");
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