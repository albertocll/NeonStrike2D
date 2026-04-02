using UnityEngine;

public class MenuTitlePulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float floatAmount = 5f;

    private RectTransform rectTransform;
    private Vector2 basePosition;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        basePosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.unscaledTime * pulseSpeed);
        float offsetY = wave * floatAmount;
        rectTransform.anchoredPosition = basePosition + new Vector2(0f, offsetY);
    }
}