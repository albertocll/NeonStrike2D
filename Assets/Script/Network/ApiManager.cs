using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance { get; private set; }

    [SerializeField] private string serverUrl = "http://192.168.1.26:5036";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // LOGIN
    public async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var body = JsonUtility.ToJson(new LoginRequest { email = email, password = password });
        var result = await PostAsync("/login", body);
        return JsonUtility.FromJson<LoginResponse>(result);
    }

    // REGISTRO
    public async Task<RegisterResponse> RegisterAsync(string username, string email, string password)
    {
        var body = JsonUtility.ToJson(new RegisterRequest 
        { 
            username = username, 
            email = email, 
            password = password 
        });
        var result = await PostAsync("/register", body);
        return JsonUtility.FromJson<RegisterResponse>(result);
    }

    // RANKING
    public async Task<string> GetRankingAsync()
    {
        return await GetAsync("/ranking");
    }

    // HTTP POST
    private async Task<string> PostAsync(string endpoint, string json)
    {
        var request = new UnityWebRequest($"{serverUrl}{endpoint}", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error en {endpoint}: {request.error}");
            return null;
        }

        return request.downloadHandler.text;
    }

    // HTTP GET
    private async Task<string> GetAsync(string endpoint, string token = null)
    {
        var request = UnityWebRequest.Get($"{serverUrl}{endpoint}");
        if (token != null)
            request.SetRequestHeader("Authorization", $"Bearer {token}");

        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error en {endpoint}: {request.error}");
            return null;
        }

        return request.downloadHandler.text;
    }
}

// DTOs
[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public bool success;
    public string message;
    public int userId;
    public string username;
    public string email;
    public string token;
}

[Serializable]
public class RegisterRequest
{
    public string username;
    public string email;
    public string password;
}

[Serializable]
public class RegisterResponse
{
    public bool success;
    public string message;
}