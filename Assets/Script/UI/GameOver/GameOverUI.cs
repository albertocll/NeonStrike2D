using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text waveReachedText;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public void Show()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (waveReachedText != null && waveManager != null)
            waveReachedText.text = "WAVE " + waveManager.CurrentWave;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}