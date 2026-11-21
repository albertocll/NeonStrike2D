using UnityEngine;
using UnityEngine.SceneManagement;

// Música del juego (issue #262). Pistas:
// - Menú: menuMusic, en bucle en MainMenu, CharacterSelect y el resto de pantallas fuera de nivel.
// - Zona: LevelData.zoneMusic de cada nivel, en las oleadas normales (en Sector7, durante todo el nivel).
// - Crescendo: crescendoMusicA / crescendoMusicB, en las dos oleadas antes del jefe, alternándose de un nivel
//   a otro (primer nivel A, segundo B, tercero A...).
// - Jefe: LevelData.bossMusic, en la oleada del jefe del nivel.
// Si falta una pista se usa la actual: gameMusic para la zona y gameMusicIntense para el crescendo y el jefe.
// Los cambios de pista son fundidos entre dos AudioSource; si la pista no cambia, sigue sonando sin cortes.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public enum LevelTrack { Zone, Crescendo, Boss }

    [Header("Pistas")]
    [SerializeField] private AudioClip menuMusic;
    [Tooltip("Crescendo de los niveles 1, 3 y 5 (Level1, PlasmaFrontier, CorruptedSignalZone).")]
    [SerializeField] private AudioClip crescendoMusicA;
    [Tooltip("Crescendo de los niveles 2, 4 y 6 (TheGrid, DroneHive, CoreChamber).")]
    [SerializeField] private AudioClip crescendoMusicB;

    [Header("Sustitutos mientras falten pistas")]
    [Tooltip("Se usa en los niveles sin zoneMusic en su LevelData.")]
    [SerializeField] private AudioClip gameMusic;
    [Tooltip("Se usa como crescendo y como jefe mientras falten esas pistas.")]
    [SerializeField] private AudioClip gameMusicIntense;
    [Tooltip("Segundo en el que empieza gameMusicIntense cuando suena como sustituto.")]
    [SerializeField] private float gameMusicIntenseStartOffset = 0f;

    [Header("Volumen y fundidos")]
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float crossfadeDuration = 2f;

    private const string MutePrefKey = "MusicMuted";

    // sources[active] es la pista que suena o entra; la otra se apaga.
    private readonly AudioSource[] sources = new AudioSource[2];
    private readonly float[] gains = new float[2];
    private int active;

    private WaveManager waveManager;
    private LevelData currentLevel;
    private int currentLevelIndex;
    private bool inLevel;

    public bool IsMuted { get; private set; }

    // Volumen del deslizador de Opciones.
    public float Volume
    {
        get => volume;
        set => volume = Mathf.Clamp01(value);
    }

    // Interruptor de música de Opciones: silencia sin tocar el volumen ni guardar nada.
    public bool Silenced { get; set; }

    public AudioClip CurrentClip => sources[active] != null ? sources[active].clip : null;

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

        sources[0] = CreateSource(gameObject);
        var crossfadeLayer = new GameObject("MusicCrossfadeLayer");
        crossfadeLayer.transform.SetParent(transform);
        sources[1] = CreateSource(crossfadeLayer);
    }

    private static AudioSource CreateSource(GameObject owner)
    {
        var source = owner.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.priority = 0;
        return source;
    }

    private void Start()
    {
        if (Instance != this) return;
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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Instance != this) return;

        if (inLevel)
        {
            if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
                PlayTrack(ClipForLevel(currentLevel, currentLevelIndex, waveManager.CurrentWave));
        }

        UpdateFades(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;

        // La pantalla de carga no cambia la música: suena la anterior hasta que llegue la escena siguiente.
        if (scene.name == "LoadingScreen") return;

        currentLevel = LevelManager.ForScene(scene.name);
        inLevel = currentLevel != null;
        currentLevelIndex = inLevel ? System.Array.IndexOf(LevelManager.Levels, currentLevel) : -1;
        waveManager = inLevel ? FindFirstObjectByType<WaveManager>() : null;

        if (!inLevel)
            PlayTrack(menuMusic);
        // En un nivel, Update elige la pista en cuanto exista el WaveManager.
    }

    // Qué pista toca en la oleada 'wave' de un nivel. Sin jefe (Sector7), siempre la de zona.
    public static LevelTrack TrackForWave(LevelData level, int wave)
    {
        if (level == null || level.bossPrefab == null || level.bossWave <= 0) return LevelTrack.Zone;
        if (wave == level.bossWave) return LevelTrack.Boss;
        if (wave >= level.bossWave - 2 && wave < level.bossWave) return LevelTrack.Crescendo;
        return LevelTrack.Zone;
    }

    // Crescendo A en los niveles de posición par (0, 2, 4) y B en los impares.
    public static bool UsesCrescendoA(int levelIndex) => levelIndex % 2 == 0;

    public AudioClip ClipForLevel(LevelData level, int levelIndex, int wave)
    {
        switch (TrackForWave(level, wave))
        {
            case LevelTrack.Boss:
                return level.bossMusic != null ? level.bossMusic : gameMusicIntense;
            case LevelTrack.Crescendo:
                AudioClip crescendo = UsesCrescendoA(levelIndex) ? crescendoMusicA : crescendoMusicB;
                return crescendo != null ? crescendo : gameMusicIntense;
            default:
                return level != null && level.zoneMusic != null ? level.zoneMusic : gameMusic;
        }
    }

    // Pasa a 'clip' con un fundido. Si ya es la pista que suena, no hace nada; si es la que se estaba
    // apagando, vuelve a subir desde donde iba sin empezar de nuevo.
    private void PlayTrack(AudioClip clip)
    {
        if (clip == null || sources[active].clip == clip) return;

        int other = 1 - active;
        if (sources[other].clip != clip || !sources[other].isPlaying)
        {
            sources[other].Stop();
            sources[other].clip = clip;
            sources[other].time = clip == gameMusicIntense ? Mathf.Clamp(gameMusicIntenseStartOffset, 0f, clip.length - 0.01f) : 0f;
            gains[other] = 0f;
            sources[other].Play();
        }
        active = other;
    }

    private void UpdateFades(float dt)
    {
        float step = crossfadeDuration > 0f ? dt / crossfadeDuration : 1f;
        float level = IsMuted ? 0f : volume;

        for (int i = 0; i < 2; i++)
        {
            gains[i] = Mathf.MoveTowards(gains[i], i == active ? 1f : 0f, step);
            sources[i].volume = level * gains[i];
            sources[i].mute = Silenced;

            if (i != active && gains[i] <= 0f && sources[i].isPlaying)
                sources[i].Stop();
        }
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        PlayerPrefs.SetInt(MutePrefKey, muted ? 1 : 0);
    }
}
