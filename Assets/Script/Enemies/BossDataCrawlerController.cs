using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// DataCrawler (issue #73), jefe de la oleada 20. No embiste ni tiene fases: se mueve en
// diagonal rebotando por la arena, dispara abanicos de balas al jugador a media distancia
// y cada cierto tiempo invoca 2-3 enemigos basicos (con un tope de vivos a la vez).
public class BossDataCrawlerController : BossBase
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Abanico")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float fanInterval = 2.5f;
    [SerializeField] private float fanRange = 10f;
    [SerializeField] private int fanBulletCount = 5;
    [SerializeField] private float fanSpread = 50f;
    [SerializeField] private int fanBulletDamage = 8;
    [SerializeField] private float fanTelegraph = 0.35f;
    [SerializeField] private Color fanTelegraphColor = new Color(1f, 0.4f, 1f, 1f);
    [SerializeField] private Sprite shootingSprite;

    [Header("Invocacion")]
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private float summonInterval = 9f;
    [SerializeField] private int minSummons = 2;
    [SerializeField] private int maxSummons = 3;
    [SerializeField] private int maxAlive = 6;
    [SerializeField] private float summonDistance = 1.5f;
    [SerializeField] private float summonTelegraph = 0.6f;
    [SerializeField] private Color summonTelegraphColor = new Color(0.4f, 1f, 0.5f, 1f);

    private readonly List<GameObject> summoned = new List<GameObject>();

    protected override IEnumerator Fight()
    {
        Vector2 dir = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f).normalized;
        float nextFan = Time.time + fanInterval;
        float nextSummon = Time.time + summonInterval * 0.5f;

        while (!IsDead)
        {
            if (Time.time >= nextSummon)
            {
                yield return SummonAdds();
                nextSummon = Time.time + summonInterval;
            }
            else if (Time.time >= nextFan && InFanRange())
            {
                yield return FireFan();
                nextFan = Time.time + fanInterval;
            }
            else
            {
                SetPose(moveSprite);
                dir = MoveDiagonal(dir);
                yield return new WaitForFixedUpdate();
            }
        }
    }

    private bool InFanRange() =>
        target != null && Vector2.Distance(transform.position, target.position) <= fanRange;

    // Avanza en diagonal y rebota en los bordes de la arena.
    private Vector2 MoveDiagonal(Vector2 dir)
    {
        Vector2 next = rb.position + dir * moveSpeed * Time.fixedDeltaTime;
        Vector2 clamped = ClampToArena(next);
        if (!Mathf.Approximately(clamped.x, next.x)) dir.x = -dir.x;
        if (!Mathf.Approximately(clamped.y, next.y)) dir.y = -dir.y;
        rb.MovePosition(clamped);
        return dir;
    }

    private IEnumerator FireFan()
    {
        SetPose(shootingSprite);
        yield return Blink(fanTelegraphColor, fanTelegraph);
        if (IsDead) yield break;

        ShootFan(bulletPrefab, DirectionToTarget(transform.position), fanBulletCount, fanSpread, fanBulletDamage);
        yield return new WaitForSeconds(0.25f);
        SetPose(idleSprite);
    }

    private IEnumerator SummonAdds()
    {
        int free = maxAlive - CountAlive(summoned);
        if (free <= 0 || summonPrefab == null) yield break;

        SetPose(idleSprite);
        yield return Blink(summonTelegraphColor, summonTelegraph);
        if (IsDead) yield break;

        int count = Mathf.Min(free, Random.Range(minSummons, maxSummons + 1));
        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count) * Mathf.PI * 2f + Random.value;
            Vector2 pos = (Vector2)transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * summonDistance;
            GameObject add = Summon(summonPrefab, pos);
            if (add != null) summoned.Add(add);
            yield return null; // una por frame (#170)
        }
    }
}
