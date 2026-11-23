using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// 1.0.2: el auto apuntado solo elige enemigos vivos, dentro de la arena y a la vista. Cerca de una
// esquina, el enemigo que aun esta entrando por fuera del muro no cuenta (antes la nave disparaba
// en diagonal contra el muro).
public class PlayerAutoAimTests
{
    // Arena como la de los niveles: hacia (1033, 527), con el interior de 1020 a 1046 y de 520 a 534.
    private static readonly Vector2 Center = new Vector2(1033f, 527f);
    private const float HalfWidth = 13f, HalfHeight = 7f;

    private readonly List<GameObject> created = new List<GameObject>();
    private PlayerAutoOrientation2D aim;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Wall("Wall_Left", new Vector2(Center.x - HalfWidth - 0.5f, Center.y), new Vector2(1f, 2f * HalfHeight + 2f));
        Wall("Wall_Right", new Vector2(Center.x + HalfWidth + 0.5f, Center.y), new Vector2(1f, 2f * HalfHeight + 2f));
        Wall("Wall_Bottom", new Vector2(Center.x, Center.y - HalfHeight - 0.5f), new Vector2(2f * HalfWidth + 2f, 1f));
        Wall("Wall_Top", new Vector2(Center.x, Center.y + HalfHeight + 0.5f), new Vector2(2f * HalfWidth + 2f, 1f));
        yield return null;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in created)
            if (go != null) Object.Destroy(go);
        created.Clear();
    }

    private GameObject Track(GameObject go)
    {
        created.Add(go);
        return go;
    }

    private void Wall(string name, Vector2 position, Vector2 size)
    {
        var go = Track(new GameObject(name));
        go.transform.position = position;
        go.AddComponent<BoxCollider2D>().size = size;
    }

    private GameObject Enemy(string name, Vector2 position)
    {
        var go = Track(new GameObject(name));
        go.layer = LayerMask.NameToLayer("Enemy");
        go.transform.position = position;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        go.AddComponent<EnemyController>();
        return go;
    }

    private void Player(Vector2 position)
    {
        var go = Track(new GameObject("Jugador"));
        go.transform.position = position;
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 0.5f);
        aim = go.AddComponent<PlayerAutoOrientation2D>();
        typeof(PlayerAutoOrientation2D)
            .GetField("enemyLayer", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(aim, (LayerMask)(1 << LayerMask.NameToLayer("Enemy")));
    }

    private static IEnumerator Settle()
    {
        yield return new WaitForFixedUpdate();
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator EnLaEsquina_NoApuntaAlEnemigoDeFueraDelMuro()
    {
        // La nave en la esquina de abajo a la izquierda. Fuera del muro izquierdo, a 3 unidades, un
        // enemigo que aun esta entrando; dentro de la arena, uno mas lejos.
        Player(new Vector2(1021.5f, 521.5f));
        Enemy("Fuera", new Vector2(1018.5f, 521.5f));
        var inside = Enemy("Dentro", new Vector2(1030f, 527f));

        yield return Settle();

        Assert.AreSame(inside.transform, aim.CurrentTarget);
    }

    [UnityTest]
    public IEnumerator SoloEnemigosFueraDeLaArena_NoHayObjetivo()
    {
        Player(new Vector2(1021.5f, 521.5f));
        Enemy("FueraIzquierda", new Vector2(1018.5f, 521.5f));
        Enemy("FueraAbajo", new Vector2(1022f, 518.5f));

        yield return Settle();

        Assert.IsNull(aim.CurrentTarget, "Sin enemigos validos no hay objetivo, asi que no se dispara al vacio.");
    }

    [UnityTest]
    public IEnumerator ConMuroEnMedio_EligeElQueSeVe()
    {
        Player(new Vector2(1025f, 527f));
        Enemy("Tapado", new Vector2(1029f, 527f));
        Wall("Obstaculo", new Vector2(1027f, 527f), new Vector2(0.5f, 3f));
        var visible = Enemy("Visible", new Vector2(1025f, 532f));

        yield return Settle();

        Assert.AreSame(visible.transform, aim.CurrentTarget);
    }

    [UnityTest]
    public IEnumerator EnemigoMuerto_NoEsObjetivo()
    {
        Player(new Vector2(1030f, 527f));
        var dead = Enemy("Muerto", new Vector2(1032f, 527f));
        var alive = Enemy("Vivo", new Vector2(1036f, 527f));
        typeof(EnemyController)
            .GetField("isDead", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(dead.GetComponent<EnemyController>(), true);

        yield return Settle();

        Assert.AreSame(alive.transform, aim.CurrentTarget);
    }

    [UnityTest]
    public IEnumerator SiElObjetivoMuere_SeSueltaEnElMismoFotograma()
    {
        Player(new Vector2(1030f, 527f));
        var only = Enemy("Unico", new Vector2(1033f, 527f));

        yield return Settle();
        Assert.AreSame(only.transform, aim.CurrentTarget);

        typeof(EnemyController)
            .GetField("isDead", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(only.GetComponent<EnemyController>(), true);
        yield return null;

        Assert.IsNull(aim.CurrentTarget);
    }
}
