using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text waveReachedText;
    [SerializeField] private TMP_Text scoreReachedText;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Ranking")]
    [SerializeField] private RankingUI rankingUI;

    [SerializeField] private GameObject joystickContainer;
    [SerializeField] private GameObject dashButton;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject pausePanel;

    // Textos del HUD (vida, puntos y oleada) que se ocultan con el fin de la partida.
    // Al reiniciar se recarga la escena y vuelven a verse.
    [SerializeField] private GameObject[] hudToHide;

    public void Show()
    {
        if (DamageOverlay.Instance != null)
            DamageOverlay.Instance.Hide();

        if (joystickContainer != null)
            joystickContainer.SetActive(false);

        if (dashButton != null)
            dashButton.SetActive(false);

        if (pauseButton != null)
            pauseButton.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (hudToHide != null)
            foreach (var hud in hudToHide)
                if (hud != null) hud.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (waveReachedText != null && waveManager != null)
            waveReachedText.text = "oleada alcanzada " + waveManager.CurrentWave;

        if (scoreReachedText != null && ScoreManager.Instance != null)
            scoreReachedText.text = "puntos " + ScoreManager.Instance.CurrentScore;

        if (rankingUI != null)
            rankingUI.Show();

        var joystick = FindFirstObjectByType<Joystick>();
        if (joystick != null)
            joystick.gameObject.SetActive(false);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        LevelManager.StartNewRun();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneLoader.Load(mainMenuSceneName);
    }

    public void ShowRanking()
    {
        if (rankingUI != null)
            rankingUI.Show();
    }
}