using UnityEngine;

// Proyectil de area de Flak (issue #194). Vuela en parabola fingida (crece a mitad de camino)
// hasta el punto marcado y explota alli: dana a los jugadores dentro de radius. Mientras vuela,
// un circulo en el suelo marca la zona de impacto y se va llenando para avisar de cuando cae.
// Sin collider: solo dana al explotar, nunca por el camino.
public class FlakShell : MonoBehaviour
{
    [SerializeField] private float flightTime = 1.2f;
    [SerializeField] private float arcScale = 1.6f;
    [SerializeField] private float radius = 1.3f;
    [SerializeField] private int damage = 15;

    [Header("Marca de impacto")]
    [SerializeField] private Color markerColor = new Color(1f, 0.45f, 0.1f, 0.25f);
    [SerializeField] private Color markerFillColor = new Color(1f, 0.3f, 0.05f, 0.45f);
    [SerializeField] private Color explosionColor = new Color(1f, 0.85f, 0.4f, 0.8f);
    [SerializeField] private float explosionDuration = 0.25f;
    [Tooltip("Sprite-Unlit-Default: la marca no depende de las luces 2D.")]
    [SerializeField] private Material markerMaterial;

    private const int TextureSize = 64;
    private static Sprite discSprite;

    private Vector3 start;
    private Vector3 target;
    private Vector3 baseScale;
    private float elapsed;
    private SpriteRenderer shellRenderer;
    private SpriteRenderer marker;
    private SpriteRenderer markerFill;
    private bool exploded;
    private bool visualOnly;

    // Copia en el invitado de un proyectil del anfitrion (#206): se ve y explota, pero no hace dano.
    public void MakeVisualOnly() => visualOnly = true;

    public void Init(Vector2 targetPoint)
    {
        start = transform.position;
        target = targetPoint;
        baseScale = transform.localScale;
        shellRenderer = GetComponent<SpriteRenderer>();

        marker = CreateDisc("ImpactMarker", markerColor, 0);
        markerFill = CreateDisc("ImpactMarkerFill", markerFillColor, 1);
        markerFill.transform.localScale = Vector3.zero;

        Vector2 d = targetPoint - (Vector2)start;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

        if (!visualOnly)
            CoopEnemySync.Instance?.ReportFlakShell(this, start, targetPoint);
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        if (exploded)
        {
            // Destello: el circulo se aclara y se desvanece.
            float k = elapsed / explosionDuration;
            Color c = explosionColor;
            c.a *= 1f - k;
            marker.color = c;
            if (k >= 1f) Destroy(gameObject);
            return;
        }

        float t = Mathf.Clamp01(elapsed / flightTime);
        transform.position = Vector3.Lerp(start, target, t);
        transform.localScale = baseScale * Mathf.Lerp(1f, arcScale, Mathf.Sin(t * Mathf.PI));

        // La marca se queda quieta en el suelo y el relleno crece hasta el borde al caer.
        marker.transform.position = target;
        marker.transform.localScale = WorldScale(radius * 2f);
        markerFill.transform.position = target;
        markerFill.transform.localScale = WorldScale(radius * 2f * t);

        if (t >= 1f) Explode();
    }

    private void Explode()
    {
        exploded = true;
        elapsed = 0f;
        if (shellRenderer != null) shellRenderer.enabled = false;
        markerFill.enabled = false;
        transform.localScale = baseScale;
        marker.transform.localScale = WorldScale(radius * 2f);

        if (visualOnly) return;

        foreach (var hit in Physics2D.OverlapCircleAll(target, radius))
        {
            if (!PlayerTargets.IsPlayer(hit)) continue;
            var health = hit.GetComponentInParent<PlayerHealth>();
            if (health != null && !health.IsDead)
                health.TakeDamage(damage);
        }
    }

    private SpriteRenderer CreateDisc(string name, Color color, int orderOffset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.rotation = Quaternion.identity;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDiscSprite();
        if (markerMaterial != null) sr.sharedMaterial = markerMaterial;
        sr.color = color;
        // Justo encima del suelo (Ground: Default/0) y por debajo del proyectil. Es translucida,
        // asi que si queda por encima de un personaje sigue viendose lo que hay debajo.
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 1 + orderOffset;
        go.transform.localScale = WorldScale(radius * 2f);
        return sr;
    }

    // Escala local para que el disco (1 unidad de diametro) mida 'diameter' en el mundo,
    // compensando la escala del proyectil, que cambia durante el vuelo.
    private Vector3 WorldScale(float diameter)
    {
        float parent = Mathf.Max(0.0001f, transform.lossyScale.x);
        float s = diameter / parent;
        return new Vector3(s, s, 1f);
    }

    // Disco con borde marcado, generado por codigo para no anadir arte.
    private static Sprite GetDiscSprite()
    {
        if (discSprite != null) return discSprite;

        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[TextureSize * TextureSize];
        float r = TextureSize * 0.5f;
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = d > 1f ? 0f : (d > 0.9f ? 1f : 0.6f);
                pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        discSprite = Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        return discSprite;
    }
}
