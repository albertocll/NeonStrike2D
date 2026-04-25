using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform joystickContainer;
    [SerializeField] private float range = 100f;

    private Canvas canvas;
    public Vector2 Direction { get; private set; }

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        joystickContainer.gameObject.SetActive(false);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        joystickContainer.gameObject.SetActive(true);

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera, out pos);
        joystickContainer.anchoredPosition = pos;

        handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickContainer, eventData.position, eventData.pressEventCamera, out pos);

        pos = Vector2.ClampMagnitude(pos, range);
        handle.anchoredPosition = pos;
        Direction = pos / range;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        joystickContainer.gameObject.SetActive(false);
        handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }
}