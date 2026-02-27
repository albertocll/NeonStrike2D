using UnityEngine;

public class ExitController : MonoBehaviour
{
    [Header("Exit Panel")]
    public GameObject exitPanel;

    public void OpenExitPanel()
    {
        exitPanel.SetActive(true);
    }

    public void CloseExitPanel()
    {
        exitPanel.SetActive(false);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
