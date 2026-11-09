using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("Enemigos de niveles anteriores (salen en menor proporcion).")]
    [SerializeField] private List<GameObject> basicEnemyPrefabs;
    [Tooltip("Enemigos nuevos del nivel (salen en mayor proporcion).")]
    [SerializeField] private List<GameObject> advancedEnemyPrefabs;
    [SerializeField] private int advancedEnemyFromWave = 3;
    [Tooltip("Probabilidad de que cada enemigo de la oleada sea de la lista de avanzados.")]
    [SerializeField, Range(0f, 1f)] private float advancedEnemyChance = 0.3f;

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider2D spawnArea;
    [SerializeField] private RectTransform[] uiExclusionZones;

    [Header("Entrada desde fuera de la arena")]
    [Tooltip("Velocidad (unidades/s) a la que los enemigos cruzan el muro hasta entrar en la arena.")]
    [SerializeField] private float entrySpeed = 3f;
    [Tooltip("Distancia extra por fuera de los Wall_* a la que aparecen los enemigos.")]
    [SerializeField] private float outsideMargin = 0.5f;

    private const float ExclusionPadding = 0.5f;
    private const string WallPrefix = "Wall_";

    private Bounds? outerBounds;

    // El jefe aparece fuera de los muros y de la camara, por un lado al azar como el resto de
    // enemigos, y BossSentinelController lo hace entrar hasta el centro de la arena.
    public void SpawnBoss(GameObject bossPrefab, WaveManager waveManager)
    {
        Bounds arena = spawnArea.bounds;
        GameObject bossInstance = Instantiate(bossPrefab, arena.center, Quaternion.identity);

        float half = SpawnAreaUtils.GetHalfExtent(bossPrefab);
        Vector2 halfSize = new Vector2(half, half);
        var bossRenderer = bossInstance.GetComponentInChildren<SpriteRenderer>();
        if (bossRenderer != null) halfSize = Vector2.Max(halfSize, bossRenderer.bounds.extents);

        // Rectangulo del que tiene que quedar fuera: los muros y, si es mayor, la vista de la camara.
        Bounds outer = GetOuterBounds();
        Vector2 min = outer.min, max = outer.max;
        Camera cam = Camera.main;
        if (cam != null)
        {
            float z = Mathf.Abs(cam.transform.position.z);
            min = Vector2.Min(min, cam.ViewportToWorldPoint(new Vector3(0f, 0f, z)));
            max = Vector2.Max(max, cam.ViewportToWorldPoint(new Vector3(1f, 1f, z)));
        }

        float x = Random.Range(arena.min.x, arena.max.x);
        float y = Random.Range(arena.min.y, arena.max.y);
        Vector2 start;
        switch (Random.Range(0, 4))
        {
            case 0: start = new Vector2(min.x - halfSize.x - outsideMargin, y); break; // izquierda
            case 1: start = new Vector2(max.x + halfSize.x + outsideMargin, y); break; // derecha
            case 2: start = new Vector2(x, max.y + halfSize.y + outsideMargin); break; // arriba
            default: start = new Vector2(x, min.y - halfSize.y - outsideMargin); break; // abajo
        }

        bossInstance.transform.position = start;
        bossInstance.GetComponent<BossSentinelController>()?.SetArena(arena);

        EnemyWaveMember waveMember = bossInstance.GetComponent<EnemyWaveMember>();
        if (waveMember != null)
            waveMember.Initialize(waveManager);
    }

    public void SpawnWave(int enemyCount, WaveManager waveManager, int currentWave)
    {
        StartCoroutine(SpawnWaveRoutine(enemyCount, waveManager, currentWave));
    }

    // ponytail: 1 Instantiate por frame evita el hitch de spawnear toda la oleada
    // de golpe. Si una oleada gigante (p.ej. 30+) tardara demasiado en completarse,
    // subir a 2-3 por frame en vez de 1.
    private IEnumerator SpawnWaveRoutine(int enemyCount, WaveManager waveManager, int currentWave)
    {
        LevelData level = LevelManager.ForScene(gameObject.scene.name);
        float healthMultiplier = level != null ? level.enemyHealthMultiplier : 1f;

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyPrefab = ChooseEnemyPrefab(currentWave);

            // Un Swarm trae su racimo: cuenta como groupSize enemigos de la oleada.
            var swarm = enemyPrefab.GetComponent<SwarmAI>();
            if (swarm != null) i += swarm.GroupSize - 1;
            float halfExtent = SpawnAreaUtils.GetHalfExtent(enemyPrefab);
            Vector2 destination = GetRandomPosition(enemyPrefab);
            Vector2 spawnPosition = GetOutsidePosition(halfExtent);

            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            if (enemyInstance.TryGetComponent(out EnemyController controller))
                controller.ScaleHealth(healthMultiplier);
            enemyInstance.AddComponent<EnemyArenaEntry>().Begin(destination, spawnArea.bounds, halfExtent, entrySpeed);

            EnemyWaveMember waveMember = enemyInstance.GetComponent<EnemyWaveMember>();

            if (waveMember != null)
            {
                waveMember.Initialize(waveManager);
            }

            yield return null;
        }
    }

    public GameObject ChooseEnemyPrefab(int currentWave)
    {
        if (currentWave >= advancedEnemyFromWave && advancedEnemyPrefabs.Count > 0 && Random.value < advancedEnemyChance)
            return advancedEnemyPrefabs[Random.Range(0, advancedEnemyPrefabs.Count)];
        return basicEnemyPrefabs[Random.Range(0, basicEnemyPrefabs.Count)];
    }

    // Punto por fuera de los Wall_*, en un lado al azar y en un punto al azar de ese lado
    // (dentro del rango de la arena, para que el camino hasta dentro solo cruce ese muro).
    private Vector2 GetOutsidePosition(float halfExtent)
    {
        Bounds inner = spawnArea.bounds;
        Bounds outer = GetOuterBounds();
        float offset = halfExtent + outsideMargin;

        float x = Random.Range(inner.min.x, inner.max.x);
        float y = Random.Range(inner.min.y, inner.max.y);

        switch (Random.Range(0, 4))
        {
            case 0: return new Vector2(outer.min.x - offset, y); // izquierda
            case 1: return new Vector2(outer.max.x + offset, y); // derecha
            case 2: return new Vector2(x, outer.max.y + offset); // arriba
            default: return new Vector2(x, outer.min.y - offset); // abajo
        }
    }

    // Rectangulo exterior de los Wall_* de la escena. Sin muros, el SpawnArea ampliado 1 unidad.
    private Bounds GetOuterBounds()
    {
        if (outerBounds.HasValue) return outerBounds.Value;

        Bounds result = spawnArea.bounds;
        result.Expand(2f);
        bool found = false;

        foreach (var col in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (!col.name.StartsWith(WallPrefix) || col.gameObject.scene != gameObject.scene) continue;
            if (!found) { result = col.bounds; found = true; }
            else result.Encapsulate(col.bounds);
        }

        outerBounds = result;
        return result;
    }

    private Vector2 GetRandomPosition(GameObject enemyPrefab)
    {
        Vector2 candidate = Vector2.zero;
        for (int attempt = 0; attempt < SpawnAreaUtils.MaxSpawnAttempts; attempt++)
        {
            candidate = SpawnAreaUtils.GetRandomPoint(spawnArea, enemyPrefab);
            if (!IsInsideExclusionZone(candidate) && !SpawnAreaUtils.OverlapsSpawnable(candidate, enemyPrefab))
                return candidate;
        }

        return candidate;
    }

    private bool IsInsideExclusionZone(Vector2 point)
    {
        if (uiExclusionZones == null || uiExclusionZones.Length == 0) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        float z = -cam.transform.position.z;
        Vector3[] corners = new Vector3[4];

        foreach (RectTransform zone in uiExclusionZones)
        {
            if (zone == null) continue;

            zone.GetWorldCorners(corners);
            Vector3 min = cam.ScreenToWorldPoint(new Vector3(corners[0].x, corners[0].y, z));
            Vector3 max = cam.ScreenToWorldPoint(new Vector3(corners[2].x, corners[2].y, z));

            if (point.x >= min.x - ExclusionPadding && point.x <= max.x + ExclusionPadding &&
                point.y >= min.y - ExclusionPadding && point.y <= max.y + ExclusionPadding)
            {
                return true;
            }
        }

        return false;
    }
}