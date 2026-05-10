using UnityEngine;
using TMPro;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private CharacterData[] characters;
    [SerializeField] private Transform spawnPointLocal;
    [SerializeField] private Transform spawnPointRemote;
    [SerializeField] private string roomId;

    private GameObject _remotePlayer;

    void Awake()
    {
        roomId = GameData.RoomId;
        SpawnLocalPlayer();

        if (!string.IsNullOrEmpty(GameData.RemoteCharacter))
            SpawnRemotePlayer(GameData.RemoteCharacter);

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnReceiveGameState += OnReceiveGameState;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnReceiveGameState -= OnReceiveGameState;
            NetworkManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
        }
    }

    private void SpawnLocalPlayer()
    {
        string selected = GameData.SelectedCharacter;
        Vector3 spawnPos = spawnPointLocal != null ? spawnPointLocal.position : transform.position;

        foreach (var data in characters)
        {
            if (data.characterName == selected)
            {
                GameObject player = Instantiate(data.prefab, spawnPos, Quaternion.identity);
                player.transform.localScale = data.scale;

                var health = player.GetComponent<PlayerHealth>();
                if (health) health.Init(data.maxHealth);

                var controller = player.GetComponent<PlayerController>();
                if (controller) controller.Init(data.speed, data.damage);

                var anim = player.GetComponentInChildren<Animator>();
                if (anim && data.animatorController)
                    anim.runtimeAnimatorController = data.animatorController;

                var sync = player.AddComponent<PlayerSync>();
                sync.Init(roomId, true);

                var nameText = player.GetComponentInChildren<TMP_Text>();
                if (nameText != null) nameText.text = GameData.Username;

                return;
            }
        }
    }

    private void SpawnRemotePlayer(string character)
    {
        Debug.Log($"[PlayerSpawner] SpawnRemotePlayer: {character}");
        Vector3 spawnPos = spawnPointRemote != null ? spawnPointRemote.position : transform.position + Vector3.right * 3f;

        foreach (var data in characters)
        {
            if (data.characterName == character)
            {
                _remotePlayer = Instantiate(data.prefab, spawnPos, Quaternion.identity);
                _remotePlayer.transform.localScale = data.scale;

                var anim = _remotePlayer.GetComponentInChildren<Animator>();
                if (anim && data.animatorController)
                    anim.runtimeAnimatorController = data.animatorController;

                var sync = _remotePlayer.AddComponent<PlayerSync>();
                sync.Init(roomId, false);

                var nameText = _remotePlayer.GetComponentInChildren<TMP_Text>();
                if (nameText != null) nameText.text = GameData.RemoteUsername;

                return;
            }
        }
    }

    private void OnPlayerJoined(string username, int count, string character)
    {
        Debug.Log($"[PlayerSpawner] OnPlayerJoined: {username}, count: {count}, character: {character}");
        if (count == 2 && _remotePlayer == null)
            SpawnRemotePlayer(character);
    }

    private void OnPlayerLeft(string opponent)
    {
        Debug.Log($"[PlayerSpawner] OnPlayerLeft: {opponent}");
        if (_remotePlayer != null)
        {
            Destroy(_remotePlayer);
            _remotePlayer = null;
        }
    }

    private void OnReceiveGameState(string stateJson)
    {
        Debug.Log($"[PlayerSpawner] stateJson: {stateJson}");
        if (_remotePlayer == null) return;

        var state = JsonUtility.FromJson<PlayerState>(stateJson);
        var sync = _remotePlayer.GetComponent<PlayerSync>();
        if (sync != null)
            sync.SetTargetPosition(new Vector2(state.x, state.y));
    }
}

[System.Serializable]
public class PlayerState
{
    public float x;
    public float y;
}