using System.Threading.Tasks;
using UnityEngine;

public class PlayerSync : MonoBehaviour
{
    [SerializeField] private bool isLocalPlayer = true;
    [SerializeField] private float sendRate = 0.05f;
    [SerializeField] private float interpolationSpeed = 10f;
    [SerializeField] private float movingThreshold = 0.05f;
    [SerializeField] private string movingParam = "Moving";

    private string _roomId;
    private Vector2 _targetPosition;
    private float _timer;
    private bool _hasReceivedFirstState = false;
    private Animator _anim;

    public void Init(string roomId, bool isLocal)
    {
        _roomId = roomId;
        isLocalPlayer = isLocal;
        _targetPosition = transform.position;
        _anim = GetComponentInChildren<Animator>();

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

            if (_anim != null)
            {
                bool isMoving = Vector2.Distance(transform.position, _targetPosition) > movingThreshold;
                _anim.SetBool(movingParam, isMoving);
            }
        }
    }

    private async Task SendPositionAsync()
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;
        var pos = transform.position;
        var json = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{{\"x\":{0},\"y\":{1}}}", pos.x, pos.y);
        await NetworkManager.Instance.SendGameStateAsync(_roomId, json);
    }

    public void SetTargetPosition(Vector2 position)
    {
        _targetPosition = position;
        if (!_hasReceivedFirstState)
        {
            transform.position = position;
            _hasReceivedFirstState = true;
        }
    }
}