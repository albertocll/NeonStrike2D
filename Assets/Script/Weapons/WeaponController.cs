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

    private bool heavyWeaponActive;
    private float heavyWeaponFireRateMultiplier = 1f;
    private int heavyWeaponDamageBonus;

    private bool tripleShotActive;
    private float tripleShotSpreadAngle;

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

        float currentFireRate = fireRate * (heavyWeaponActive ? heavyWeaponFireRateMultiplier : 1f);
        nextShotTime = Time.time + (1f / currentFireRate);

        if (tripleShotActive)
            FireTripleShot();
        else
            FireSingleShot(autoOrientation.CurrentAimDirection);
    }

    private void FireSingleShot(Vector2 direction)
    {
        Bullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.IgnoreCollider(ownerCollider);
        bullet.Init(direction);

        if (heavyWeaponActive)
            bullet.damage += heavyWeaponDamageBonus;
    }

    private void FireTripleShot()
    {
        Vector2 baseDir = autoOrientation.CurrentAimDirection;

        FireSingleShot(RotateDirection(baseDir, -tripleShotSpreadAngle));
        FireSingleShot(baseDir);
        FireSingleShot(RotateDirection(baseDir, tripleShotSpreadAngle));
    }

    private Vector2 RotateDirection(Vector2 direction, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        );
    }

    public void SetHeavyWeapon(bool active, float fireRateMultiplier, int damageBonus)
    {
        heavyWeaponActive = active;
        heavyWeaponFireRateMultiplier = fireRateMultiplier;
        heavyWeaponDamageBonus = damageBonus;
    }

    public void SetTripleShot(bool active, float spreadAngle)
    {
        tripleShotActive = active;
        tripleShotSpreadAngle = spreadAngle;
    }
}