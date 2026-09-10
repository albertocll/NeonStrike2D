using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerPowerUps : MonoBehaviour
{
    [Header("Heavy Weapon")]
    [SerializeField] private float heavyWeaponFireRateMultiplier = 2.5f;
    [SerializeField] private int heavyWeaponDamageBonus = 2;

    [Header("Triple Shot")]
    [SerializeField] private float tripleShotSpreadAngle = 15f;

    [Header("Speed Boost")]
    [SerializeField] private float speedBoostMultiplier = 1.6f;

    [Header("Visual Feedback")]
    [SerializeField] private Color shieldTintColor = new Color(0.4f, 0.9f, 1f, 1f);
    [SerializeField] private Color heavyWeaponTintColor = new Color(1f, 0.5f, 0.2f, 1f);
    [SerializeField] private Color tripleShotTintColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color speedBoostTintColor = new Color(0.4f, 1f, 0.5f, 1f);

    private PlayerController controller;
    private WeaponController weapon;
    private PlayerHealth health;
    private SpriteRenderer spriteRenderer;
    private Color baseSpriteColor = Color.white;

    private bool shieldActive;
    private float shieldTimer;

    private bool heavyWeaponActive;
    private float heavyWeaponTimer;

    private bool tripleShotActive;
    private float tripleShotTimer;

    private bool speedBoostActive;
    private float speedBoostTimer;

    public bool IsShieldActive => shieldActive;
    public bool IsHeavyWeaponActive => heavyWeaponActive;
    public bool IsTripleShotActive => tripleShotActive;
    public bool IsSpeedBoostActive => speedBoostActive;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        weapon = GetComponentInChildren<WeaponController>();
        health = GetComponent<PlayerHealth>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            baseSpriteColor = spriteRenderer.color;
    }

    private void Update()
    {
        if (shieldActive)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f)
                EndShield();
        }

        if (heavyWeaponActive)
        {
            heavyWeaponTimer -= Time.deltaTime;
            if (heavyWeaponTimer <= 0f)
                EndHeavyWeapon();
        }

        if (tripleShotActive)
        {
            tripleShotTimer -= Time.deltaTime;
            if (tripleShotTimer <= 0f)
                EndTripleShot();
        }

        if (speedBoostActive)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
                EndSpeedBoost();
        }

        UpdateTintColor();
    }

    public void ApplyPowerUp(PowerUp.PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUp.PowerUpType.Shield:
                StartShield(duration);
                break;
            case PowerUp.PowerUpType.HeavyWeapon:
                StartHeavyWeapon(duration);
                break;
            case PowerUp.PowerUpType.TripleShot:
                StartTripleShot(duration);
                break;
            case PowerUp.PowerUpType.SpeedBoost:
                StartSpeedBoost(duration);
                break;
        }
    }

    private void StartShield(float duration)
    {
        shieldActive = true;
        shieldTimer = duration;
        if (health != null)
            health.SetInvulnerable(true);
    }

    private void EndShield()
    {
        shieldActive = false;
        if (health != null)
            health.SetInvulnerable(false);
    }

    private void StartHeavyWeapon(float duration)
    {
        heavyWeaponActive = true;
        heavyWeaponTimer = duration;
        if (weapon != null)
            weapon.SetHeavyWeapon(true, heavyWeaponFireRateMultiplier, heavyWeaponDamageBonus);
    }

    private void EndHeavyWeapon()
    {
        heavyWeaponActive = false;
        if (weapon != null)
            weapon.SetHeavyWeapon(false, 1f, 0);
    }

    private void StartTripleShot(float duration)
    {
        tripleShotActive = true;
        tripleShotTimer = duration;
        if (weapon != null)
            weapon.SetTripleShot(true, tripleShotSpreadAngle);
    }

    private void EndTripleShot()
    {
        tripleShotActive = false;
        if (weapon != null)
            weapon.SetTripleShot(false, 0f);
    }

    private void StartSpeedBoost(float duration)
    {
        speedBoostActive = true;
        speedBoostTimer = duration;
        if (controller != null)
            controller.SetSpeedMultiplier(speedBoostMultiplier);
    }

    private void EndSpeedBoost()
    {
        speedBoostActive = false;
        if (controller != null)
            controller.SetSpeedMultiplier(1f);
    }

    private void UpdateTintColor()
    {
        if (spriteRenderer == null) return;

        if (shieldActive)
            spriteRenderer.color = shieldTintColor;
        else if (heavyWeaponActive)
            spriteRenderer.color = heavyWeaponTintColor;
        else if (tripleShotActive)
            spriteRenderer.color = tripleShotTintColor;
        else if (speedBoostActive)
            spriteRenderer.color = speedBoostTintColor;
        else
            spriteRenderer.color = baseSpriteColor;
    }
}