using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Paso de un nivel al siguiente: salto a hiperespacio mientras se carga la escena del nivel.
// Vive en Resources/LevelTransition.prefab y sobrevive al cambio de escena hasta que termina.
public class LevelTransition : MonoBehaviour
{
    private const string PrefabPath = "LevelTransition";

    [Header("Duracion")]
    [SerializeField] private float minDuration = 3f;
    [SerializeField] private float fadeDuration = 0.35f;
    [Tooltip("Segundos maximos esperando a que el otro jugador cargue el nivel, por si se desconecta.")]
    [SerializeField] private float partnerTimeout = 10f;

    [Header("Texto")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float fontSize = 34f;
    [SerializeField] private Color textColor = new Color(0.75f, 0.95f, 1f, 1f);

    [Header("Estrellas")]
    [SerializeField] private int starCount = 160;
    [SerializeField] private float starThickness = 3f;
    [SerializeField] private float starMaxSpeed = 2600f;
    [SerializeField] private float starStretch = 0.35f;

    [Header("Nave")]
    [SerializeField] private float shipHeight = 170f;

    public static bool IsRunning { get; private set; }

    private CanvasGroup group;
    private RectTransform starsRoot;
    private RectTransform shipRect;
    private RectTransform[] stars;
    private float[] starDistance;
    private float startTime;

    public static void Begin(LevelData next)
    {
        if (IsRunning || next == null) return;

        LevelTransition prefab = Resources.Load<LevelTransition>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[LevelTransition] Falta Resources/{PrefabPath}.prefab");
            return;
        }

        IsRunning = true;

        GameObject player = GameObject.FindWithTag("Player");
        SpriteRenderer playerSprite = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        RunState.Capture(player, next.firstWave - 1);

        LevelTransition transition = Instantiate(prefab);
        DontDestroyOnLoad(transition.gameObject);
        transition.StartCoroutine(transition.Run(next, playerSprite != null ? playerSprite.sprite : null));
    }

    private IEnumerator Run(LevelData next, Sprite ship)
    {
        Time.timeScale = 1f;
        Build(ship, next.displayName);
        startTime = Time.unscaledTime;

        yield return Fade(0f, 1f);

        AsyncOperation load = SceneManager.LoadSceneAsync(next.sceneName);
        load.allowSceneActivation = false;
        while (load.progress < 0.9f || Time.unscaledTime - startTime < minDuration)
            yield return null;

        load.allowSceneActivation = true;
        while (!load.isDone)
            yield return null;

        if (GameData.IsCoop)
            yield return WaitForPartner(next.sceneName);

        yield return Fade(1f, 0f);

        IsRunning = false;
        Destroy(gameObject);
    }

    // Cada cliente avisa al servidor de que ha cargado; el servidor manda LevelStart con los dos avisos.
    private IEnumerator WaitForPartner(string sceneName)
    {
        bool released = false;
        Action<string> onStart = _ => released = true;
        Action<string> onLeft = _ => released = true;

        NetworkManager network = NetworkManager.Instance;
        network.OnLevelStart += onStart;
        network.OnPlayerLeft += onLeft;
        _ = network.SendLevelLoadedAsync(GameData.RoomId, sceneName);

        float waitStart = Time.unscaledTime;
        while (!released && Time.unscaledTime - waitStart < partnerTimeout)
            yield return null;

        if (!released)
            Debug.LogWarning("[LevelTransition] El otro jugador no ha cargado el nivel a tiempo; se sigue sin esperarle.");

        network.OnLevelStart -= onStart;
        network.OnPlayerLeft -= onLeft;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        group.alpha = to;
    }

    private void Update()
    {
        if (stars == null) return;

        float dt = Time.unscaledDeltaTime;
        float elapsed = Time.unscaledTime - startTime;

        // Arranca despacio (estrellas como puntos) y acelera hasta el salto.
        float ramp = Mathf.Clamp01(elapsed / 1.2f);
        float speed = Mathf.Lerp(60f, starMaxSpeed, ramp * ramp);
        float maxRadius = starsRoot.rect.size.magnitude * 0.5f;

        for (int i = 0; i < stars.Length; i++)
        {
            starDistance[i] += speed * dt * (0.2f + starDistance[i] / maxRadius);
            if (starDistance[i] > maxRadius)
                starDistance[i] = UnityEngine.Random.Range(10f, maxRadius * 0.15f);

            RectTransform star = stars[i];
            Vector2 direction = star.right;
            float length = Mathf.Max(starThickness, starDistance[i] * starStretch * ramp);
            star.anchoredPosition = direction * starDistance[i];
            star.sizeDelta = new Vector2(length, starThickness);
        }

        // La nave avanza: un leve vaiven hacia delante.
        shipRect.anchoredPosition = new Vector2(Mathf.Sin(elapsed * 3f) * 14f + ramp * 20f, Mathf.Sin(elapsed * 5f) * 4f);
    }

    private void Build(Sprite ship, string text)
    {
        var canvasGo = new GameObject("LevelTransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = canvasGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;

        Image background = CreateImage("Background", canvasGo.transform);
        Stretch(background.rectTransform);
        background.color = Color.black;

        starsRoot = new GameObject("Stars", typeof(RectTransform)).GetComponent<RectTransform>();
        starsRoot.SetParent(canvasGo.transform, false);
        Stretch(starsRoot);

        stars = new RectTransform[starCount];
        starDistance = new float[starCount];
        for (int i = 0; i < starCount; i++)
        {
            Image star = CreateImage("Star", starsRoot);
            star.color = new Color(0.85f, 0.95f, 1f, UnityEngine.Random.Range(0.5f, 1f));
            RectTransform rt = star.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            stars[i] = rt;
            starDistance[i] = UnityEngine.Random.Range(10f, 900f);
        }

        Image shipImage = CreateImage("Ship", canvasGo.transform);
        shipImage.sprite = ship;
        shipImage.enabled = ship != null;
        shipImage.preserveAspect = true;
        shipRect = shipImage.rectTransform;
        shipRect.anchorMin = shipRect.anchorMax = new Vector2(0.5f, 0.5f);
        float aspect = ship != null ? ship.rect.width / ship.rect.height : 1f;
        shipRect.sizeDelta = new Vector2(shipHeight * aspect, shipHeight);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        var label = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontStyle = FontStyles.Normal; // tal cual viene displayName: minusculas salvo la inicial de cada palabra
        label.fontSize = fontSize;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(1200f, 60f);
        textRect.anchoredPosition = new Vector2(0f, -shipHeight * 0.5f - 70f);
    }

    private static Image CreateImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
}
