using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    private int currentScore;

    public int CurrentScore => currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Viniendo de otro nivel se conserva la puntuacion de la partida.
        currentScore = RunState.Active ? RunState.Score : 0;
        RunState.Score = currentScore;
        UpdateUI();
    }

    public void AddScore(int points)
    {
        currentScore += points;
        RunState.Score = currentScore;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {currentScore}";
    }
}