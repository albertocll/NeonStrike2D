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
    [SerializeField] private float barBottomOffset = 70f;
    [SerializeField] private float panelHeight = 80f;
    [SerializeField] private float barHeight = 24f;
    [SerializeField] private float nameFontSize = 30f;

    [Header("Presentacion")]
    [SerializeField] private TMP_FontAsset introFont;
    [SerializeField] private float introFontSize = 140f;
    [SerializeField] private Color introColor = new Color(0.85f, 1f, 1f, 1f);
    [SerializeField] private Color introOutlineColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float introOutlineWidth = 0.3f;
    [SerializeField] private float introSlideDuration = 0.35f;
    [SerializeField] private float introFadeDuration = 0.4f;

    private BossSentinelController boss;
    private CanvasGroup introGroup;
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
        boss.OnCombatStarted += HandleCombatStarted;

        if (bannerText != null) bannerText.text = boss.DisplayName;
        if (healthSlider != null) healthSlider.value = 1f;
        if (panel != null) panel.SetActive(true);

        PlayIntro(boss.DisplayName);
    }

    private void HandleHealthChanged(float normalized)
    {
        if (healthSlider != null) healthSlider.value = normalized;
    }

    private void HandleCombatStarted()
    {
        HideIntro();
    }

    private void HandleDefeated()
    {
        if (panel != null) panel.SetActive(false);
        HideIntro();
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (boss == null) return;
        boss.OnHealthChanged -= HandleHealthChanged;
        boss.OnDefeated -= HandleDefeated;
        boss.OnCombatStarted -= HandleCombatStarted;
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

    // Nombre grande de presentacion, creado por codigo (sin assets nuevos).
    private void CreateIntroText()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var band = new GameObject("BossIntro", typeof(RectTransform), typeof(CanvasGroup));
        band.transform.SetParent(canvas.transform, false);
        band.transform.SetAsLastSibling();

        var bandRt = (RectTransform)band.transform;
        bandRt.anchorMin = new Vector2(0f, 0.5f);
        bandRt.anchorMax = new Vector2(1f, 0.5f);
        bandRt.pivot = new Vector2(0.5f, 0.5f);
        bandRt.sizeDelta = new Vector2(0f, introFontSize * 1.6f);
        bandRt.anchoredPosition = new Vector2(0f, 120f);

        introGroup = band.GetComponent<CanvasGroup>();
        introGroup.blocksRaycasts = false;
        introGroup.interactable = false;

        var textGo = new GameObject("BossIntroText", typeof(RectTransform));
        textGo.transform.SetParent(band.transform, false);

        introText = textGo.AddComponent<TextMeshProUGUI>();
        introText.font = introFont != null ? introFont : (bannerText != null ? bannerText.font : introText.font);
        introText.fontSize = introFontSize;
        introText.fontStyle = FontStyles.Bold;
        introText.color = introColor;
        introText.alignment = TextAlignmentOptions.Center;
        introText.textWrappingMode = TextWrappingModes.NoWrap;
        introText.raycastTarget = false;

        // fontMaterial crea una instancia: el contorno no toca el material compartido de la fuente.
        Material mat = introText.fontMaterial;
        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, introOutlineColor);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, introOutlineWidth);
        introText.outlineColor = introOutlineColor;
        introText.outlineWidth = introOutlineWidth;

        var rt = introText.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        band.SetActive(false);
    }

    private void PlayIntro(string bossName)
    {
        if (introText == null) return;
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroInRoutine(bossName));
    }

    private void HideIntro()
    {
        if (introGroup == null || !introGroup.gameObject.activeSelf) return;
        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroOutRoutine());
    }

    // Entra desde la izquierda con frenada al final y se queda hasta que empieza el combate.
    private IEnumerator IntroInRoutine(string bossName)
    {
        var rt = introText.rectTransform;
        float startX = -((RectTransform)introText.canvas.transform).rect.width;

        introText.text = bossName;
        introGroup.alpha = 1f;
        introGroup.gameObject.SetActive(true);

        for (float t = 0f; t < introSlideDuration; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / introSlideDuration, 3f);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(startX, 0f, k), 0f);
            yield return null;
        }
        rt.anchoredPosition = Vector2.zero;
        introRoutine = null;
    }

    private IEnumerator IntroOutRoutine()
    {
        float startAlpha = introGroup.alpha;
        for (float t = 0f; t < introFadeDuration; t += Time.deltaTime)
        {
            introGroup.alpha = Mathf.Lerp(startAlpha, 0f, t / introFadeDuration);
            yield return null;
        }

        introGroup.gameObject.SetActive(false);
        introText.rectTransform.anchoredPosition = Vector2.zero;
        introRoutine = null;
    }
}
