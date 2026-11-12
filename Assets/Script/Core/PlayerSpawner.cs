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
        PlayerTargets.Local = null;
        PlayerTargets.Remote = null;
        PlayerTargets.LocalDown = false;
        PlayerTargets.RemoteDown = false;
        SpawnLocalPlayer();

        if (!string.IsNullOrEmpty(GameData.RemoteCharacter))
            SpawnRemotePlayer(GameData.RemoteCharacter);

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnReceiveGameState += OnReceiveGameState;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
        }

        // Enemigos compartidos en cooperativo: el anfitrion los simula y el invitado ve copias (#206).
        if (GameData.IsCoop)
            gameObject.AddComponent<CoopEnemySync>().Init(roomId);
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
        Transform localPoint = GameData.IsHost ? spawnPointLocal : spawnPointRemote;
        Vector3 spawnPos = localPoint != null ? localPoint.position : transform.position;

        foreach (var data in characters)
        {
            if (data.characterName == selected)
            {
                GameObject player = Instantiate(data.prefab, spawnPos, Quaternion.identity);
                player.transform.localScale = data.scale;

                var health = player.GetComponent<PlayerHealth>();
                if (health) health.Init(data.maxHealth);

                var controller = player.GetComponent<PlayerController>();
                if (controller) controller.Init(data.speed, data.damage, data.characterName);

                var weapon = player.GetComponent<WeaponController>();
                if (weapon) weapon.SetBulletSprite(data.bulletSprite, data.bulletScale);

                var anim = player.GetComponentInChildren<Animator>();
                if (anim && data.animatorController)
                    anim.runtimeAnimatorController = data.animatorController;

                var sync = player.AddComponent<PlayerSync>();
                sync.Init(roomId, true);
                PlayerTargets.Local = player.transform;

                PlayerPickupRange.Create(player);

                var nameText = player.GetComponentInChildren<TMP_Text>();
                if (nameText != null) nameText.text = GameData.Username;

                var healthUI = FindFirstObjectByType<PlayerHealthUI>(FindObjectsInactive.Include);
                if (healthUI != null) healthUI.playerHealth = health;

                return;
            }
        }
    }

    private void SpawnRemotePlayer(string character)
    {
        Transform remotePoint = GameData.IsHost ? spawnPointRemote : spawnPointLocal;
        Vector3 spawnPos = remotePoint != null ? remotePoint.position : transform.position + Vector3.right * 3f;

        foreach (var data in characters)
        {
            if (data.characterName == character)
            {
                _remotePlayer = Instantiate(data.prefab, spawnPos, Quaternion.identity);
                _remotePlayer.transform.localScale = data.scale;

                // Los enemigos del anfitrion tambien atacan a esta copia; el dano se reenvia al otro jugador (#206).
                var remoteHealth = _remotePlayer.GetComponent<PlayerHealth>();
                if (remoteHealth != null) remoteHealth.MakeRemoteProxy();
                PlayerTargets.Remote = _remotePlayer.transform;

                _remotePlayer.tag = "Untagged";

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
        if (count == 2 && _remotePlayer == null)
            SpawnRemotePlayer(character);
    }

    private void OnPlayerLeft(string opponent)
    {
        if (_remotePlayer != null)
        {
            Destroy(_remotePlayer);
            _remotePlayer = null;
            PlayerTargets.Remote = null;
        }
    }

    private void OnReceiveGameState(string stateJson)
    {
        if (_remotePlayer == null) return;

        var state = JsonUtility.FromJson<PlayerState>(stateJson);
        if (state.kind != PlayerState.PositionKind) return;

        var sync = _remotePlayer.GetComponent<PlayerSync>();
        if (sync != null)
            sync.SetTargetPosition(new Vector2(state.x, state.y));
    }
}

[System.Serializable]
public class PlayerState
{
    public const string PositionKind = "pos";

    public string kind;
    public float x;
    public float y;
}