using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelRegister;

    private void Start()
    {
        if (panelLogin != null) panelLogin.SetActive(false);
        if (panelRegister != null) panelRegister.SetActive(false);
    }

    public void Play()
    {
        if (NetworkManager.Instance == null || string.IsNullOrEmpty(NetworkManager.Instance.Token))
        {
            AbrirLogin();
            return;
        }
        SceneManager.LoadScene("CharacterSelect");
    }

    public void AbrirLogin()
    {
        if (panelLogin != null) panelLogin.SetActive(true);
        if (panelRegister != null) panelRegister.SetActive(false);
    }

    public void AbrirRegister()
    {
        if (panelRegister != null) panelRegister.SetActive(true);
        if (panelLogin != null) panelLogin.SetActive(false);
    }

    public void CerrarPaneles()
    {
        if (panelLogin != null) panelLogin.SetActive(false);
        if (panelRegister != null) panelRegister.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
    }
}