using UnityEngine;

// Halo naranja que palpita alrededor de Fuse para que se distinga del fondo.
// Va en un hijo del prefab con su propio SpriteRenderer; el sprite radial se
// genera por codigo (compartido entre todas las Fuse) para no añadir arte.
[RequireComponent(typeof(SpriteRenderer))]
public class FuseHalo : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.5f, 0.05f, 0.595f);
    [Tooltip("Diametro del halo en unidades de mundo.")]
    [SerializeField] private float size = 1.36f;
    [Tooltip("Pulsos por segundo.")]
    [SerializeField] private float pulseSpeed = 2f;
    [Tooltip("Cuanto crece y se aviva en cada pulso (0 = nada, 1 = el doble).")]
    [Range(0f, 1f)]
    [SerializeField] private float pulseAmount = 0.35f;

    private const int TextureSize = 64;
    private static Sprite haloSprite;

    private SpriteRenderer haloRenderer;
    private SpriteRenderer ownerRenderer;

    void Awake()
    {
        haloRenderer = GetComponent<SpriteRenderer>();
        haloRenderer.sprite = GetHaloSprite();
        ownerRenderer = transform.parent.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // EnemyController.Die() oculta el sprite de Fuse al detonar y destruye
        // el objeto 0.8 s despues: el halo se apaga a la vez que el sprite.
        haloRenderer.enabled = ownerRenderer.enabled;
        if (!haloRenderer.enabled) return;

        // Seno al cuadrado: va de 0 a 1 y vuelve, pulseSpeed veces por segundo.
        float s = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI);
        float pulse = s * s;

        float parentScale = transform.parent.lossyScale.x;
        float scale = size * (1f + pulseAmount * pulse) / parentScale;
        transform.localScale = new Vector3(scale, scale, 1f);

        Color c = color;
        c.a = color.a * Mathf.Lerp(1f - pulseAmount, 1f, pulse);
        haloRenderer.color = c;
    }

    private static Sprite GetHaloSprite()
    {
        if (haloSprite != null) return haloSprite;

        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[TextureSize * TextureSize];
        float r = TextureSize * 0.5f;
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        haloSprite = Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        return haloSprite;
    }
}
