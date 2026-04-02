using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuSecondaryButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(0.6f, 0.9f, 1f);

    private Vector3 baseScale;
    private Vector3 targetScale;
    private Image image;

    private void Start()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
        image = GetComponent<Image>();
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = baseScale * hoverScale;
        if (image != null) image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = baseScale;
        if (image != null) image.color = normalColor;
    }
}