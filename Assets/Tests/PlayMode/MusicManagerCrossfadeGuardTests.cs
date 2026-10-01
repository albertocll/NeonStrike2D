using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// #148: un segundo disparo del crossfade mientras ya hay uno en marcha no debe
// lanzar una segunda corutina compitiendo por las mismas AudioSource.
public class MusicManagerCrossfadeGuardTests
{
    private MusicManager music;

    private static PropertyInfo InstanceProperty =>
        typeof(MusicManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

    [SetUp]
    public void SetUp()
    {
        // Limpia cualquier singleton que quedara de un test anterior antes de crear el nuevo.
        InstanceProperty.GetSetMethod(true).Invoke(null, new object[] { null });

        var go = new GameObject("MusicManagerUnderTest");
        music = go.AddComponent<MusicManager>();

        var dummyClip = AudioClip.Create("DummyIntense", 1, 1, 44100, false);
        typeof(MusicManager)
            .GetField("gameMusicIntense", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(music, dummyClip);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(music.gameObject);
        InstanceProperty.GetSetMethod(true).Invoke(null, new object[] { null });
    }

    [Test]
    public void StartIntenseCrossfade_SecondCallWhileActive_DoesNotStartANewCoroutine()
    {
        var startMethod = typeof(MusicManager).GetMethod("StartIntenseCrossfade", BindingFlags.NonPublic | BindingFlags.Instance);
        var routineField = typeof(MusicManager).GetField("crossfadeRoutine", BindingFlags.NonPublic | BindingFlags.Instance);

        startMethod.Invoke(music, null);
        var firstRoutine = routineField.GetValue(music);
        Assert.IsNotNull(firstRoutine, "El primer disparo deberia arrancar la corutina de crossfade.");

        startMethod.Invoke(music, null);
        var secondRoutine = routineField.GetValue(music);

        Assert.AreSame(firstRoutine, secondRoutine,
            "Un segundo disparo mientras el crossfade esta activo no deberia reemplazar la corutina (guard de reentrada, #148).");
    }
}
