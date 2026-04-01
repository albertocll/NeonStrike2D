using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text waveReachedText;

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.transform.root.gameObject.SetActive(true);
    }

    public void Show(int waveReached)
    {
        Debug.Log("SHOW GAME OVER");
        Debug.Log(gameOverPanel.name);

        if (gameOverPanel != null)
            gameObject.SetActive(true);

        if (waveReachedText != null)
            waveReachedText.text = "Wave alcanzada: " + waveReached;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}