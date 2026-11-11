using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType
    {
        HeavyWeapon,
        Shield,
        TripleShot,
        SpeedBoost
    }

    [Header("Config")]
    public PowerUpType type = PowerUpType.SpeedBoost;
    public float duration = 8f;

    [Header("Animation")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotateSpeed = 60f;

    private Vector3 startPos;
    private bool picked; // el casco y la zona de recogida pueden entrar en el mismo paso de fisica

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        if (enableRotation)
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject player = PlayerPickupRange.PlayerOf(other);
        if (player == null || picked) return;

        PlayerPowerUps powerUps = player.GetComponent<PlayerPowerUps>();
        if (powerUps != null)
        {
            picked = true;
            powerUps.ApplyPowerUp(type, duration);
            Destroy(gameObject);
        }
    }
}