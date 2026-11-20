using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ventana para elegir nombre la primera vez que alguien entra con Google (issue #75).
// Sale con el nombre propuesto por el servidor ya escrito; al confirmar lo cambia con
// PUT /me/username y actualiza el token y el WebSocket.
public class ChooseUsernamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private Button buttonConfirm;
    [SerializeField] private TMP_Text textFeedback;

    private const int UsernameMaxLength = 16;

    private TaskCompletionSource<LoginResponse> tcs;
    private LoginResponse current;

    private bool initialized;

    // Se inicializa al mostrarse y no en Awake: el panel empieza desactivado en la escena.
    private void Init()
    {
        if (initialized) return;
        initialized = true;
        inputUsername.characterLimit = UsernameMaxLength;
        inputUsername.onValidateInput = (text, index, c) =>
            (c < 128 && (char.IsLetterOrDigit(c) || c == '_')) ? c : '\0';
        buttonConfirm.onClick.AddListener(OnConfirmClicked);
    }

    // Muestra la ventana y termina cuando el nombre queda guardado. Devuelve la respuesta con el
    // token vigente (el nuevo si se cambió el nombre, el de entrada si se dejó el propuesto).
    public Task<LoginResponse> ShowAsync(LoginResponse login)
    {
        Init();
        current = login;
        tcs = new TaskCompletionSource<LoginResponse>();
        inputUsername.text = login.username;
        textFeedback.text = "";
        buttonConfirm.interactable = true;
        gameObject.SetActive(true);
        return tcs.Task;
    }

    private async void OnConfirmClicked()
    {
        string username = inputUsername.text.Trim();

        if (username == current.username)
        {
            Close(current);
            return;
        }

        if (!LoginUI.IsValidUsername(username))
        {
            textFeedback.text = "El nombre debe tener entre 3 y 16 caracteres: letras, números o _.";
            return;
        }

        buttonConfirm.interactable = false;
        textFeedback.text = "Guardando...";

        var response = await ApiManager.Instance.ChangeUsernameAsync(username);
        if (this == null) return;

        if (response != null && response.success)
        {
            await NetworkManager.Instance.ApplyUsernameChangeAsync(response.username, response.token);
            GameData.Username = response.username;
            Close(response);
        }
        else
        {
            textFeedback.text = response?.message ?? "Error al conectar.";
            buttonConfirm.interactable = true;
        }
    }

    private void Close(LoginResponse result)
    {
        gameObject.SetActive(false);
        tcs?.TrySetResult(result);
    }
}
