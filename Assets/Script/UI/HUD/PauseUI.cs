using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject joystickContainer;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Toggle muteToggle;

    public bool IsPaused { get; private set; }

    private bool IsMultiplayer => NetworkManager.Instance != null && NetworkManager.Instance.IsConnected;

    private void Start()
    {
        if (muteToggle != null)
        {
            if (MusicManager.Instance != null)
                muteToggle.SetIsOnWithoutNotify(MusicManager.Instance.IsMuted);

            muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);
        }
    }

    private void OnMuteToggleChanged(bool isMuted)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetMuted(isMuted);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (joystickContainer != null)
            joystickContainer.SetActive(false);

        // En multijugador no detenemos el tiempo real para no desincronizar al otro jugador.
        // Solo se pausa el tiempo (Time.timeScale) en partidas en solitario.
        if (!IsMultiplayer)
            Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (joystickContainer != null)
            joystickContainer.SetActive(true);

        Time.timeScale = 1f;
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