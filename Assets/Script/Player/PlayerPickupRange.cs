using UnityEngine;

// Zona de recogida de consumibles y potenciadores, aparte del collider del casco (que es el
// que reciben las balas y los enemigos). Es un trigger hijo, sin la etiqueta Player, asi que
// lo que dana al jugador lo ignora; Collectible y PowerUp lo buscan por este componente.
public class PlayerPickupRange : MonoBehaviour
{
    // Radio respecto a la mitad del lado mayor del sprite: un poco mas grande que el dibujo.
    private const float RadiusFactor = 1.2f;

    public static PlayerPickupRange Create(GameObject player)
    {
        var go = new GameObject("PickupRange");
        go.layer = player.layer;
        go.transform.SetParent(player.transform, false);

        var sr = player.GetComponent<SpriteRenderer>();
        Vector2 size = sr != null && sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = Mathf.Max(size.x, size.y) * 0.5f * RadiusFactor;

        return go.AddComponent<PlayerPickupRange>();
    }

    // El jugador que recoge: su collider de casco (Player) o esta zona de recogida.
    public static GameObject PlayerOf(Collider2D other)
    {
        if (other.CompareTag("Player")) return other.gameObject;
        var range = other.GetComponent<PlayerPickupRange>();
        return range != null ? range.transform.parent.gameObject : null;
    }
}
