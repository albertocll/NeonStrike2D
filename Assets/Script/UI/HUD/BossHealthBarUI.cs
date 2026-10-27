using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text bannerText;

    [Header("Barra (abajo, ancha)")]
    [SerializeField, Range(0f, 0.5f)] private float barSideMargin = 0.2f;
    [SerializeField] private float barBottomOffset = 24f;
    [SerializeField] private float panelHeight = 80f;
    [SerializeField] private float barHeight = 24f;
    [SerializeField] private float nameFontSize = 30f;

    [Header("Presentacion")]
    [SerializeField] private float introFontSize = 140f;
    [SerializeField] private Color introColor = new Color(0f, 0.9f, 1f, 1f);
    [SerializeField] private float introSlideDuration = 0.35f;
    [SerializeField] private float introHoldDuration = 2f;
    [SerializeField] private float introFadeDuration = 0.4f;

    private BossSentinelController boss;
    private TextMeshProUGUI introText;
    private Coroutine introRoutine;

    private void Awake()
    {
        LayoutBar();
        CreateIntroText();
    }

    private void OnEnable()
    {
        BossSentinelController.OnAnyBossSpawned += HandleBossSpawned;
    }

    private void OnDisable()
    {
        BossSentinelController.OnAnyBossSpawned -= HandleBossSpawned;
        Unsubscribe();
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void HandleBossSpawned(BossSentinelController spawnedBoss)
    {
        Unsubscribe();

        boss = spawnedBoss;
        boss.OnHealthChanged += HandleHealthChanged;
        boss.OnDefeated += HandleDefeated;

        if (bannerText != null) bannerText.text = boss.DisplayName;
        if (healthSlider != null) healthSlider.value = 1f;
        if (panel != null) panel.SetActive(true);

        PlayIntro(boss.DisplayName);
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

    // Coloca la barra abajo del todo, ancha, con el nombre encima.
    private void LayoutBar()
    {
        if (panel != null)
        {
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = new Vector2(barSideMargin, 0f);
            rt.anchorMax = new Vector2(1f - barSideMargin, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, barBottomOffset);
            rt.sizeDelta = new Vector2(0f, panelHeight);
        }

        if (healthSlider != null)
        {
            var rt = (RectTransform)healthSlider.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 10f);
            rt.sizeDelta = new Vector2(-20f, barHeight);
        }

        if (bannerText != null)
        {
            var rt = bannerText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -6f);
            rt.sizeDelta = new Vector2(-20f, panelHeight - barHeight - 16f);
            bannerText.fontSize = nameFontSize;
            bannerText.alignment = TextAlignmentOptions.Left;
        }
    }

    // Texto grande de presentacion, creado por codigo (sin assets nuevos).
    private void CreateIntroText()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("BossIntroText", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        introText = go.AddComponent<TextMeshProUGUI>();
        if (bannerText != null) introText.font = bannerText.font;
        introText.fontSize = introFontSize;
        introText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        introText.color = introColor;
        introText.alignment = TextAlignmentOptions.Center;
        introText.textWrappingMode = TextWrappingModes.NoWrap;
        introText.raycastTarget = false;

        var rt = introText.rectTransform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, introFontSize * 1.5f);
        rt.anchoredPosition = new Vector2(0f, 120f);

        go.SetActive(false);
    }

    private void PlayIntro(string bossName)
    {
        if (introText == null) return;
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroRoutine(bossName));
    }

    private IEnumerator IntroRoutine(string bossName)
    {
        var rt = introText.rectTransform;
        float y = rt.anchoredPosition.y;
        float startX = -((RectTransform)introText.canvas.transform).rect.width;

        introText.text = bossName;
        introText.alpha = 1f;
        introText.gameObject.SetActive(true);

        // Entra desde la izquierda con frenada al final.
        for (float t = 0f; t < introSlideDuration; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / introSlideDuration, 3f);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(startX, 0f, k), y);
            yield return null;
        }
        rt.anchoredPosition = new Vector2(0f, y);

        yield return new WaitForSeconds(introHoldDuration);

        for (float t = 0f; t < introFadeDuration; t += Time.deltaTime)
        {
            introText.alpha = 1f - t / introFadeDuration;
            yield return null;
        }

        introText.gameObject.SetActive(false);
        introRoutine = null;
    }
}
