using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;

    [Header("Animator params")]
    [SerializeField] private string movingParam = "Moving";

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0f;
    }

    private void Update()
    {
        input.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        input.y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        input = input.normalized;

        anim.SetBool(movingParam, input.sqrMagnitude > 0.01f);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
    }
}