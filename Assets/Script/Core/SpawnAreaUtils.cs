using UnityEngine;

public static class SpawnAreaUtils
{
    public const int MaxSpawnAttempts = 10;
    private const float OverlapPadding = 0.3f;

    // ponytail: comprobacion de solape compartida por los 3 spawners (enemigos,
    // coleccionables, power-ups) para que nada nazca encima de otra cosa ya
    // presente en el area. Power-ups/coleccionables van Untagged, se detectan
    // por componente en vez de por tag.
    public static bool OverlapsSpawnable(Vector2 point, GameObject prefab)
    {
        float radius = GetHalfExtent(prefab) + OverlapPadding;
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy")) return true;
            if (hit.GetComponent<PowerUp>() != null) return true;
            if (hit.GetComponent<Collectible>() != null) return true;
        }

        return false;
    }

    public static Vector2 GetRandomPoint(BoxCollider2D area, GameObject prefab)
    {
        float margin = GetHalfExtent(prefab) + GetExtraMargin(area);
        Bounds bounds = area.bounds;

        float minX = bounds.min.x + margin;
        float maxX = bounds.max.x - margin;
        float minY = bounds.min.y + margin;
        float maxY = bounds.max.y - margin;

        if (minX > maxX) minX = maxX = bounds.center.x;
        if (minY > maxY) minY = maxY = bounds.center.y;

        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    private static float GetExtraMargin(BoxCollider2D area)
    {
        var margin = area.GetComponent<SpawnAreaMargin>();
        return margin != null ? margin.ExtraMargin : 0f;
    }

    private static float GetHalfExtent(GameObject prefab)
    {
        float scale = Mathf.Max(prefab.transform.localScale.x, prefab.transform.localScale.y);

        var box = prefab.GetComponent<BoxCollider2D>();
        if (box != null)
            return Mathf.Max(box.size.x, box.size.y) * 0.5f * scale;

        var circle = prefab.GetComponent<CircleCollider2D>();
        if (circle != null)
            return circle.radius * scale;

        var capsule = prefab.GetComponent<CapsuleCollider2D>();
        if (capsule != null)
            return Mathf.Max(capsule.size.x, capsule.size.y) * 0.5f * scale;

        return 0f;
    }
}
