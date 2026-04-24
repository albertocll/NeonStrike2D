using UnityEngine;
using TMPro;

public class RankingRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI bestWaveText;

    public void Setup(int position, string username, int bestWave)
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
            bestWaveText.text = bestWave.ToString();
            bestWaveText.alignment = TextAlignmentOptions.Center;
        }
    }
}