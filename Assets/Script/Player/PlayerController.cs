using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 8f;

    [Header("Animator params (exact names)")]
    public string movingParam = "Moving";
    public string shootParam  = "Shoot";

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0f;
    }

    void Update()
    {
        // INPUT
        input.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        input.y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
        input = input.normalized;

        // ANIMATOR
        anim.SetBool(movingParam, input.sqrMagnitude > 0.01f);
        anim.SetBool(shootParam, Input.GetKey(KeyCode.Space));
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * speed;
    }
}
