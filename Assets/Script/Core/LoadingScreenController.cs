using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    [Tooltip("Tiempo minimo (segundos) que se muestra la pantalla de carga aunque la carga real sea mas rapida.")]
    [SerializeField] private float minDisplayTime = 1f;

    [Tooltip("Paneles de leyenda (CollectiblesLegend, PowerUpsLegend, DashLegend). En cada carga se muestra uno al azar, fijo hasta que termina.")]
    [SerializeField] private GameObject[] legendPanels;

    private void Start()
    {
        ShowRandomLegendPanel();
        StartCoroutine(LoadTargetScene());
    }

    private void ShowRandomLegendPanel()
    {
        if (legendPanels == null || legendPanels.Length == 0) return;

        int chosen = Random.Range(0, legendPanels.Length);
        for (int i = 0; i < legendPanels.Length; i++)
            if (legendPanels[i] != null) legendPanels[i].SetActive(i == chosen);
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
