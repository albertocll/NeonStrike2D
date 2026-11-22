using System.Threading.Tasks;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    // Único sitio que abre y cierra los paneles del menú: al abrir uno se ocultan los demás y el
    // contenido principal; al cerrarlo vuelve el contenido principal.
    [Header("Panels")]
    [SerializeField] private GameObject mainContent;
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelRegister;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject exitPanel;

    // Restauración de la sesión recordada (issue #216). Se espera antes de abrir el login
    // para no pedir la contraseña ni abrir un segundo WebSocket si la sesión ya se está restaurando.
    private Task _restoreSession = Task.CompletedTask;

    private void Start()
    {
        ShowOnly(null);

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

        ShowOnly(panelLogin);
    }

    public void AbrirRegister() => ShowOnly(panelRegister);

    public void AbrirOpciones() => ShowOnly(optionsPanel);

    public void AbrirSalir() => ShowOnly(exitPanel);

    public void CerrarPaneles() => ShowOnly(null);

    // Deja abierto solo el panel indicado; con null, ninguno y vuelve el contenido principal.
    private void ShowOnly(GameObject panel)
    {
        foreach (var p in new[] { panelLogin, panelRegister, optionsPanel, exitPanel })
            if (p != null) p.SetActive(p == panel);
        if (mainContent != null) mainContent.SetActive(panel == null);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
