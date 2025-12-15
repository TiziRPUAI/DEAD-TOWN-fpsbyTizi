using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeathScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image blackPanel; // full-screen black Image
    [SerializeField] private TextMeshProUGUI messageText; // "YOU DIED"

    [Header("Timing")]
    public float panelFadeDuration = 1.0f;
    public float messageDelay = 0.5f;
    public float messageFadeDuration = 0.8f;

    [Header("Canvas sorting")]
    [Tooltip("Sorting order usado por este Canvas para forzar que esté encima")]
    public int sortingOrder = 100;

    private Coroutine running;
    private Canvas _canvas;

    private void Awake()
    {
        // Try to auto-find if not assigned
        if (blackPanel == null)
            blackPanel = GetComponent<Image>() ?? GetComponentInChildren<Image>(true);

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        // Ensure starting invisible
        if (blackPanel != null)
            SetGraphicAlpha(blackPanel, 0f);
        if (messageText != null)
            SetGraphicAlpha(messageText, 0f);

        // Ensure a Canvas exists on this GameObject and that it overrides sorting
        _canvas = GetComponent<Canvas>();
        if (_canvas == null)
        {
            _canvas = gameObject.AddComponent<Canvas>();
            // Añadir GraphicRaycaster para bloquear input sobre otros elementos
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        _canvas.overrideSorting = true;
        _canvas.sortingOrder = sortingOrder;
    }

    // Llama para mostrar el blackout + texto
    public void Show()
    {
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        if (blackPanel == null)
            yield break;

        
        yield return StartCoroutine(FadeGraphic(blackPanel, blackPanel.color.a, 1f, panelFadeDuration));

       
        if (messageText != null)
        {
            yield return new WaitForSeconds(messageDelay);
            yield return StartCoroutine(FadeGraphic(messageText, messageText.color.a, 1f, messageFadeDuration));
        }

        running = null;
    }

    private IEnumerator FadeGraphic(Graphic g, float from, float to, float duration)
    {
        if (g == null)
            yield break;
        if (duration <= 0f)
        {
            SetGraphicAlpha(g, to);
            yield break;
        }

        float t = 0f;
        SetGraphicAlpha(g, from);
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetGraphicAlpha(g, a);
            yield return null;
        }
        SetGraphicAlpha(g, to);
    }

    private void SetGraphicAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color;
        c.a = Mathf.Clamp01(a);
        g.color = c;
        if (g == blackPanel)
            blackPanel.raycastTarget = c.a > 0.01f;
    }
}
