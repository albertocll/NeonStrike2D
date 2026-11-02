using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Niveles (ordenados por primera oleada)")]
    [SerializeField] private LevelData[] levels;
    [SerializeField] private GameObject currentMap;

    [Header("Transicion")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float fontSize = 120f;
    [SerializeField] private Color textColor = new Color(0.85f, 1f, 1f, 1f);
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float holdDuration = 1.5f;

    private CanvasGroup overlayGroup;
    private TextMeshProUGUI overlayText;

    public LevelData CurrentLevel { get; private set; }

    private void Awake()
    {
        CreateOverlay();
    }

    // El ultimo nivel cuya primera oleada ya se ha alcanzado; pasado el ultimo, se queda en el.
    public LevelData LevelForWave(int wave)
    {
        LevelData result = levels.Length > 0 ? levels[0] : null;
        foreach (LevelData level in levels)
        {
            if (level != null && wave >= level.firstWave)
                result = level;
        }
        return result;
    }

    // Sustituye el mapa actual (tambien el que viene colocado en la escena) por el del nivel.
    public void ShowLevel(LevelData level)
    {
        if (level == null || level == CurrentLevel) return;

        CurrentLevel = level;
        if (currentMap != null) Destroy(currentMap);
        currentMap = level.mapPrefab != null ? Instantiate(level.mapPrefab) : null;
    }

    public IEnumerator TransitionTo(LevelData level)
    {
        overlayText.text = level.displayName;
        overlayGroup.gameObject.SetActive(true);
        yield return Fade(0f, 1f);

        ShowLevel(level);
        MoveLocalPlayerToSpawn();

        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f);
        overlayGroup.gameObject.SetActive(false);
    }

    // Solo se mueve el jugador local: el remoto llega por PlayerSync desde su propio cliente.
    private void MoveLocalPlayerToSpawn()
    {
        if (currentMap == null) return;

        GameObject player = GameObject.FindWithTag("Player");
        Transform point = currentMap.transform.Find(GameData.IsHost ? "SpawnPointLocal" : "SpawnPointRemote");
        if (player == null || point == null) return;

        player.transform.position = point.position;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = point.position;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            overlayGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        overlayGroup.alpha = to;
    }

    // Pantalla negra con el nombre del nivel, creada por codigo. Va la primera del canvas
    // para que el HUD y los paneles de pausa/game over queden por encima.
    private void CreateOverlay()
    {
        if (canvas == null) return;

        var overlay = new GameObject("LevelTransition", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        overlay.transform.SetAsFirstSibling();

        var rt = (RectTransform)overlay.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        var image = overlay.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        overlayGroup = overlay.GetComponent<CanvasGroup>();
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable = false;

        var textGo = new GameObject("LevelTransitionText", typeof(RectTransform));
        textGo.transform.SetParent(overlay.transform, false);

        overlayText = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) overlayText.font = font;
        overlayText.fontSize = fontSize;
        overlayText.fontStyle = FontStyles.Bold;
        overlayText.color = textColor;
        overlayText.alignment = TextAlignmentOptions.Center;
        overlayText.textWrappingMode = TextWrappingModes.NoWrap;
        overlayText.raycastTarget = false;

        var textRt = overlayText.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        overlay.SetActive(false);
    }
}
