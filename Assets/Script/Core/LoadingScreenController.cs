using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    [Tooltip("Tiempo minimo (segundos) que se muestra la pantalla de carga, para dar tiempo a leer el tip aunque la carga real sea mas rapida.")]
    [SerializeField] private float minDisplayTime = 3f;

    [Tooltip("Paneles de leyenda a rotar durante la carga (CollectiblesLegend, PowerUpsLegend, DashLegend), en bucle mientras dure la pantalla.")]
    [SerializeField] private GameObject[] legendPanels;

    [Tooltip("Segundos que se muestra cada panel antes de pasar al siguiente.")]
    [SerializeField] private float timePerPanel = 3f;

    private void Start()
    {
        StartCoroutine(LoadTargetScene());

        if (legendPanels != null && legendPanels.Length > 0)
            StartCoroutine(RotateLegendPanels());
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

    private IEnumerator RotateLegendPanels()
    {
        // timePerPanel es independiente de minDisplayTime; si la carga real
        // tarda mas, el bucle sigue rotando hasta que la escena cambie
        // (este objeto se destruye y la corutina se detiene sola).
        float interval = Mathf.Max(timePerPanel, 0.1f);

        int current = 0;
        for (int i = 0; i < legendPanels.Length; i++)
            if (legendPanels[i] != null) legendPanels[i].SetActive(i == current);

        while (true)
        {
            yield return new WaitForSeconds(interval);

            if (legendPanels[current] != null) legendPanels[current].SetActive(false);
            current = (current + 1) % legendPanels.Length;
            if (legendPanels[current] != null) legendPanels[current].SetActive(true);
        }
    }
}
