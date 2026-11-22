using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject optionsWindow;
    public Toggle musicToggle;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle sfxToggle;

    [Header("Filas de la ventana (las coloca su layout)")]
    public Button backButton;
    public RectTransform leftHandedRow;
    public RectTransform accountRow;
    public RectTransform legalRow;

    private const string KeyLeftHanded = "options.leftHanded";
    private const string KeyLogout = "options.logout";
    private const float ToggleFontSize = 34f;
    private static readonly Vector2 LeftHandedSize = new Vector2(330f, 40f);
    public const float AccountFontSize = 28f;
    public static readonly Vector2 AccountButtonSize = new Vector2(300f, 56f);

    private float cachedVolume = 1f;

    // Imagen de fondo de la ventana de Opciones: está en el hijo Fondo para que el layout no use
    // el tamaño del sprite. Si no existe, la de la propia ventana.
    public static Image WindowBackground(Transform window)
    {
        var background = window.Find("Fondo");
        return background != null ? background.GetComponent<Image>() : window.GetComponent<Image>();
    }

    private void Start()
    {
        CreateLeftHandedToggle();
        CreateLogoutButton();

        // Inicializamos valores
        cachedVolume = musicSlider.value;
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.Volume = cachedVolume;
            MusicManager.Instance.Silenced = false;
        }
        musicToggle.isOn = true;

        // Eventos
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);

        if (sfxSlider != null && SFXManager.Instance != null)
        {
            sfxSlider.value = SFXManager.Instance.Volume;
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (sfxToggle != null && SFXManager.Instance != null)
        {
            sfxToggle.isOn = !SFXManager.Instance.Muted;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
        }
    }

    // Ajuste de controles para zurdos (issue #232): copia del toggle de efectos en su propia fila.
    private void CreateLeftHandedToggle()
    {
        if (sfxToggle == null || leftHandedRow == null) return;

        var toggle = Instantiate(sfxToggle, leftHandedRow);
        toggle.name = "LeftHandedToggle";
        toggle.onValueChanged = new Toggle.ToggleEvent(); // sin los listeners copiados del toggle de efectos
        ((RectTransform)toggle.transform).sizeDelta = LeftHandedSize;

        var label = toggle.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            MenuUiFactory.SetKey(label, KeyLeftHanded);
            label.enableAutoSizing = false;
            label.fontSize = ToggleFontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        toggle.isOn = Joystick.LeftHanded;
        toggle.onValueChanged.AddListener(isOn => Joystick.LeftHanded = isOn);
    }

    private void Update()
    {
        // La sesión recordada se restaura en segundo plano, así que la fila de cuenta se comprueba cada frame.
        if (accountRow != null)
        {
            bool hasSession = NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.Token);
            if (accountRow.gameObject.activeSelf != hasSession) accountRow.gameObject.SetActive(hasSession);
        }
    }

    // Botón de cerrar sesión (issue #230): primero de la fila de cuenta.
    private void CreateLogoutButton()
    {
        if (backButton == null || accountRow == null)
        {
            Debug.LogWarning("[OptionsMenu] Falta el botón de volver o la fila de cuenta; no se crea el botón de cerrar sesion.");
            return;
        }

        var button = MenuUiFactory.CloneButton(backButton, accountRow, "LogoutButton", KeyLogout,
            AccountButtonSize, AccountFontSize, OnLogoutClicked);
        button.transform.SetAsFirstSibling();
    }

    private async void OnLogoutClicked()
    {
        var menuManager = FindFirstObjectByType<MenuManager>();
        if (menuManager != null) menuManager.CerrarPaneles();
        await LoginUI.LogoutAsync();
    }

    public void OnSfxSliderChanged(float value)
    {
        SFXManager.Instance?.SetVolume(value);
    }

    public void OnSfxToggleChanged(bool isOn)
    {
        SFXManager.Instance?.SetMuted(!isOn);
    }

    public void OnMusicSliderChanged(float value)
    {
        // Si la música está apagada,
        // NO aplicamos volumen al AudioSource
        if (!musicToggle.isOn)
        {
            cachedVolume = value;   // guardamos el valor por si vuelve a ON
            return;
        }

        // Si está encendida → aplicamos el volumen
        if (MusicManager.Instance != null) MusicManager.Instance.Volume = value;
        cachedVolume = value;
    }

    public void OnMusicToggleChanged(bool isOn)
    {
        if (isOn)
        {
            // Música ON → restauramos volumen al valor del slider
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.Silenced = false;
                MusicManager.Instance.Volume = cachedVolume;
            }
        }
        else
        {
            // Música OFF → muteamos solamente
            if (MusicManager.Instance != null) MusicManager.Instance.Silenced = true;
        }
    }

    // Botón de borrar cuenta (issue #280) y enlaces legales (issue #117), creados por código.
    private void Awake()
    {
        if (!TryGetComponent(out DeleteAccountButton _)) gameObject.AddComponent<DeleteAccountButton>();
        if (!TryGetComponent(out LegalLinks _)) gameObject.AddComponent<LegalLinks>();
    }
}
