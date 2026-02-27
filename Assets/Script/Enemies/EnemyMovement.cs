using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2f;
    public Vector2 direction = Vector2.left;

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
