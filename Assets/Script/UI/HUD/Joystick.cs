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
    [Tooltip("Fraccion del recorrido que no mueve la nave: evita que un temblor del dedo cerca del centro la mueva a toda velocidad en cualquier direccion.")]
    [Range(0f, 0.5f)] [SerializeField] private float deadZone = 0.15f;
    [Tooltip("Si el dedo se aleja mas que el recorrido, la base del joystick le sigue. Asi, para cambiar de direccion basta con mover el dedo hacia el otro lado, sin tener que volver primero hasta la base.")]
    [SerializeField] private bool baseFollowsFinger = true;

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

        if (baseFollowsFinger && pos.magnitude > range)
        {
            Vector2 excess = pos - pos.normalized * range;
            joystickContainer.anchoredPosition += excess * LocalToAreaScale();
            pos -= excess;
        }

        pos = Vector2.ClampMagnitude(pos, range);
        handle.anchoredPosition = pos;
        Direction = ApplyDeadZone(pos / range, deadZone);
    }

    // Fuera de la zona muerta, la inclinacion se reparte de 0 a 1 (sin salto al salir de ella).
    public static Vector2 ApplyDeadZone(Vector2 raw, float deadZone)
    {
        float magnitude = raw.magnitude;
        if (magnitude <= deadZone || magnitude <= 0f) return Vector2.zero;
        float scaled = Mathf.Clamp01((magnitude - deadZone) / (1f - deadZone));
        return raw / magnitude * scaled;
    }

    // joystickContainer y el area pueden tener escalas distintas: lo que se mueve el dedo en el
    // espacio del joystick, pasado al espacio del area (donde vive su anchoredPosition).
    private float LocalToAreaScale()
    {
        float containerScale = joystickContainer.lossyScale.x;
        float areaScale = area.lossyScale.x;
        return areaScale > 0f ? containerScale / areaScale : 1f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        joystickContainer.gameObject.SetActive(false);
        handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
    }
}
