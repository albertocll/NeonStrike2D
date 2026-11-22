using UnityEngine;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public TMP_Text healthText;

    private void Update()
    {
        if (playerHealth == null || healthText == null) return;

        healthText.text = LanguageManager.Format("hud.hp", playerHealth.CurrentHealth);
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }


}