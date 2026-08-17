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

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 24f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 3f;
    [SerializeField] private KeyCode dashKey = KeyCode.Space;
    [SerializeField] private Color trailColor = new Color(0.1f, 0.9f, 1f, 0.8f);
    [SerializeField] private float trailTime = 0.25f;
    [SerializeField] private float trailStartWidth = 0.3f;

    private TrailRenderer trail;

    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 input;
    private Vector2 lastMoveDirection = Vector2.right;

    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;

    private int damage = 1;
    private string characterName = "";

    public bool IsDashing => isDashing;
    public bool IsDashReady => cooldownTimer <= 0f;
    public float CooldownProgress => dashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(cooldownTimer / dashCooldown);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 0f;

        trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.time = trailTime;
            trail.startWidth = trailStartWidth;
            trail.endWidth = 0f;

            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (trailShader == null)
                trailShader = Shader.Find("Sprites/Default");
            if (trailShader == null)
                trailShader = Shader.Find("Universal Render Pipeline/Unlit");

            trail.material = new Material(trailShader);
            trail.emitting = false;
        }
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

        if (input.sqrMagnitude > 0.01f)
            lastMoveDirection = input;

        anim.SetBool(movingParam, input.sqrMagnitude > 0.01f);

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(dashKey))
            TryDash();

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                if (trail != null)
                    trail.emitting = false;
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 moveDir = isDashing ? lastMoveDirection : input;
        float currentSpeed = isDashing ? dashSpeed : speed;

        Vector2 newPos = rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
        newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
        newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
        rb.MovePosition(newPos);
    }

    public void TryDash()
    {
        if (isDashing || cooldownTimer > 0f) return;

        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        if (trail != null)
            trail.emitting = true;
    }

    private Color GetTrailColorForCharacter()
    {
        string n = characterName.ToLower();

        if (n.Contains("violet"))
            return new Color(0.55f, 0.35f, 1f, 0.8f);   // Violeta-cian
        if (n.Contains("cyrus"))
            return new Color(1f, 0.15f, 0.15f, 0.8f);   // Rojo
        if (n.Contains("nyx"))
            return new Color(0.75f, 0.2f, 0.95f, 0.8f); // Morado
        if (n.Contains("atlas"))
            return new Color(0.15f, 0.6f, 1f, 0.8f);    // Azul-cian

        return trailColor; // Fallback por si el nombre no coincide con ninguno
    }

    public void Init(float speed, int damage, string characterName)
    {
        this.speed = speed;
        this.damage = damage;
        this.characterName = characterName ?? "";

        if (trail != null)
        {
            Color characterTrailColor = GetTrailColorForCharacter();
            trail.startColor = characterTrailColor;
            trail.endColor = new Color(characterTrailColor.r, characterTrailColor.g, characterTrailColor.b, 0f);

            if (trail.material.HasProperty("_BaseColor"))
                trail.material.SetColor("_BaseColor", characterTrailColor);
            if (trail.material.HasProperty("_Color"))
                trail.material.SetColor("_Color", characterTrailColor);
        }
    }
}