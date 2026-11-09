using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hivemind (issue #187), jefe de la oleada 40. Su peligro son los anadidos: se mueve de forma
// erratica por la arena (cambia de rumbo cada poco) y, a mas ritmo que el DataCrawler, invoca
// pequenos grupos de enemigos basicos, sin pasar nunca de maxAlive vivos a la vez.
public class BossHivemindController : BossBase
{
    [Header("Movimiento erratico")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float minRetarget = 1f;
    [SerializeField] private float maxRetarget = 2f;

    [Header("Invocacion")]
    [Tooltip("Enemigos basicos que invoca (al azar).")]
    [SerializeField] private List<GameObject> summonPrefabs = new List<GameObject>();
    [SerializeField] private float summonInterval = 3.5f;
    [SerializeField] private int summonsPerWave = 2;
    [SerializeField] private int maxAlive = 8;
    [SerializeField] private float summonDistance = 1.5f;
    [SerializeField] private float summonTelegraph = 0.5f;
    [SerializeField] private Color summonTelegraphColor = new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private Sprite summoningSprite;

    private readonly List<GameObject> summoned = new List<GameObject>();

    protected override IEnumerator Fight()
    {
        Vector2 destination = RandomArenaPoint();
        float retargetAt = Time.time;
        float nextSummon = Time.time + summonInterval * 0.5f;

        while (!IsDead)
        {
            if (Time.time >= nextSummon)
            {
                yield return SummonGroup();
                nextSummon = Time.time + summonInterval;
                continue;
            }

            if (Time.time >= retargetAt || rb.position == destination)
            {
                destination = RandomArenaPoint();
                retargetAt = Time.time + Random.Range(minRetarget, maxRetarget);
            }

            SetPose(moveSprite);
            rb.MovePosition(Vector2.MoveTowards(rb.position, destination, moveSpeed * Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator SummonGroup()
    {
        int free = maxAlive - CountAlive(summoned);
        if (free <= 0 || summonPrefabs.Count == 0) yield break;

        SetPose(summoningSprite);
        yield return Blink(summonTelegraphColor, summonTelegraph);
        if (IsDead) yield break;

        int count = Mathf.Min(free, summonsPerWave);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Count)];
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * summonDistance;
            GameObject add = Summon(prefab, pos);
            if (add != null) summoned.Add(add);
            yield return null; // una por frame (#170)
        }
        SetPose(idleSprite);
    }
}
