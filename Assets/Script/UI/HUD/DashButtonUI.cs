using UnityEngine;
using UnityEngine.UI;

public class DashButtonUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Image dashIcon;
    [SerializeField] private Color readyColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color cooldownColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (dashIcon == null)
            dashIcon = GetComponent<Image>();
    }

    private void Update()
    {
        if (player == null || dashIcon == null) return;

        dashIcon.color = player.IsDashReady ? readyColor : cooldownColor;
    }

    public void OnDashButtonPressed()
    {
        if (player != null)
            player.TryDash();
    }
}