using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private float volume = 0.5f;

    private const string MutePrefKey = "MusicMuted";

    private AudioSource audioSource;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Level1")
            PlayClip(gameMusic);
        else
            PlayClip(menuMusic);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource.clip == clip) return;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        PlayerPrefs.SetInt(MutePrefKey, muted ? 1 : 0);
        audioSource.volume = muted ? 0f : volume;
    }
}