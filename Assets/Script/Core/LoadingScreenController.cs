using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        string targetScene = SceneLoader.TargetScene;
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[LoadingScreenController] SceneLoader.TargetScene vacio, cargando MainMenu como fallback.");
            targetScene = "MainMenu";
        }

        var operation = SceneManager.LoadSceneAsync(targetScene);
        if (operation == null)
        {
            Debug.LogWarning($"[LoadingScreenController] LoadSceneAsync no pudo iniciar la carga de '{targetScene}'.");
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
            yield return null;

        operation.allowSceneActivation = true;
    }
}
