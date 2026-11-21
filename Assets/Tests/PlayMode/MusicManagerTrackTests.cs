using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// #262: pistas de sustitución mientras faltan archivos y cambio de pista sin reiniciar la que se apagaba.
public class MusicManagerTrackTests
{
    private MusicManager music;
    private LevelData level;
    private GameObject boss;
    private AudioClip gameClip;
    private AudioClip intenseClip;

    private static PropertyInfo InstanceProperty =>
        typeof(MusicManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

    private void SetField(string name, object value) =>
        typeof(MusicManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(music, value);

    private void PlayTrack(AudioClip clip) =>
        typeof(MusicManager).GetMethod("PlayTrack", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(music, new object[] { clip });

    [SetUp]
    public void SetUp()
    {
        InstanceProperty.GetSetMethod(true).Invoke(null, new object[] { null });

        music = new GameObject("MusicManagerUnderTest").AddComponent<MusicManager>();
        gameClip = AudioClip.Create("Game", 44100, 1, 44100, false);
        intenseClip = AudioClip.Create("Intense", 44100, 1, 44100, false);
        SetField("gameMusic", gameClip);
        SetField("gameMusicIntense", intenseClip);

        boss = new GameObject("BossForTest");
        level = ScriptableObject.CreateInstance<LevelData>();
        level.firstWave = 1;
        level.bossWave = 10;
        level.bossPrefab = boss;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(music.gameObject);
        Object.DestroyImmediate(level);
        Object.DestroyImmediate(boss);
        Object.DestroyImmediate(gameClip);
        Object.DestroyImmediate(intenseClip);
        InstanceProperty.GetSetMethod(true).Invoke(null, new object[] { null });
    }

    [Test]
    public void ClipForLevel_WithoutNewTracks_UsesCurrentGameAndIntenseMusic()
    {
        Assert.AreSame(gameClip, music.ClipForLevel(level, 0, 5));
        Assert.AreSame(intenseClip, music.ClipForLevel(level, 0, 8));
        Assert.AreSame(intenseClip, music.ClipForLevel(level, 1, 9));
        Assert.AreSame(intenseClip, music.ClipForLevel(level, 0, 10));
    }

    [Test]
    public void ClipForLevel_WithNewTracks_UsesThem()
    {
        var zone = AudioClip.Create("Zone", 44100, 1, 44100, false);
        var bossClip = AudioClip.Create("Boss", 44100, 1, 44100, false);
        var crescendoA = AudioClip.Create("CrescendoA", 44100, 1, 44100, false);
        var crescendoB = AudioClip.Create("CrescendoB", 44100, 1, 44100, false);
        level.zoneMusic = zone;
        level.bossMusic = bossClip;
        SetField("crescendoMusicA", crescendoA);
        SetField("crescendoMusicB", crescendoB);

        Assert.AreSame(zone, music.ClipForLevel(level, 0, 5));
        Assert.AreSame(crescendoA, music.ClipForLevel(level, 0, 8));
        Assert.AreSame(crescendoB, music.ClipForLevel(level, 1, 9));
        Assert.AreSame(bossClip, music.ClipForLevel(level, 0, 10));

        Object.DestroyImmediate(zone);
        Object.DestroyImmediate(bossClip);
        Object.DestroyImmediate(crescendoA);
        Object.DestroyImmediate(crescendoB);
    }

    [Test]
    public void PlayTrack_SameClip_KeepsSource_AndBackToFadingClip_DoesNotRestartIt()
    {
        PlayTrack(gameClip);
        Assert.AreSame(gameClip, music.CurrentClip);

        PlayTrack(gameClip);
        Assert.AreSame(gameClip, music.CurrentClip);

        PlayTrack(intenseClip);
        Assert.AreSame(intenseClip, music.CurrentClip);

        // Volver a la pista que se estaba apagando reutiliza su AudioSource: no se intercambian clips.
        var sources = music.GetComponentsInChildren<AudioSource>();
        AudioSource gameSource = System.Array.Find(sources, s => s.clip == gameClip);
        Assert.IsNotNull(gameSource);
        PlayTrack(gameClip);
        Assert.AreSame(gameClip, music.CurrentClip);
        Assert.AreSame(gameClip, gameSource.clip);
    }
}
