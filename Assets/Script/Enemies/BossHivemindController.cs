using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hivemind (issue #187), jefe de la oleada 40. Su peligro son los anadidos: se mueve de forma
// erratica por la arena (cambia de rumbo cada poco) y, a mas ritmo que el DataCrawler, invoca
// enjambres (EnemySwarmUnit: cada uno genera su racimo al aparecer). El tope cuenta todos los
// enemigos basicos vivos de la arena, racimos incluidos, para no saturar la partida.
public class BossHivemindController : BossBase
{
    [Header("Movimiento erratico")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float minRetarget = 1f;
    [SerializeField] private float maxRetarget = 2f;

    [Header("Invocacion")]
    [Tooltip("Enemigos que invoca (al azar). EnemySwarmUnit trae su propio racimo.")]
    [SerializeField] private List<GameObject> summonPrefabs = new List<GameObject>();
    [SerializeField] private float summonInterval = 3.5f;
    [SerializeField] private int summonsPerWave = 1;
    [Tooltip("Maximo de enemigos basicos vivos en la arena (cada racimo de Swarm son 5).")]
    [SerializeField] private int maxAlive = 12;
    [SerializeField] private float summonDistance = 1.5f;
    [SerializeField] private float summonTelegraph = 0.5f;
    [SerializeField] private Color summonTelegraphColor = new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private Sprite summoningSprite;

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
        if (AliveBasicEnemies() >= maxAlive || summonPrefabs.Count == 0) yield break;

        SetPose(summoningSprite);
        yield return Blink(summonTelegraphColor, summonTelegraph);
        if (IsDead) yield break;

        for (int i = 0; i < summonsPerWave; i++)
        {
            GameObject prefab = summonPrefabs[Random.Range(0, summonPrefabs.Count)];
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle.normalized * summonDistance;
            Summon(prefab, pos);
            yield return null; // una por frame (#170)
        }
        SetPose(idleSprite);
    }

    // ponytail: recorre los enemigos de la escena en cada invocacion (cada 3,5 s); en la
    // oleada del jefe solo estan sus anadidos, asi que no hace falta llevar la cuenta aparte.
    private static int AliveBasicEnemies()
    {
        int alive = 0;
        foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if (!e.IsDead) alive++;
        return alive;
    }
}
