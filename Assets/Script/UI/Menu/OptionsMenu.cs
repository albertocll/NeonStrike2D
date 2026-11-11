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

    [Header("Audio")]
    public AudioSource musicSource;

    private float cachedVolume = 1f;

    // Botón LOGOUT (issue #230): copia del botón de volver, reflejado al otro lado de la ventana.
    private GameObject logoutButton;

    private void Start()
    {
        CreateLogoutButton();

        if (musicSource == null)
            musicSource = FindFirstObjectByType<MusicManager>().GetComponent<AudioSource>();

        // Inicializamos valores
        cachedVolume = musicSlider.value;
        musicSource.volume = cachedVolume;
        musicSource.mute = false;
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

    private void Update()
    {
        // La sesión recordada se restaura en segundo plano, así que la visibilidad se comprueba cada frame.
        if (logoutButton != null)
        {
            bool hasSession = NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.Token);
            if (logoutButton.activeSelf != hasSession) logoutButton.SetActive(hasSession);
        }
    }

    private void CreateLogoutButton()
    {
        Button back = null;
        foreach (var b in optionsWindow.GetComponentsInChildren<Button>(true))
            if (b.name == "BackButton") { back = b; break; }
        if (back == null)
        {
            Debug.LogWarning("[OptionsMenu] No se encontró BackButton; no se crea el botón LOGOUT.");
            return;
        }

        var button = Instantiate(back, back.transform.parent);
        button.name = "LogoutButton";
        button.onClick = new Button.ButtonClickedEvent(); // quita el CloseOptions copiado del botón de volver
        button.onClick.AddListener(OnLogoutClicked);

        var rect = (RectTransform)button.transform;
        var backRect = (RectTransform)back.transform;
        rect.anchoredPosition = new Vector2(-backRect.anchoredPosition.x, backRect.anchoredPosition.y);

        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "LOGOUT";
            // Ensancha el fondo si el texto no cabe en el ancho del botón de volver.
            float needed = label.GetPreferredValues("LOGOUT").x + 20f;
            if (needed > rect.sizeDelta.x) rect.sizeDelta = new Vector2(needed, rect.sizeDelta.y);
        }

        logoutButton = button.gameObject;
        logoutButton.SetActive(false);
    }

    private async void OnLogoutClicked()
    {
        logoutButton.GetComponent<Button>().interactable = false;
        CloseOptions();
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

    public void OpenOptions()
    {
        optionsWindow.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsWindow.SetActive(false);
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
        musicSource.volume = value;
        cachedVolume = value;
    }

    public void OnMusicToggleChanged(bool isOn)
    {
        if (isOn)
        {
            // Música ON → restauramos volumen al valor del slider
            musicSource.mute = false;
            musicSource.volume = cachedVolume;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        else
        {
            // Música OFF → muteamos solamente
            musicSource.mute = true;
        }
    }

    // Botón de borrar cuenta (issue #280), creado por código.
    private void Awake()
    {
        if (!TryGetComponent(out DeleteAccountButton _)) gameObject.AddComponent<DeleteAccountButton>();
    }
}
