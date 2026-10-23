using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private string bannerLabel = "¡CENTINELA!";

    private BossCentinelaController boss;

    private void OnEnable()
    {
        BossCentinelaController.OnAnyBossSpawned += HandleBossSpawned;
    }

    private void OnDisable()
    {
        BossCentinelaController.OnAnyBossSpawned -= HandleBossSpawned;
        Unsubscribe();
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void HandleBossSpawned(BossCentinelaController spawnedBoss)
    {
        Unsubscribe();

        boss = spawnedBoss;
        boss.OnHealthChanged += HandleHealthChanged;
        boss.OnDefeated += HandleDefeated;

        if (bannerText != null) bannerText.text = bannerLabel;
        if (healthSlider != null) healthSlider.value = 1f;
        if (panel != null) panel.SetActive(true);
    }

    private void HandleHealthChanged(float normalized)
    {
        if (healthSlider != null) healthSlider.value = normalized;
    }

    private void HandleDefeated()
    {
        if (panel != null) panel.SetActive(false);
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (boss == null) return;
        boss.OnHealthChanged -= HandleHealthChanged;
        boss.OnDefeated -= HandleDefeated;
        boss = null;
    }
}
