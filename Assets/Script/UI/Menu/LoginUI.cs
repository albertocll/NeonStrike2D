using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("Panel Login")]
    [SerializeField] private TMP_InputField inputEmail;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private Button buttonLogin;
    [SerializeField] private Button buttonGoToRegister;
    [SerializeField] private Button buttonClose;

    [Header("Panel Register")]
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputEmailReg;
    [SerializeField] private TMP_InputField inputPasswordReg;
    [SerializeField] private Button buttonRegister;
    [SerializeField] private Button buttonGoToLogin;
    [SerializeField] private Button buttonCloseReg;

    [Header("Feedback")]
    [SerializeField] private TMP_Text textFeedbackLogin;
    [SerializeField] private TMP_Text textFeedbackRegister;

    [Header("Refs")]
    [SerializeField] private MenuManager menuManager;

    [Header("Recordar cuenta")]
    [SerializeField] private Toggle toggleRememberMe;

    [Header("Invitado")]
    [SerializeField] private Button buttonGuest;

    private const string KEY_EMAIL = "remembered_email";
    private const string KEY_TOKEN = "remembered_token";
    private const string KEY_REMEMBER = "remember_me";

    // Clave de versiones anteriores que guardaba la contraseña en texto plano (issue #216).
    private const string KEY_LEGACY_PASSWORD = "remembered_password";

    private void Start()
    {
        buttonLogin.onClick.AddListener(OnLoginClicked);
        buttonGoToRegister.onClick.AddListener(() => menuManager.AbrirRegister());
        buttonClose.onClick.AddListener(() => menuManager.CerrarPaneles());
        buttonGuest.onClick.AddListener(OnGuestClicked);

        buttonRegister.onClick.AddListener(OnRegisterClicked);
        buttonGoToLogin.onClick.AddListener(() => menuManager.AbrirLogin());
        buttonCloseReg.onClick.AddListener(() => menuManager.CerrarPaneles());

        if (PlayerPrefs.GetInt(KEY_REMEMBER, 0) == 1)
        {
            inputEmail.text = PlayerPrefs.GetString(KEY_EMAIL, "");
            toggleRememberMe.isOn = true;
        }
    }

    // Borra la contraseña que hubieran dejado guardada versiones anteriores del juego.
    public static void DeleteLegacyPassword()
    {
        if (!PlayerPrefs.HasKey(KEY_LEGACY_PASSWORD)) return;
        PlayerPrefs.DeleteKey(KEY_LEGACY_PASSWORD);
        PlayerPrefs.Save();
    }

    // Si hay una sesión recordada, valida el token contra /me y la restaura sin pedir contraseña.
    // El JWT caduca a las 2 horas: pasado ese tiempo /me lo rechaza y hay que volver a iniciar sesión.
    public static async Task TryRestoreSessionAsync()
    {
        if (NetworkManager.Instance == null || ApiManager.Instance == null) return;
        if (!string.IsNullOrEmpty(NetworkManager.Instance.Token)) return;
        if (PlayerPrefs.GetInt(KEY_REMEMBER, 0) != 1) return;

        string token = PlayerPrefs.GetString(KEY_TOKEN, "");
        if (string.IsNullOrEmpty(token)) return;

        var me = await ApiManager.Instance.GetMeAsync(token);
        if (me == null || !int.TryParse(me.userId, out int userId))
        {
            PlayerPrefs.DeleteKey(KEY_TOKEN);
            PlayerPrefs.Save();
            return;
        }

        // Durante la espera el jugador pudo entrar como invitado o iniciar sesión a mano.
        if (NetworkManager.Instance.IsGuest || !string.IsNullOrEmpty(NetworkManager.Instance.Token)) return;

        NetworkManager.Instance.SetUserData(userId, me.username, token);
        GameData.Username = me.username;

        try
        {
            await NetworkManager.Instance.ConnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoginUI] Sesión restaurada pero no se pudo conectar el WebSocket: {e.Message}");
        }
    }

    public async void OnLoginClicked()
    {
        string email = inputEmail.text.Trim();
        string password = inputPassword.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            textFeedbackLogin.text = "Rellena todos los campos.";
            return;
        }

        textFeedbackLogin.text = "Conectando...";
        buttonLogin.interactable = false;

        try
        {
            var response = await ApiManager.Instance.LoginAsync(email, password);

            if (response != null && response.success)
            {
                SaveRememberedSession(email, response.token);
                NetworkManager.Instance.SetUserData(response.userId, response.username, response.token);
                GameData.Username = response.username;
                await NetworkManager.Instance.ConnectAsync();
                textFeedbackLogin.text = $"Bienvenido, {response.username}!";
                await Task.Delay(1000);
                menuManager.CerrarPaneles();
                SceneLoader.Load("CharacterSelect");
            }
            else
            {
                textFeedbackLogin.text = response?.message ?? "Error al conectar.";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginUI] Error en login: {e}");
            string errorType = e.GetType().Name;
            string inner = e.InnerException != null ? $" | Inner: {e.InnerException.GetType().Name}: {e.InnerException.Message}" : "";
            string fullError = $"[{errorType}] {e.Message}{inner}";
            Debug.LogError(fullError);
            textFeedbackLogin.text = fullError.Length > 200 ? fullError.Substring(0, 200) : fullError;
        }
        finally
        {
            if (this != null && buttonLogin != null)
                buttonLogin.interactable = true;
        }
    }

    // Recuerda el email y el token de sesión; la contraseña nunca se guarda.
    private void SaveRememberedSession(string email, string token)
    {
        if (toggleRememberMe.isOn)
        {
            PlayerPrefs.SetString(KEY_EMAIL, email);
            PlayerPrefs.SetString(KEY_TOKEN, token);
            PlayerPrefs.SetInt(KEY_REMEMBER, 1);
        }
        else
        {
            PlayerPrefs.DeleteKey(KEY_EMAIL);
            PlayerPrefs.DeleteKey(KEY_TOKEN);
            PlayerPrefs.SetInt(KEY_REMEMBER, 0);
        }
        PlayerPrefs.Save();
    }

    public async void OnRegisterClicked()
    {
        string username = inputUsername.text.Trim();
        string email = inputEmailReg.text.Trim();
        string password = inputPasswordReg.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            textFeedbackRegister.text = "Rellena todos los campos.";
            return;
        }

        textFeedbackRegister.text = "Registrando...";
        buttonRegister.interactable = false;

        try
        {
            var response = await ApiManager.Instance.RegisterAsync(username, email, password);

            if (response != null && response.success)
            {
                textFeedbackRegister.text = "Registro correcto. Ahora inicia sesión.";
                menuManager.AbrirLogin();
            }
            else
            {
                textFeedbackRegister.text = response?.message ?? "Error al registrar.";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginUI] Error en registro: {e}");
            string errorType = e.GetType().Name;
            string inner = e.InnerException != null ? $" | Inner: {e.InnerException.GetType().Name}: {e.InnerException.Message}" : "";
            textFeedbackRegister.text = $"[{errorType}] {e.Message}{inner}";
        }
        finally
        {
            buttonRegister.interactable = true;
        }
    }

    public void OnGuestClicked()
    {
        NetworkManager.Instance.SetGuestData();
        GameData.Username = "Invitado";
        SceneLoader.Load("CharacterSelect");
    }
}
