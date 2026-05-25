using UnityEngine;
using TMPro;

public class RankingRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI bestWaveText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void Setup(int position, string username, int bestWave, int score)
    {
        if (positionText != null)
        {
            positionText.text = $"#{position}";
            positionText.alignment = TextAlignmentOptions.Center;
        }
        if (usernameText != null)
        {
            usernameText.text = username;
            usernameText.alignment = TextAlignmentOptions.Center;
        }
        if (bestWaveText != null)
        {
            bestWaveText.text = $"Wave {bestWave}";
            bestWaveText.alignment = TextAlignmentOptions.Center;
        }
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
            scoreText.alignment = TextAlignmentOptions.Center;
        }
    }
}