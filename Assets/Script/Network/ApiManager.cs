using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    public static ApiManager Instance { get; private set; }

    [SerializeField] private string serverUrl = "https://neonstrike2d-production.up.railway.app";

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

    // GUARDAR RESULTADO
    public async Task SaveMatchResultAsync(int userId, int bestWave)
    {
        var body = JsonUtility.ToJson(new MatchResultRequest
        {
            userId = userId,
            bestWave = bestWave
        });
        await PostAsync("/match/result", body, NetworkManager.Instance.Token);
    }

    // AMIGOS
    public async Task<string> SendFriendRequestAsync(string username)
    {
        return await PostAsync($"/friends/request/{username}", "{}", NetworkManager.Instance.Token);
    }

    public async Task<string> AcceptFriendRequestAsync(int requesterId)
    {
        return await PostAsync($"/friends/accept/{requesterId}", "{}", NetworkManager.Instance.Token);
    }

    public async Task<string> GetOnlineFriendsAsync()
    {
        return await GetAsync("/friends/online", NetworkManager.Instance.Token);
    }

    // HTTP POST
    private async Task<string> PostAsync(string endpoint, string json, string token = null)
    {
        var request = new UnityWebRequest($"{serverUrl}{endpoint}", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        if (token != null)
            request.SetRequestHeader("Authorization", $"Bearer {token}");

        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError($"Error de conexión en {endpoint}: {request.error}");
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

    // USUARIOS
    public async Task<int?> GetUserIdByUsernameAsync(string username)
    {
        var result = await GetAsync($"/users/{username}", NetworkManager.Instance.Token);
        if (string.IsNullOrEmpty(result)) return null;
        var response = JsonUtility.FromJson<UserResponse>(result);
        return response?.userId;
    }
}

// DTOs
[Serializable] public class LoginRequest { public string email; public string password; }
[Serializable] public class LoginResponse { public bool success; public string message; public int userId; public string username; public string email; public string token; }
[Serializable] public class RegisterRequest { public string username; public string email; public string password; }
[Serializable] public class RegisterResponse { public bool success; public string message; }
[Serializable] public class RankingEntry { public int userId; public string username; public int bestWave; }
[Serializable] public class RankingList { public RankingEntry[] items; }
[Serializable] public class MatchResultRequest { public int userId; public int bestWave; }
[Serializable] public class FriendRequestResponse { public bool success; public string message; }
[Serializable] public class OnlineFriendsResponse { public string[] items; }
[Serializable] public class UserResponse { public int userId; public string username; }