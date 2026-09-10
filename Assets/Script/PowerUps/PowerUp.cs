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
        if (!other.CompareTag("Player")) return;

        PlayerPowerUps powerUps = other.GetComponentInParent<PlayerPowerUps>();
        if (powerUps != null)
        {
            powerUps.ApplyPowerUp(type, duration);
            Destroy(gameObject);
        }
    }
}