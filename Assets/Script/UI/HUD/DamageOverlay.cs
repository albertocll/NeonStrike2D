using UnityEngine;
using UnityEngine.UI;

public class DamageOverlay : MonoBehaviour
{
    public static DamageOverlay Instance { get; private set; }

    [SerializeField] private Image overlayImage;
    [SerializeField] private float flashAlpha = 0.4f;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private float lowHealthThreshold = 0.25f;
    [SerializeField] private float pulseSpeed = 2f;

    private bool isPulsing = false;
    private float currentAlpha = 0f;

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
        if (overlayImage != null)
            SetAlpha(0f);
    }

    public void Flash()
    {
        currentAlpha = flashAlpha;
    }

    public void SetPulsing(bool pulsing)
    {
        isPulsing = pulsing;
    }

    private void Update()
    {
        if (overlayImage == null) return;

        if (isPulsing)
        {
            currentAlpha = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * flashAlpha * 0.6f;
        }
        else if (currentAlpha > 0f)
        {
            currentAlpha -= fadeSpeed * Time.deltaTime;
            if (currentAlpha < 0f) currentAlpha = 0f;
        }

        SetAlpha(currentAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color c = overlayImage.color;
        c.a = alpha;
        overlayImage.color = c;
    }

    public void Hide()
    {
        isPulsing = false;
        currentAlpha = 0f;
        SetAlpha(0f);
    }
}