using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;

    [Header("Animator params")]
    [SerializeField] private string movingParam = "Moving";

    [Header("Bounds")]
    [Tooltip("Solo si la escena no tiene muros Wall_*: con muros, el limite es su borde interior menos lo que mide el collider de la nave.")]
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

    private Collider2D bodyCollider;
    private Rect? arenaInner;

    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;

    private int damage = 1;
    private string characterName = "";
    private float speedMultiplier = 1f;

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

        bodyCollider = GetComponent<Collider2D>();
        arenaInner = FindArenaInner();
    }

    // Rectangulo interior de los muros Wall_* de la escena: para cada muro, su cara interior
    // es lo mas interior entre su collider y lo que se ve de el (su sprite o el de sus hijos).
    private Rect? FindArenaInner()
    {
        float left = float.NegativeInfinity, right = float.PositiveInfinity;
        float bottom = float.NegativeInfinity, top = float.PositiveInfinity;
        int found = 0;

        foreach (var wall in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (!wall.name.StartsWith("Wall_") || wall.gameObject.scene != gameObject.scene) continue;

            Bounds b = wall.bounds;
            foreach (var r in wall.GetComponentsInChildren<Renderer>())
                if (r.enabled && r.gameObject.activeInHierarchy) b.Encapsulate(r.bounds);

            switch (wall.name)
            {
                case "Wall_Left": left = Mathf.Max(left, b.max.x); found++; break;
                case "Wall_Right": right = Mathf.Min(right, b.min.x); found++; break;
                case "Wall_Bottom": bottom = Mathf.Max(bottom, b.max.y); found++; break;
                case "Wall_Top": top = Mathf.Min(top, b.min.y); found++; break;
            }
        }

        if (found < 4 || left >= right || bottom >= top) return null;
        return Rect.MinMaxRect(left, bottom, right, top);
    }

    // Limites para la posicion de la nave: con muros, que su collider (ajustado al sprite)
    // no pase de la cara interior de ningun muro; sin muros, los valores del Inspector.
    private Vector2 ClampToArena(Vector2 pos)
    {
        if (!arenaInner.HasValue || bodyCollider == null)
            return new Vector2(Mathf.Clamp(pos.x, minX, maxX), Mathf.Clamp(pos.y, minY, maxY));

        Rect a = arenaInner.Value;
        Bounds cb = bodyCollider.bounds;
        Vector2 current = rb.position;
        float l = current.x - cb.min.x, r = cb.max.x - current.x;
        float d = current.y - cb.min.y, u = cb.max.y - current.y;

        return new Vector2(
            Mathf.Clamp(pos.x, a.xMin + l, Mathf.Max(a.xMin + l, a.xMax - r)),
            Mathf.Clamp(pos.y, a.yMin + d, Mathf.Max(a.yMin + d, a.yMax - u)));
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
        float currentSpeed = isDashing ? dashSpeed : speed * speedMultiplier;

        Vector2 newPos = rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(ClampToArena(newPos));
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

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
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