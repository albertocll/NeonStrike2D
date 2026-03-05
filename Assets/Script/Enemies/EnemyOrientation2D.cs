using UnityEngine;

public class EnemyOrientation2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visual;     // child "Visual"
    [SerializeField] private Transform target;     // Player

    [Header("Facing")]
    [SerializeField] private bool invertFacing = true; // <- IMPORTANTe: invierte el sentido

    [Header("Tuning")]
    [SerializeField] private float xDeadzone = 0.05f;
    [SerializeField] private float minFlipInterval = 0.08f;

    private bool facingRight = true;
    private float lastFlipTime;

    void Awake()
    {
        if (visual == null)
        {
            var v = transform.Find("Visual");
            if (v != null) visual = v;
        }

        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (visual == null || target == null) return;

        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) <= xDeadzone) return;

        SetFacingFromX(dx);
    }

    public void SetFacingFromX(float xDir)
    {
        if (Time.time - lastFlipTime < minFlipInterval) return;

        bool wantRight = xDir > 0f;
        if (invertFacing) wantRight = !wantRight;

        if (wantRight == facingRight) return;

        facingRight = wantRight;
        lastFlipTime = Time.time;

        Vector3 s = visual.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1f : -1f);
        visual.localScale = s;
    }
}