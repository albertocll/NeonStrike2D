using System.Threading.Tasks;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelRegister;

    // Restauración de la sesión recordada (issue #216). Se espera antes de abrir el login
    // para no pedir la contraseña ni abrir un segundo WebSocket si la sesión ya se está restaurando.
    private Task _restoreSession = Task.CompletedTask;

    private void Start()
    {
        if (panelLogin != null) panelLogin.SetActive(false);
        if (panelRegister != null) panelRegister.SetActive(false);

        LoginUI.DeleteLegacyPassword();
        _restoreSession = LoginUI.TryRestoreSessionAsync();
    }

    private static bool HasSession =>
        NetworkManager.Instance != null && !string.IsNullOrEmpty(NetworkManager.Instance.Token);

    public void Play()
    {
        AbrirLogin();
    }

    // Con una sesión válida (iniciada o restaurada) va directo a CharacterSelect; si no, abre el panel de login.
    public async void AbrirLogin()
    {
        if (!_restoreSession.IsCompleted)
            await _restoreSession;
        if (this == null) return;

        if (HasSession)
        {
            CerrarPaneles();
            SceneLoader.Load("CharacterSelect");
            return;
        }

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
