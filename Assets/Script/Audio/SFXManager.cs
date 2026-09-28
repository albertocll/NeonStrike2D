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

    private AudioSource audioSource;

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

    public void PlayPlayerShoot() => Play(playerShoot);
    public void PlayEnemyHit() => Play(enemyHit);
    public void PlayEnemyDeath() => Play(enemyDeath);
    public void PlayCollectibleScore() => Play(collectibleScore);
    public void PlayCollectibleHealth() => Play(collectibleHealth);
    public void PlayPlayerDamage() => Play(playerDamage);
    public void PlayPlayerDeath() => Play(playerDeath);
    public void PlayWaveStart() => Play(waveStart);
    public void PlayWaveBonus() => Play(waveBonus);

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}
