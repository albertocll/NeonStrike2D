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
        var operation = SceneManager.LoadSceneAsync(SceneLoader.TargetScene);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
            yield return null;

        operation.allowSceneActivation = true;
    }
}
