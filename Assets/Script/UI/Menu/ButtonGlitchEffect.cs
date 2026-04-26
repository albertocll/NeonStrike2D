using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonGlitchEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color[] glitchColors = new Color[]
    {
        new Color(0f, 0.898f, 1f),    // #00e5ff
        new Color(0.784f, 0.314f, 0.753f), // #c850c0
        new Color(0.482f, 0.184f, 1f)  // #7b2fff
    };
    [SerializeField] private float glitchInterval = 0.05f;

    private Coroutine _glitchCoroutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        _glitchCoroutine = StartCoroutine(GlitchLoop());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_glitchCoroutine != null)
            StopCoroutine(_glitchCoroutine);
        label.color = normalColor;
    }

    private IEnumerator GlitchLoop()
    {
        while (true)
        {
            label.color = glitchColors[Random.Range(0, glitchColors.Length)];
            yield return new WaitForSeconds(glitchInterval);
        }
    }
}