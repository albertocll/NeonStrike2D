using UnityEngine;

// Salir del juego desde el panel de salir. Abrir y cerrar el panel lo hace MenuManager.
public class ExitController : MonoBehaviour
{
    [Header("Exit Panel")]
    public GameObject exitPanel;

    public void ExitGame()
    {
        Application.Quit();
    }
}
