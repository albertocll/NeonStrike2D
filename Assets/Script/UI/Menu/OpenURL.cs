using UnityEngine;

public class OpenURL : MonoBehaviour
{
    [SerializeField] private string url = "https://neonstrike-game.netlify.app/";
    
    public void OpenLink()
    {
        Application.OpenURL(url);
    }
}