using UnityEngine;

public static class SpawnAreaUtils
{
    public static Vector2 GetRandomPoint(BoxCollider2D area, GameObject prefab)
    {
        float margin = GetHalfExtent(prefab);
        Bounds bounds = area.bounds;

        float minX = bounds.min.x + margin;
        float maxX = bounds.max.x - margin;
        float minY = bounds.min.y + margin;
        float maxY = bounds.max.y - margin;

        if (minX > maxX) minX = maxX = bounds.center.x;
        if (minY > maxY) minY = maxY = bounds.center.y;

        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
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
