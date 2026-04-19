using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text waveReachedText;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Ranking")]
    [SerializeField] private RankingUI rankingUI;

    [SerializeField] private GameObject joystickContainer;

    public void Show()
    {
        if (joystickContainer != null)
            joystickContainer.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (waveReachedText != null && waveManager != null)
            waveReachedText.text = "WAVE " + waveManager.CurrentWave;

        if (rankingUI != null)
            rankingUI.Show();
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

    public void ShowRanking()
    {
        if (rankingUI != null)
            rankingUI.Show();
    }
}