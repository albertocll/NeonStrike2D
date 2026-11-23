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
    private PlayerAutoOrientation2D _orientation;

    public void Init(string roomId, bool isLocal)
    {
        _roomId = roomId;
        isLocalPlayer = isLocal;
        _targetPosition = transform.position;
        _anim = GetComponentInChildren<Animator>();
        _orientation = GetComponent<PlayerAutoOrientation2D>();

        if (!isLocal)
        {
            GetComponent<PlayerController>().enabled = false;
            // La posicion la pone este script directamente: sin interpolacion de la fisica, que la pisaria.
            var body = GetComponent<Rigidbody2D>();
            if (body != null) body.interpolation = RigidbodyInterpolation2D.None;
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
        // dx, dy: hacia donde mira la nave (1.0.2). Un cliente anterior no los manda y se ignoran.
        Vector2 facing = _orientation != null ? _orientation.FacingDirection : Vector2.right;
        var json = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{{\"kind\":\"{0}\",\"x\":{1},\"y\":{2},\"dx\":{3},\"dy\":{4}}}",
            PlayerState.PositionKind, pos.x, pos.y, facing.x, facing.y);
        await NetworkManager.Instance.SendGameStateAsync(_roomId, json);
    }

    public void SetTargetFacing(Vector2 direction)
    {
        if (_orientation != null) _orientation.SetRemoteFacing(direction);
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