using UnityEngine;

public class Collectible : MonoBehaviour
{
    public enum CollectibleType { Health, Score }

    [Header("Config")]
    public CollectibleType type = CollectibleType.Score;
    public int value = 100;
    public int healthAmount = 20;

    [Header("Animation")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"[Collectible] Recogido por {other.name}, tipo: {type}");

        if (type == CollectibleType.Score)
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(value);
        }
        else if (type == CollectibleType.Health)
        {
            var health = other.GetComponent<PlayerHealth>();
            if (health != null && !health.IsDead)
                health.Heal(healthAmount);
        }

        Destroy(gameObject);
    }
}
