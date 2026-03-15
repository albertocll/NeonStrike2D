using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    public Transform firePoint;
    public Bullet bulletPrefab;
    public PlayerAutoOrientation2D autoOrientation;

    [Header("Stats")]
    public float fireRate = 10f; // balas por segundo

    private float nextShotTime;
    private Collider2D ownerCollider;

    void Awake()
    {
        ownerCollider = GetComponent<Collider2D>();

        if (autoOrientation == null)
        {
            autoOrientation = GetComponent<PlayerAutoOrientation2D>();
        }
    }

    void Update()
    {
        if (!firePoint || !bulletPrefab || !autoOrientation) return;
        if (autoOrientation.CurrentTarget == null) return;

        if (Time.time < nextShotTime) return;
        nextShotTime = Time.time + (1f / fireRate);

        Bullet b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // evita que se destruya nada más nacer por tocar al player
        b.IgnoreCollider(ownerCollider);

        // dispara hacia la dirección calculada por el auto orientation
        b.Init(autoOrientation.CurrentAimDirection);
    }
}