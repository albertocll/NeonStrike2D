using System;
using System.Collections;
using UnityEngine;

// NexusCore (issue #189), jefe final de la oleada 60. Seis fases por vida, cada una con sus
// sprites (de cian a rojo) y la mecanica de un jefe anterior:
//   1 Sentinel: embestidas con aviso.
//   2 Bulwark: escudo que absorbe el dano hasta romperse; vulnerable unos segundos y vuelve.
//   3 DataCrawler: movimiento en diagonal y disparo en cruz (alterna + y x).
//   4 Hivemind: movimiento erratico e invocacion de racimos de Swarm con tope de vivos.
//   5 Specter: teletransporte (intangible) y rafaga dirigida al jugador.
//   6 Final: sin ataque especial, persigue al jugador cada vez mas rapido.
// No hay sprite de transicion: al cambiar de fase se queda invulnerable, parpadea en blanco
// y late un momento. La barra de vida muestra las marcas de las fases (IBossPhases).
public class BossNexusCoreController : BossBase, IBossPhases
{
    [Serializable]
    private class PhaseSprites
    {
        public Sprite idle;
        public Sprite hit;
        public Sprite move;
        [Tooltip("Escudo roto (2), disparo (3), invocacion (4) o teletransporte (5).")]
        public Sprite special;
    }

    [Header("Fases")]
    [Tooltip("Fraccion de vida a la que empieza cada fase 2..6.")]
    [SerializeField] private float[] phaseThresholds = { 0.83f, 0.67f, 0.5f, 0.33f, 0.17f };
    [SerializeField] private PhaseSprites[] phaseSprites = new PhaseSprites[6];
    [SerializeField] private float transitionDuration = 1.2f;
    [SerializeField] private float transitionPulse = 0.15f;

    [Header("Fase 1: embestida")]
    [SerializeField] private float chargeTelegraph = 0.5f;
    [SerializeField] private float chargeSpeed = 14f;
    [SerializeField] private float chargeDuration = 0.6f;
    [SerializeField] private float chargePause = 0.9f;

    [Header("Fase 2: escudo")]
    [SerializeField] private int shieldHealth = 500;
    [SerializeField] private float shieldVulnerableDuration = 4f;
    [SerializeField] private float shieldRegenWarning = 0.8f;
    [SerializeField] private float shieldChaseSpeed = 1.5f;
    [SerializeField] private Color haloColor = new Color(0.3f, 0.5f, 1f, 0.22f);
    [SerializeField] private Color haloHitColor = new Color(0.6f, 0.8f, 1f, 0.7f);
    [SerializeField] private float haloSize = 1.4f;
    [SerializeField] private Material haloMaterial;

    [Header("Fase 3: diagonal y cruz")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float diagonalSpeed = 2.5f;
    [SerializeField] private float crossInterval = 1.6f;
    [SerializeField] private int crossBulletDamage = 8;

    [Header("Fase 4: invocacion")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private float summonInterval = 4f;
    [SerializeField] private int maxAlive = 10;
    [SerializeField] private int unitsPerSummon = 5;
    [SerializeField] private float erraticSpeed = 2.5f;

    [Header("Fase 5: teletransporte")]
    [SerializeField] private float teleportIdle = 2f;
    [SerializeField] private float teleportFade = 0.4f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private int burstBulletDamage = 8;

    [Header("Fase 6: final")]
    [SerializeField] private float finalSpeed = 2.5f;
    [SerializeField] private float finalSpeedGain = 0.15f; // por segundo

    private const int HaloTextureSize = 64;
    private static Sprite haloSprite;

    private int currentPhase;
    private int currentShield;
    private bool shieldUp;
    private SpriteRenderer halo;
    private float haloFlashUntil;

    public float[] PhaseThresholds => phaseThresholds;
    public int CurrentPhase => currentPhase + 1;
    public bool ShieldUp => shieldUp;

    protected override void Awake()
    {
        base.Awake();
        halo = CreateHalo();
        halo.enabled = false;
        ApplyPhaseSprites(0);
    }

    private int PhaseFor(float fraction)
    {
        int p = 0;
        while (p < phaseThresholds.Length && fraction <= phaseThresholds[p]) p++;
        return p;
    }

    private bool StillIn(int phase) => !IsDead && PhaseFor(HealthFraction) == phase;

    protected override IEnumerator Fight()
    {
        currentPhase = -1;
        while (!IsDead)
        {
            int p = PhaseFor(HealthFraction);
            if (p != currentPhase)
            {
                if (currentPhase >= 0) yield return Transition(p);
                currentPhase = p;
                ApplyPhaseSprites(p);
                if (p == 1) RaiseShield();
            }
            if (IsDead) yield break;

            switch (currentPhase)
            {
                case 0: yield return ChargePhase(); break;
                case 1: yield return ShieldPhase(); break;
                case 2: yield return CrossPhase(); break;
                case 3: yield return SummonPhase(); break;
                case 4: yield return TeleportPhase(); break;
                default: yield return FinalPhase(); break;
            }
        }
    }

    // ---- Transicion ----

    private IEnumerator Transition(int next)
    {
        IsInvulnerable = true;
        shieldUp = false;
        halo.enabled = false;
        SetAlpha(1f);
        rb.linearVelocity = Vector2.zero;
        SFXManager.Instance?.PlayBossAppear();

        Vector3 baseScale = transform.localScale;
        for (float t = 0f; t < transitionDuration && !IsDead; t += Time.deltaTime)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Mathf.Repeat(t, 0.12f) < 0.06f ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            float k = 1f + transitionPulse * Mathf.Sin(t / transitionDuration * Mathf.PI * 3f);
            transform.localScale = baseScale * k;
            if (t > transitionDuration * 0.5f) ApplyPhaseSprites(next);
            yield return null;
        }
        transform.localScale = baseScale;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        IsInvulnerable = false;
    }

    private void ApplyPhaseSprites(int p)
    {
        if (phaseSprites == null || p >= phaseSprites.Length || phaseSprites[p] == null) return;
        var s = phaseSprites[p];
        idleSprite = s.idle;
        hitSprite = s.hit;
        moveSprite = s.move != null ? s.move : s.idle;
        SetPose(idleSprite);
    }

    private Sprite Special => phaseSprites[currentPhase]?.special;

    // ---- Fase 1: Sentinel ----

    private IEnumerator ChargePhase()
    {
        SetPose(idleSprite);
        yield return new WaitForSeconds(chargePause);
        if (!StillIn(0)) yield break;

        yield return Blink(Color.white, chargeTelegraph);
        if (!StillIn(0)) yield break;

        Vector2 dir = DirectionToTarget(rb.position);
        SetPose(moveSprite);
        for (float t = 0f; t < chargeDuration && StillIn(0); t += Time.fixedDeltaTime)
        {
            Vector2 next = rb.position + dir * chargeSpeed * Time.fixedDeltaTime;
            Vector2 clamped = ClampToArena(next);
            rb.MovePosition(clamped);
            yield return new WaitForFixedUpdate();
            if (clamped != next) break;
        }
        SetPose(idleSprite);
    }

    // ---- Fase 2: Bulwark ----

    private IEnumerator ShieldPhase()
    {
        while (StillIn(1) && shieldUp)
        {
            Chase(shieldChaseSpeed);
            yield return new WaitForFixedUpdate();
        }
        if (!StillIn(1)) yield break;

        float until = Time.time + shieldVulnerableDuration;
        while (StillIn(1) && Time.time < until - shieldRegenWarning)
        {
            SetPose(Special);
            Chase(shieldChaseSpeed);
            yield return new WaitForFixedUpdate();
        }
        for (float t = 0f; t < shieldRegenWarning && StillIn(1); t += Time.deltaTime)
        {
            halo.enabled = Mathf.Repeat(t, 0.16f) < 0.08f;
            yield return null;
        }
        if (StillIn(1)) RaiseShield();
    }

    private void RaiseShield()
    {
        currentShield = shieldHealth;
        shieldUp = true;
        halo.enabled = true;
        SetPose(idleSprite);
    }

    protected override int FilterDamage(int damage)
    {
        if (!shieldUp) return damage;
        currentShield -= damage;
        haloFlashUntil = Time.time + 0.08f;
        if (currentShield <= 0)
        {
            shieldUp = false;
            halo.enabled = false;
            SFXManager.Instance?.PlayEnemyDeath();
        }
        return 0;
    }

    // ---- Fase 3: DataCrawler ----

    private IEnumerator CrossPhase()
    {
        Vector2 dir = new Vector2(UnityEngine.Random.value < 0.5f ? -1f : 1f, UnityEngine.Random.value < 0.5f ? -1f : 1f).normalized;
        float nextShot = Time.time + crossInterval;
        bool diagonalCross = false;

        while (StillIn(2))
        {
            if (Time.time >= nextShot)
            {
                SetPose(Special);
                FireCross(diagonalCross);
                diagonalCross = !diagonalCross;
                nextShot = Time.time + crossInterval;
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            SetPose(moveSprite);
            Vector2 next = rb.position + dir * diagonalSpeed * Time.fixedDeltaTime;
            Vector2 clamped = ClampToArena(next);
            if (!Mathf.Approximately(clamped.x, next.x)) dir.x = -dir.x;
            if (!Mathf.Approximately(clamped.y, next.y)) dir.y = -dir.y;
            rb.MovePosition(clamped);
            yield return new WaitForFixedUpdate();
        }
    }

    private void FireCross(bool diagonal)
    {
        float offset = diagonal ? 45f : 0f;
        for (int i = 0; i < 4; i++)
        {
            float a = (offset + 90f * i) * Mathf.Deg2Rad;
            Shoot(bulletPrefab, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), crossBulletDamage);
        }
    }

    // ---- Fase 4: Hivemind ----

    private IEnumerator SummonPhase()
    {
        Vector2 destination = RandomArenaPoint();
        float retargetAt = Time.time;
        float nextSummon = Time.time + 1f;

        while (StillIn(3))
        {
            if (Time.time >= nextSummon)
            {
                nextSummon = Time.time + summonInterval;
                if (summonPrefab != null && AliveBasicEnemies() + unitsPerSummon <= maxAlive)
                {
                    SetPose(Special);
                    yield return Blink(new Color(1f, 0.8f, 0.3f, 1f), 0.5f);
                    if (!StillIn(3)) yield break;
                    Summon(summonPrefab, (Vector2)transform.position + UnityEngine.Random.insideUnitCircle.normalized * 1.5f);
                }
                continue;
            }

            if (Time.time >= retargetAt || rb.position == destination)
            {
                destination = RandomArenaPoint();
                retargetAt = Time.time + UnityEngine.Random.Range(1f, 2f);
            }
            SetPose(moveSprite);
            rb.MovePosition(Vector2.MoveTowards(rb.position, destination, erraticSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
    }

    private static int AliveBasicEnemies()
    {
        int alive = 0;
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (!e.IsDead) alive++;
        return alive;
    }

    // ---- Fase 5: Specter ----

    private IEnumerator TeleportPhase()
    {
        SetPose(idleSprite);
        yield return new WaitForSeconds(teleportIdle);
        if (!StillIn(4)) yield break;

        IsInvulnerable = true;
        SetPose(Special);
        yield return Fade(1f, 0.25f);
        if (!IsDead) rb.position = FarPoint();
        yield return Fade(0.25f, 1f);
        IsInvulnerable = false;
        SetAlpha(1f);
        SetPose(idleSprite);

        for (int i = 0; i < burstCount && StillIn(4); i++)
        {
            ShootFan(bulletPrefab, DirectionToTarget(transform.position), 3, 20f, burstBulletDamage);
            yield return new WaitForSeconds(0.15f);
        }
    }

    private Vector2 FarPoint()
    {
        Vector2 best = RandomArenaPoint();
        for (int i = 0; i < 12; i++)
        {
            Vector2 p = RandomArenaPoint();
            if (target == null || Vector2.Distance(p, target.position) >= 4f) return p;
            if (Vector2.Distance(p, target.position) > Vector2.Distance(best, target.position)) best = p;
        }
        return best;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < teleportFade && !IsDead; t += Time.deltaTime)
        {
            float a = Mathf.Lerp(from, to, t / teleportFade);
            if (Mathf.Repeat(t, 0.1f) < 0.05f) a *= 0.5f;
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(to);
    }

    // ---- Fase 6: final ----

    private IEnumerator FinalPhase()
    {
        float start = Time.time;
        while (!IsDead)
        {
            SetPose(moveSprite);
            Chase(finalSpeed + (Time.time - start) * finalSpeedGain);
            yield return new WaitForFixedUpdate();
        }
    }

    // ---- Comun ----

    private void Chase(float speed)
    {
        if (target == null) return;
        rb.MovePosition(ClampToArena(Vector2.MoveTowards(rb.position, target.position, speed * Time.fixedDeltaTime)));
    }

    private void SetAlpha(float a)
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = a;
        spriteRenderer.color = c;
    }

    protected override void Update()
    {
        base.Update();
        if (IsDead)
        {
            if (halo.enabled) halo.enabled = false;
            return;
        }
        if (!halo.enabled) return;

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

    // Burbuja: centro casi transparente y borde marcado (igual que la de Bulwark).
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
