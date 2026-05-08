using System.Threading.Tasks;
using UnityEngine;

public class PlayerSync : MonoBehaviour
{
    [SerializeField] private bool isLocalPlayer = true;
    [SerializeField] private float sendRate = 0.05f;
    [SerializeField] private float interpolationSpeed = 10f;

    private string _roomId;
    private Vector2 _targetPosition;
    private float _timer;

    public void Init(string roomId, bool isLocal)
    {
        _roomId = roomId;
        isLocalPlayer = isLocal;
        _targetPosition = transform.position;

        if (!isLocal)
        {
            GetComponent<PlayerController>().enabled = false;
            var joystick = GetComponent<Joystick>();
            if (joystick != null) joystick.enabled = false;
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            _timer += Time.deltaTime;
            if (_timer >= sendRate)
            {
                _timer = 0f;
                _ = SendPositionAsync();
            }
        }
        else
        {
            transform.position = Vector2.Lerp(transform.position, _targetPosition, Time.deltaTime * interpolationSpeed);
        }
    }

    private async Task SendPositionAsync()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;
        var pos = transform.position;
        var state = new PlayerState { x = pos.x, y = pos.y };
        var json = JsonUtility.ToJson(state);
        await NetworkManager.Instance.SendGameStateAsync(_roomId, json);
    }

    public void SetTargetPosition(Vector2 position)
    {
        _targetPosition = position;
    }
}