using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    private const string LoadingSceneName = "LoadingScreen";

    public static string TargetScene { get; private set; }

    public static void Load(string sceneName)
    {
        TargetScene = sceneName;
        Time.timeScale = 1f;
        SceneManager.LoadScene(LoadingSceneName);
    }
}
