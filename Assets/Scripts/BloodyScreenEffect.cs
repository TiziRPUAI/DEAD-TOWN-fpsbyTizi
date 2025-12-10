using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BloodyScreenEffect : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private Image image;

    [Header("Parámetros de efecto")]
    [Tooltip("Alfa máximo del efecto (0 = invisible, 1 = opaco)")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.8f;

    [Tooltip("Duración del fade in en segundos")]
    public float fadeInDuration = 0.1f;

    [Tooltip("Tiempo que se mantiene la opacidad máxima")]
    public float holdDuration = 0.15f;

    [Tooltip("Duración del fade out en segundos")]
    public float fadeOutDuration = 0.4f;

    private Coroutine runningCoroutine;

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>() ?? GetComponentInChildren<Image>();

        if (image == null)
        {
            Debug.LogWarning($"{nameof(BloodyScreenEffect)}: no se encontró Image en el GameObject. Se intentará obtener al triggerear.");
            // NO desactivamos el componente para permitir inicializarlo más tarde desde Trigger.
        }
        else
        {
            // Asegurar que empieza invisible
            SetAlpha(0f);
            image.raycastTarget = false; // no bloquear input UI
        }
    }

    // Llama a este método para iniciar el efecto. severity en [0,1] (por ejemplo damage/maxHealth)
    public void Trigger(float severity)
    {
        // Asegurar referencia al Image si Awake no la inicializó (por GameObject inactivo en el inicio)
        if (image == null)
            image = GetComponent<Image>() ?? GetComponentInChildren<Image>();

        if (image == null)
        {
            Debug.LogWarning($"{nameof(BloodyScreenEffect)}.Trigger: no hay Image para mostrar el overlay.");
            return;
        }

        float clamped = Mathf.Clamp01(severity);
        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);
        runningCoroutine = StartCoroutine(Play(clamped));
    }

    private IEnumerator Play(float severity)
    {
        if (image == null) yield break;

        float targetAlpha = Mathf.Clamp01(severity) * maxAlpha;

        // Fade in
        yield return FadeTo(targetAlpha, fadeInDuration);

        // Hold
        float t = 0f;
        while (t < holdDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // Fade out
        yield return FadeTo(0f, fadeOutDuration);

        runningCoroutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float startAlpha = image.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
            SetAlpha(a);
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float a)
    {
        Color c = image.color;
        c.a = Mathf.Clamp01(a);
        image.color = c;
    }
}