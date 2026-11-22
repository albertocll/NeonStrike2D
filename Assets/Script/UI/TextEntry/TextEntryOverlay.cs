using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ventana de escritura a pantalla completa (al estilo de las consolas): fondo negro translúcido y, en la
// mitad superior (o justo encima del teclado si el sistema da su altura), el nombre del campo, una caja
// de texto grande y los botones aceptar y cancelar. La caja grande es una copia del campo original, así
// que conserva el tipo de contenido, el teclado (correo, contraseña...) y los asteriscos.
public class TextEntryOverlay : MonoBehaviour
{
    private const float FieldWidth = 1100f;
    private const float FieldHeight = 110f;
    private const float FieldFontSize = 56f;
    private const float LabelFontSize = 44f;
    private const float ButtonFontSize = 32f;
    private static readonly Vector2 ButtonSize = new Vector2(260f, 70f);
    private static readonly Color Dim = new Color(0f, 0f, 0f, 1f);

    private static TextEntryOverlay current;

    private TMP_InputField original;
    private TMP_InputField bigField;
    private RectTransform area;

    public static bool IsOpen => current != null;

    public static void Open(TextEntryField source)
    {
        if (current != null || source == null || source.Field == null) return;

        var go = new GameObject("TextEntryOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        current = go.AddComponent<TextEntryOverlay>();
        current.Build(source);
    }

    private void Build(TextEntryField source)
    {
        original = source.Field;
        original.DeactivateInputField();

        var canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var dim = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        var dimRect = (RectTransform)dim.transform;
        dimRect.SetParent(transform, false);
        MenuUiFactory.Stretch(dimRect);
        dim.GetComponent<Image>().color = Dim;

        area = (RectTransform)new GameObject("Zona", typeof(RectTransform)).transform;
        area.SetParent(dimRect, false);
        area.anchorMin = new Vector2(0f, 0.5f);
        area.anchorMax = Vector2.one;
        area.offsetMin = area.offsetMax = Vector2.zero;
        var column = area.gameObject.AddComponent<VerticalLayoutGroup>();
        column.spacing = 50f;
        column.childAlignment = TextAnchor.MiddleCenter;
        column.childControlWidth = column.childControlHeight = false;
        column.childForceExpandWidth = column.childForceExpandHeight = false;

        CreateLabel(source.Label);
        bigField = CreateBigField();

        var template = FindButtonTemplate(original);
        var row = MenuUiFactory.CreateGroup(area, "Botones", new Vector2(FieldWidth, ButtonSize.y), true, 60f);
        if (template != null)
        {
            MenuUiFactory.CloneButton(template, row, "Aceptar", "entry.accept", ButtonSize, ButtonFontSize, Accept);
            MenuUiFactory.CloneButton(template, row, "Cancelar", "entry.cancel", ButtonSize, ButtonFontSize, Close);
        }

        StartCoroutine(FocusNextFrame());
    }

    private void CreateLabel(string text)
    {
        var label = Instantiate(original.textComponent, area);
        label.name = "Nombre";
        if (label.TryGetComponent(out LocalizedText localized)) Destroy(localized);
        label.text = text;
        label.color = Color.white;
        label.enableAutoSizing = false;
        label.fontSize = LabelFontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        var rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(FieldWidth, LabelFontSize * 1.4f);
    }

    // Copia del campo original bajo un padre inactivo, para quitarle el TextEntryField antes de que
    // se active (si no, al seleccionarla volvería a abrir otra ventana).
    private TMP_InputField CreateBigField()
    {
        var holder = new GameObject("Copia", typeof(RectTransform));
        holder.SetActive(false);
        holder.transform.SetParent(area, false);

        var big = Instantiate(original, holder.transform);
        if (big.TryGetComponent(out TextEntryField hook)) DestroyImmediate(hook);
        big.name = "CajaGrande";
        big.transform.SetParent(area, false);
        Destroy(holder);

        var rect = (RectTransform)big.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(FieldWidth, FieldHeight);

        // Sin los eventos del campo original: solo se copia el texto al aceptar.
        big.onValueChanged = new TMP_InputField.OnChangeEvent();
        big.onEndEdit = new TMP_InputField.SubmitEvent();
        big.onSubmit = new TMP_InputField.SubmitEvent();
        big.onSelect = new TMP_InputField.SelectionEvent();
        big.onDeselect = new TMP_InputField.SelectionEvent();
        big.onValidateInput = original.onValidateInput;
        big.characterLimit = original.characterLimit;

        big.shouldHideMobileInput = true;
        big.shouldHideSoftKeyboard = false;
        if (big.textComponent != null)
        {
            big.textComponent.enableAutoSizing = false;
            big.textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
        }
        if (big.placeholder is TMP_Text placeholder)
        {
            placeholder.enableAutoSizing = false;
            placeholder.verticalAlignment = VerticalAlignmentOptions.Middle;
        }
        big.pointSize = FieldFontSize;
        big.text = original.text;
        big.onSubmit.AddListener(_ => Accept());
        return big;
    }

    // Botón con el estilo del juego (fondo y etiqueta en Orbitronio) del mismo panel que el campo;
    // si no hay, del resto de la pantalla.
    private static Button FindButtonTemplate(TMP_InputField field)
    {
        var canvas = field.GetComponentInParent<Canvas>();
        var scopes = new[] { field.transform.parent, canvas != null ? canvas.rootCanvas.transform : null };
        foreach (var scope in scopes)
        {
            if (scope == null) continue;
            foreach (var button in scope.GetComponentsInChildren<Button>(true))
            {
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null && label.font != null && label.font.name.Contains("Orbitronio")
                    && button.GetComponentInParent<TMP_InputField>(true) == null)
                    return button;
            }
        }
        return null;
    }

    private IEnumerator FocusNextFrame()
    {
        // Seleccionar dentro del OnSelect del campo original daría error: se espera un frame.
        yield return null;
        if (bigField == null) yield break;
        EventSystem.current?.SetSelectedGameObject(bigField.gameObject);
        bigField.ActivateInputField();
    }

    private void Update()
    {
        // Con la altura del teclado, la zona de escritura queda justo encima; si no, en la mitad superior.
        float bottom = 0.5f;
        if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0f && Screen.height > 0)
            bottom = Mathf.Clamp(TouchScreenKeyboard.area.height / Screen.height, 0.2f, 0.7f);
        if (!Mathf.Approximately(area.anchorMin.y, bottom))
            area.anchorMin = new Vector2(0f, bottom);

        // Intro del teclado del móvil.
        var keyboard = bigField != null ? bigField.touchScreenKeyboard : null;
        if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Done)
            Accept();
    }

    private void Accept()
    {
        if (original != null && bigField != null) original.text = bigField.text;
        Close();
    }

    private void Close()
    {
        if (current != this) return;
        current = null;
        EventSystem.current?.SetSelectedGameObject(null);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }
}
