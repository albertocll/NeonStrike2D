using System.Collections;
using UnityEngine;

// Specter (issue #188), jefe de la oleada 50. No se desplaza: se queda quieto un rato
// (vulnerable), se desvanece, reaparece en otro punto de la arena lejos del jugador y dispara
// una rafaga dirigida a el. Mientras se desvanece y reaparece es intangible: no recibe dano
// ni dana por contacto, y se ve semitransparente y parpadeando. No hay sprite de
// teletransporte: el efecto se hace con transparencia.
public class BossSpecterController : BossBase
{
    [Header("Ciclo")]
    [SerializeField] private float idleDuration = 2.5f;
    [SerializeField] private float fadeDuration = 0.45f;
    [Tooltip("Distancia minima al jugador del punto de reaparicion.")]
    [SerializeField] private float minDistanceFromPlayer = 4f;
    [SerializeField, Range(0f, 1f)] private float intangibleAlpha = 0.25f;

    [Header("Rafaga")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstInterval = 0.15f;
    [SerializeField] private int bulletDamage = 8;
    [Tooltip("Balas por disparo de la rafaga: 1 recta o 3 en abanico estrecho.")]
    [SerializeField] private int bulletsPerShot = 3;
    [SerializeField] private float shotSpread = 20f;

    protected override IEnumerator Fight()
    {
        while (!IsDead)
        {
            SetPose(idleSprite);
            yield return new WaitForSeconds(idleDuration);
            if (IsDead) yield break;

            // Desaparece (intangible).
            IsInvulnerable = true;
            SetPose(moveSprite);
            yield return Fade(1f, intangibleAlpha);
            rb.position = TeleportPoint();
            yield return Fade(intangibleAlpha, 1f);
            IsInvulnerable = false;
            SetAlpha(1f);
            SetPose(idleSprite);

            // Rafaga dirigida al jugador desde la nueva posicion.
            for (int i = 0; i < burstCount && !IsDead; i++)
            {
                ShootFan(bulletPrefab, DirectionToTarget(transform.position), bulletsPerShot, shotSpread, bulletDamage);
                yield return new WaitForSeconds(burstInterval);
            }
        }
    }

    // Punto al azar de la arena lejos del jugador (y de donde estaba).
    private Vector2 TeleportPoint()
    {
        Vector2 best = RandomArenaPoint();
        float bestScore = -1f;
        for (int i = 0; i < 12; i++)
        {
            Vector2 p = RandomArenaPoint();
            float toPlayer = target != null ? Vector2.Distance(p, target.position) : minDistanceFromPlayer;
            float moved = Vector2.Distance(p, rb.position);
            if (toPlayer >= minDistanceFromPlayer && moved >= 2f) return p;
            float score = toPlayer + moved * 0.5f;
            if (score > bestScore) { bestScore = score; best = p; }
        }
        return best;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeDuration && !IsDead; t += Time.deltaTime)
        {
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            // Parpadeo para que se note que no se le puede danar.
            if (Mathf.Repeat(t, 0.1f) < 0.05f) a *= 0.5f;
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        c.a = a;
        spriteRenderer.color = c;
    }
}
