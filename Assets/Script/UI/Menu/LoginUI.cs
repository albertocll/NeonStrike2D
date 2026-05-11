using System;
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
    private const string KEY_PASSWORD = "remembered_password";
    private const string KEY_REMEMBER = "remember_me";

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
            inputPassword.text = PlayerPrefs.GetString(KEY_PASSWORD, "");
            toggleRememberMe.isOn = true;
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

        if (toggleRememberMe.isOn)
        {
            PlayerPrefs.SetString(KEY_EMAIL, email);
            PlayerPrefs.SetString(KEY_PASSWORD, password);
            PlayerPrefs.SetInt(KEY_REMEMBER, 1);
        }
        else
        {
            PlayerPrefs.DeleteKey(KEY_EMAIL);
            PlayerPrefs.DeleteKey(KEY_PASSWORD);
            PlayerPrefs.SetInt(KEY_REMEMBER, 0);
        }
        PlayerPrefs.Save();

        try
        {
            var response = await ApiManager.Instance.LoginAsync(email, password);

            if (response != null && response.success)
            {
                NetworkManager.Instance.SetUserData(response.userId, response.username, response.token);
                GameData.Username = response.username;
                await NetworkManager.Instance.ConnectAsync();
                textFeedbackLogin.text = $"Bienvenido, {response.username}!";
                await System.Threading.Tasks.Task.Delay(1000);
                menuManager.CerrarPaneles();
                UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
            }
            else
            {
                textFeedbackLogin.text = response?.message ?? "Error al conectar.";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginUI] Error en login: {e.Message}");
            textFeedbackLogin.text = "Error de conexión.";
        }
        finally
        {
            if (this != null && buttonRegister != null)
                buttonRegister.interactable = true;
        }
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
            Debug.LogError($"[LoginUI] Error en registro: {e.Message}");
            textFeedbackRegister.text = "Error de conexión.";
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
    }
}