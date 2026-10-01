using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [SerializeField] private AudioClip playerShoot;
    [SerializeField] private AudioClip enemyHit;
    [SerializeField] private AudioClip enemyDeath;
    [SerializeField] private AudioClip collectibleScore;
    [SerializeField] private AudioClip collectibleHealth;
    [SerializeField] private AudioClip playerDamage;
    [SerializeField] private AudioClip playerDeath;
    [SerializeField] private AudioClip waveStart;
    [SerializeField] private AudioClip waveBonus;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool muted = false;

    private const string VolumePrefKey = "SfxVolume";
    private const string MutedPrefKey = "SfxMuted";

    // Prioridad de AudioSource (0 = mas importante para Unity, 256 = menos; la
    // musica usa 0, ver MusicManager). Los SFX frecuentes pero menos criticos
    // ceden prioridad frente a los infrecuentes pero mas relevantes, para que
    // estos sigan sonando cuando se supera el limite de voces reales del
    // proyecto con una rafaga grande (#169).
    private const int FrequentPriority = 160;
    private const int RelevantPriority = 80;
    private const int DefaultPriority = 128;

    // Evita que un mismo sonido repetido sature las voces reales disponibles
    // (p.ej. una oleada entera muriendo a la vez). Por encima de este limite,
    // el PlayOneShot de ese instante simplemente se ignora.
    private const int MaxSimultaneousPerClip = 5;

    private AudioSource audioSource;
    private readonly Dictionary<AudioClip, int> activeInstances = new Dictionary<AudioClip, int>();

    public float Volume => volume;
    public bool Muted => muted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        volume = PlayerPrefs.GetFloat(VolumePrefKey, volume);
        muted = PlayerPrefs.GetInt(MutedPrefKey, muted ? 1 : 0) == 1;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.mute = muted;
    }

    public void SetVolume(float value)
    {
        volume = value;
        PlayerPrefs.SetFloat(VolumePrefKey, value);
    }

    public void SetMuted(bool value)
    {
        muted = value;
        audioSource.mute = value;
        PlayerPrefs.SetInt(MutedPrefKey, value ? 1 : 0);
    }

    public void PlayPlayerShoot() => Play(playerShoot, FrequentPriority);
    public void PlayEnemyHit() => Play(enemyHit, FrequentPriority);
    public void PlayEnemyDeath() => Play(enemyDeath, RelevantPriority);
    public void PlayCollectibleScore() => Play(collectibleScore, DefaultPriority);
    public void PlayCollectibleHealth() => Play(collectibleHealth, DefaultPriority);
    public void PlayPlayerDamage() => Play(playerDamage, RelevantPriority);
    public void PlayPlayerDeath() => Play(playerDeath, RelevantPriority);
    public void PlayWaveStart() => Play(waveStart, RelevantPriority);
    public void PlayWaveBonus() => Play(waveBonus, RelevantPriority);

    private void Play(AudioClip clip, int priority)
    {
        if (clip == null) return;

        int active = activeInstances.TryGetValue(clip, out var count) ? count : 0;
        if (active >= MaxSimultaneousPerClip) return;

        activeInstances[clip] = active + 1;
        audioSource.priority = priority;
        audioSource.PlayOneShot(clip, volume);
        StartCoroutine(ReleaseInstanceAfter(clip, clip.length));
    }

    private IEnumerator ReleaseInstanceAfter(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (activeInstances.TryGetValue(clip, out var count))
            activeInstances[clip] = Mathf.Max(0, count - 1);
    }
}
