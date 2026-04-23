using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;

    [Header("Animator params")]
    [SerializeField] private string movingParam = "Moving";

    [Header("Bounds")]
    [SerializeField] private float minX = 1029f;
    [SerializeField] private float maxX = 1052f;
    [SerializeField] private float minY = 524f;
    [SerializeField] private float maxY = 534f;

    [Header("Mobile")]
    [SerializeField] private Joystick joystick;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 input;

    private int damage = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0f;
    }
    private void Start()
    {
        if (joystick == null)
            joystick = FindFirstObjectByType<Joystick>();
    }
    private void Update()
    {
        // Teclado
        Vector2 keyboardInput;
        keyboardInput.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        keyboardInput.y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);

        // Joystick móvil
        Vector2 joystickInput = joystick != null ? joystick.Direction : Vector2.zero;

        // Combinar ambos
        input = (keyboardInput + joystickInput).normalized;

        anim.SetBool(movingParam, input.sqrMagnitude > 0.01f);
    }

    private void FixedUpdate()
    {
        Vector2 newPos = rb.position + input * speed * Time.fixedDeltaTime;
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
        rb.MovePosition(newPos);
    }

    public void Init(float speed, int damage)
    {
        this.speed = speed;
        this.damage = damage;
    }
}