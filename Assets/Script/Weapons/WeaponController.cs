using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform firePointLeft;
    [SerializeField] private Transform firePointRight;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private PlayerAutoOrientation2D autoOrientation;

    [Header("Stats")]
    [SerializeField] private float fireRate = 5f; // balas por segundo

    [Header("Teclado (PC)")]
    [Tooltip("En PC la nave local solo dispara mientras se mantiene esta tecla. En movil el disparo sigue siendo automatico.")]
    [SerializeField] private KeyCode fireKey = KeyCode.Space;
    [SerializeField] private bool requireFireKeyOnDesktop = true;
    private PlayerController localController;

    private float nextShotTime;
    private Collider2D ownerCollider;
    private Sprite bulletSprite;
    private float bulletScale = 1f;

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

        localController = GetComponent<PlayerController>();
    }

    // La nave del otro jugador en cooperativo tiene el PlayerController apagado (PlayerSync):
    // su disparo no depende del teclado de este cliente.
    private bool WaitsForFireKey =>
        requireFireKeyOnDesktop && !Application.isMobilePlatform &&
        localController != null && localController.enabled;

    private void Update()
    {
        if (firePoint == null || bulletPrefab == null || autoOrientation == null) return;
        if (autoOrientation.CurrentTarget == null) return;
        if (WaitsForFireKey && !Input.GetKey(fireKey)) return;
        if (Time.time < nextShotTime) return;

        float currentFireRate = fireRate * (heavyWeaponActive ? heavyWeaponFireRateMultiplier : 1f);
        nextShotTime = Time.time + (1f / currentFireRate);

        if (tripleShotActive)
            FireTripleShot();
        else
            FireSingleShot(autoOrientation.CurrentAimDirection, firePoint.position);
    }

    private void FireSingleShot(Vector2 direction, Vector3 spawnPos)
    {
        Bullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.IgnoreCollider(ownerCollider);
        bullet.Init(direction);

        if (bulletSprite != null)
        {
            var spriteRenderer = bullet.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sprite = bulletSprite;
        }

        bullet.transform.localScale = bulletPrefab.transform.localScale * bulletScale;

        SFXManager.Instance?.PlayPlayerShoot();

        if (heavyWeaponActive)
            bullet.damage += heavyWeaponDamageBonus;
    }

    private void FireTripleShot()
    {
        Vector2 baseDir = autoOrientation.CurrentAimDirection;

        FireSingleShot(RotateDirection(baseDir, -tripleShotSpreadAngle), firePointLeft.position);
        FireSingleShot(baseDir, firePoint.position);
        FireSingleShot(RotateDirection(baseDir, tripleShotSpreadAngle), firePointRight.position);
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

    public void SetBulletSprite(Sprite sprite, float scale)
    {
        if (sprite != null) bulletSprite = sprite;
        bulletScale = scale;
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