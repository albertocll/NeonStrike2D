using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private PlayerAutoOrientation2D autoOrientation;

    [Header("Stats")]
    [SerializeField] private float fireRate = 5f; // balas por segundo

    private float nextShotTime;
    private Collider2D ownerCollider;

    private void Awake()
    {
        ownerCollider = GetComponent<Collider2D>();

        if (autoOrientation == null)
            autoOrientation = GetComponent<PlayerAutoOrientation2D>();
    }

    private void Update()
    {
        if (firePoint == null || bulletPrefab == null || autoOrientation == null) return;
        if (autoOrientation.CurrentTarget == null) return;
        if (Time.time < nextShotTime) return;

        nextShotTime = Time.time + (1f / fireRate);

        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.IgnoreCollider(ownerCollider);
        bullet.Init(autoOrientation.CurrentAimDirection);
    }
}