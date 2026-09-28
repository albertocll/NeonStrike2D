using UnityEngine;

public class BackButton : MonoBehaviour
{
    public void GoBack()
    {
        SceneLoader.Load("MainMenu");
    }
}