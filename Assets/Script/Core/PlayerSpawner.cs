using UnityEngine;

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

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnReceiveGameState += OnReceiveGameState;
            NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnReceiveGameState -= OnReceiveGameState;
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

                return;
            }
        }
    }

    private void SpawnRemotePlayer()
    {
        string selected = GameData.SelectedCharacter;
        Vector3 spawnPos = spawnPointRemote != null ? spawnPointRemote.position : transform.position + Vector3.right * 3f;

        foreach (var data in characters)
        {
            if (data.characterName == selected)
            {
                _remotePlayer = Instantiate(data.prefab, spawnPos, Quaternion.identity);
                _remotePlayer.transform.localScale = data.scale;

                var anim = _remotePlayer.GetComponentInChildren<Animator>();
                if (anim && data.animatorController)
                    anim.runtimeAnimatorController = data.animatorController;

                var sync = _remotePlayer.AddComponent<PlayerSync>();
                sync.Init(roomId, false);

                return;
            }
        }
    }

    private void OnPlayerJoined(string username, int count)
    {
        if (count == 2 && _remotePlayer == null)
            SpawnRemotePlayer();
    }

    private void OnReceiveGameState(string stateJson)
    {
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