using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // Ajuste de Opciones (issue #232): 1 = joystick a la izquierda y botones reflejados a la derecha.
    public const string LeftHandedPrefKey = "left_handed_controls";

    public static bool LeftHanded
    {
        get => PlayerPrefs.GetInt(LeftHandedPrefKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(LeftHandedPrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform joystickContainer;
    [SerializeField] private float range = 100f;

    private RectTransform area;
    public Vector2 Direction { get; private set; }

    private void Awake()
    {
        area = (RectTransform)transform;
        joystickContainer.gameObject.SetActive(false);
        ApplySide();
    }

    // El joystick ocupa la mitad de la pantalla de su lado (derecha por defecto, izquierda en modo zurdo).
    // En modo zurdo, los botones del HUD se reflejan en horizontal conservando su distancia al borde.
    private void ApplySide()
    {
        bool left = LeftHanded;
        area.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
        area.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
        area.offsetMin = area.offsetMax = Vector2.zero;

        if (!left || transform.parent == null) return;

        foreach (Transform sibling in transform.parent)
        {
            if (sibling.GetComponent<Button>() == null) continue;
            var rt = (RectTransform)sibling;
            if (rt.anchorMin.x != rt.anchorMax.x) continue; // estirado en horizontal: no tiene lado
            rt.anchorMin = new Vector2(1f - rt.anchorMin.x, rt.anchorMin.y);
            rt.anchorMax = new Vector2(1f - rt.anchorMax.x, rt.anchorMax.y);
            rt.pivot = new Vector2(1f - rt.pivot.x, rt.pivot.y);
            rt.anchoredPosition = new Vector2(-rt.anchoredPosition.x, rt.anchoredPosition.y);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        joystickContainer.gameObject.SetActive(true);

        // Relativo a la propia zona del joystick (ya no ocupa toda la pantalla).
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, eventData.position, eventData.pressEventCamera, out pos);
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
