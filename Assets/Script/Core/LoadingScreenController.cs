using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    [Tooltip("Tiempo minimo (segundos) que se muestra la pantalla de carga, para dar tiempo a leer el tip aunque la carga real sea mas rapida.")]
    [SerializeField] private float minDisplayTime = 3f;

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        float startTime = Time.time;

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

        // Esperar al mayor entre el tiempo real de carga y el minimo configurado,
        // sin bloquear el hilo (la carga real ya termino, solo retrasamos la activacion).
        float remaining = minDisplayTime - (Time.time - startTime);
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        operation.allowSceneActivation = true;
    }
}
