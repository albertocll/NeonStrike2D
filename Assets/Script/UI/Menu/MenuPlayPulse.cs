using UnityEngine;

public class MenuPlayPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseAmount = 0.03f;

    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.unscaledTime * pulseSpeed);
        float scaleMultiplier = 1f + (wave * pulseAmount);
        transform.localScale = baseScale * scaleMultiplier;
    }
}