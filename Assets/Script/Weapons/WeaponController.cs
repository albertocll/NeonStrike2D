using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;
    public Bullet bulletPrefab;

    [Header("Stats")]
    public float fireRate = 10f; // balas por segundo

    private float nextShotTime;
    private Collider2D ownerCollider;

    void Awake()
    {
        ownerCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (!firePoint || !bulletPrefab) return;
        if (!Input.GetKey(KeyCode.Space)) return;

        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + (1f / fireRate);

        Bullet b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // evita que se destruya nada más nacer por tocar al player
        b.IgnoreCollider(ownerCollider);

        // dispara hacia donde apunta el firePoint
        b.Init(firePoint.right);
    }
}
