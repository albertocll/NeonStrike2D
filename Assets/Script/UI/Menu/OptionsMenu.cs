using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject optionsWindow;
    public Toggle musicToggle;
    public Slider musicSlider;

    [Header("Audio")]
    public AudioSource musicSource;

    private float cachedVolume = 1f;

    private void Start()
    {
        // Inicializamos valores
        cachedVolume = musicSlider.value;

        musicSource.volume = cachedVolume;
        musicSource.mute = false;

        musicToggle.isOn = true;

        // Eventos
        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
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
}
