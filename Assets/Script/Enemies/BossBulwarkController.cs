using System.Collections;
using UnityEngine;

// Bulwark (issue #186), jefe de la oleada 30. Tanque lento que persigue al jugador con dano
// de contacto alto. Mientras tiene el escudo, todo el dano va al escudo y la vida no baja
// (un halo cian lo rodea y destella con cada impacto). Al romperse el escudo cambia al sprite
// de escudo roto y queda vulnerable unos segundos; antes de regenerarlo parpadea para avisar.
public class BossBulwarkController : BossBase
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 1.2f;

    [Header("Escudo")]
    [Tooltip("Dano que aguanta el escudo (bala del jugador: 25).")]
    [SerializeField] private int shieldHealth = 400;
    [SerializeField] private float vulnerableDuration = 5f;
    [SerializeField] private float regenWarning = 0.8f;
    [SerializeField] private Sprite shieldBrokenSprite;

    [Header("Halo del escudo")]
    [SerializeField] private Color haloColor = new Color(0.3f, 0.9f, 1f, 0.22f);
    [SerializeField] private Color haloHitColor = new Color(0.7f, 1f, 1f, 0.7f);
    [Tooltip("Diametro del halo respecto al ancho del collider.")]
    [SerializeField] private float haloSize = 1.4f;
    [Tooltip("Sprite-Unlit-Default: el halo no depende de las luces 2D.")]
    [SerializeField] private Material haloMaterial;

    private const int HaloTextureSize = 64;
    private static Sprite haloSprite;

    private int currentShield;
    private bool shieldUp;
    private SpriteRenderer halo;
    private float haloFlashUntil;

    public bool ShieldUp => shieldUp;

    protected override void Awake()
    {
        base.Awake();
        halo = CreateHalo();
        RaiseShield();
    }

    protected override IEnumerator Fight()
    {
        while (!IsDead)
        {
            // Persecucion lenta mientras el escudo aguanta.
            while (!IsDead && shieldUp)
            {
                Chase();
                yield return new WaitForFixedUpdate();
            }
            if (IsDead) yield break;

            // Escudo roto: vulnerable y sigue avanzando.
            float until = Time.time + vulnerableDuration;
            while (!IsDead && Time.time < until - regenWarning)
            {
                Chase();
                yield return new WaitForFixedUpdate();
            }

            // Aviso de regeneracion: el halo parpadea.
            for (float t = 0f; t < regenWarning && !IsDead; t += Time.deltaTime)
            {
                halo.enabled = Mathf.Repeat(t, 0.16f) < 0.08f;
                yield return null;
            }
            if (IsDead) yield break;
            RaiseShield();
        }
    }

    private void Chase()
    {
        if (target == null) return;
        SetPose(shieldUp ? moveSprite : shieldBrokenSprite);
        Vector2 next = Vector2.MoveTowards(rb.position, target.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(ClampToArena(next));
    }

    protected override int FilterDamage(int damage)
    {
        if (!shieldUp) return damage;

        currentShield -= damage;
        haloFlashUntil = Time.time + 0.08f;
        if (currentShield <= 0) BreakShield();
        return 0;
    }

    private void RaiseShield()
    {
        currentShield = shieldHealth;
        shieldUp = true;
        halo.enabled = true;
        SetPose(idleSprite);
    }

    private void BreakShield()
    {
        shieldUp = false;
        halo.enabled = false;
        SetPose(shieldBrokenSprite);
        SFXManager.Instance?.PlayEnemyDeath();
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead)
        {
            if (halo.enabled) halo.enabled = false;
            return;
        }
        if (!shieldUp) return;

        halo.color = Time.time < haloFlashUntil ? haloHitColor : haloColor;
        float diameter = (myCol != null ? myCol.bounds.size.x : 2f) * haloSize;
        float s = diameter / Mathf.Max(0.0001f, transform.lossyScale.x);
        halo.transform.localScale = new Vector3(s, s, 1f);
    }

    private SpriteRenderer CreateHalo()
    {
        var go = new GameObject("ShieldHalo");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetHaloSprite();
        if (haloMaterial != null) sr.sharedMaterial = haloMaterial;
        sr.color = haloColor;
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
        return sr;
    }

    // Burbuja: centro casi transparente y borde marcado.
    private static Sprite GetHaloSprite()
    {
        if (haloSprite != null) return haloSprite;

        var tex = new Texture2D(HaloTextureSize, HaloTextureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[HaloTextureSize * HaloTextureSize];
        float r = HaloTextureSize * 0.5f;
        for (int y = 0; y < HaloTextureSize; y++)
        {
            for (int x = 0; x < HaloTextureSize; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = d > 1f ? 0f : Mathf.Lerp(0.25f, 1f, Mathf.Pow(d, 4f));
                pixels[y * HaloTextureSize + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        haloSprite = Sprite.Create(tex, new Rect(0, 0, HaloTextureSize, HaloTextureSize), new Vector2(0.5f, 0.5f), HaloTextureSize);
        return haloSprite;
    }
}
