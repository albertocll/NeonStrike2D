using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip gameMusicIntense;
    [SerializeField] private float volume = 0.5f;

    [Header("Intensidad de partida")]
    [SerializeField] private int intenseWaveThreshold = 5;
    [SerializeField] private float crossfadeDuration = 2f;

    private const string MutePrefKey = "MusicMuted";

    private AudioSource audioSource;
    private AudioSource crossfadeSource;
    private WaveManager waveManager;
    private Coroutine crossfadeRoutine;
    private bool intenseActive;

    public bool IsMuted { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsMuted = PlayerPrefs.GetInt(MutePrefKey, 0) == 1;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = IsMuted ? 0f : volume;

        var crossfadeLayer = new GameObject("MusicCrossfadeLayer");
        crossfadeLayer.transform.SetParent(transform);
        crossfadeSource = crossfadeLayer.AddComponent<AudioSource>();
        crossfadeSource.loop = true;
        crossfadeSource.volume = 0f;
    }

    private void Start()
    {
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (intenseActive || gameMusicIntense == null || waveManager == null) return;

        if (waveManager.CurrentWave >= intenseWaveThreshold)
            StartIntenseCrossfade();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LoadingScreen") return;

        if (crossfadeRoutine != null)
        {
            StopCoroutine(crossfadeRoutine);
            crossfadeRoutine = null;
        }
        crossfadeSource.Stop();
        crossfadeSource.volume = 0f;
        intenseActive = false;

        if (scene.name == "Level1")
        {
            waveManager = FindFirstObjectByType<WaveManager>();
            PlayClip(gameMusic);
        }
        else
        {
            waveManager = null;
            PlayClip(menuMusic);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource.clip == clip) return;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void StartIntenseCrossfade()
    {
        if (crossfadeRoutine != null) return;

        intenseActive = true;
        crossfadeRoutine = StartCoroutine(CrossfadeToIntense());
    }

    // ponytail: cambios de volumen/mute del slider de opciones durante el crossfade
    // (máx. crossfadeDuration segundos) se aplican al terminar el fade, no en directo.
    // El toggle de mute (.mute) sí se respeta en directo. Subir a live-tracking si
    // llega a notarse en partida.
    private IEnumerator CrossfadeToIntense()
    {
        float targetVolume = audioSource.volume;

        crossfadeSource.clip = gameMusicIntense;
        crossfadeSource.mute = audioSource.mute;
        crossfadeSource.volume = 0f;
        crossfadeSource.Play();

        float t = 0f;
        while (t < crossfadeDuration)
        {
            t += Time.deltaTime;
            float ratio = crossfadeDuration > 0f ? t / crossfadeDuration : 1f;
            crossfadeSource.mute = audioSource.mute;
            audioSource.volume = Mathf.Lerp(targetVolume, 0f, ratio);
            crossfadeSource.volume = Mathf.Lerp(0f, targetVolume, ratio);
            yield return null;
        }

        audioSource.clip = gameMusicIntense;
        audioSource.time = crossfadeSource.time % gameMusicIntense.length;
        audioSource.mute = crossfadeSource.mute;
        audioSource.volume = targetVolume;
        audioSource.Play();

        crossfadeSource.Stop();
        crossfadeSource.volume = 0f;
        crossfadeRoutine = null;
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        PlayerPrefs.SetInt(MutePrefKey, muted ? 1 : 0);
        audioSource.volume = muted ? 0f : volume;
    }
}
