using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("CharacterSelect");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
