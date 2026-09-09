using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Carrusel de imágenes para SpriteRenderers.
/// Asigna uno o varios SpriteRenderer (cada uno con su imagen) y el componente
/// las va mostrando una a una con una transición de fade cada X segundos.
/// Solo modifica el alpha del color, nunca la escala, por lo que cada imagen
/// CONSERVA SU TAMAÑO original.
/// </summary>
public class CarruselSpritesFade : MonoBehaviour
{
    [Header("Imágenes (SpriteRenderers)")]
    [Tooltip("Arrastra aquí uno o más SpriteRenderer. Se irán mostrando en orden.")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    [Header("Tiempos")]
    [Tooltip("Segundos que cada imagen permanece visible.")]
    [Min(0f)]
    [SerializeField] private float tiempoVisible = 10f;

    [Tooltip("Duración del fade entre una imagen y la siguiente.")]
    [Min(0f)]
    [SerializeField] private float duracionFade = 1f;

    [Header("Opciones")]
    [Tooltip("Si está activo, al llegar a la última vuelve a la primera.")]
    [SerializeField] private bool enBucle = true;

    [Tooltip("Ignora la escala de tiempo (útil si el juego se pausa con Time.timeScale = 0).")]
    [SerializeField] private bool usarTiempoNoEscalado = false;

    // Solo los renderers válidos (no nulos), en orden.
    private readonly List<SpriteRenderer> renderersValidos = new List<SpriteRenderer>();
    private Coroutine rutina;
    private int indiceActual;

    private void OnEnable()
    {
        RecolectarRenderers();

        if (renderersValidos.Count == 0)
        {
            Debug.LogWarning("CarruselSpritesFade: no se asignó ningún SpriteRenderer.", this);
            return;
        }

        // Muestra la primera y oculta el resto, sin tocar el tamaño.
        indiceActual = 0;
        for (int i = 0; i < renderersValidos.Count; i++)
            SetAlpha(renderersValidos[i], i == indiceActual ? 1f : 0f);

        // Con una sola imagen no hay nada que rotar.
        if (renderersValidos.Count > 1)
            rutina = StartCoroutine(RutinaCarrusel());
    }

    private void OnDisable()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }
    }

    private void RecolectarRenderers()
    {
        renderersValidos.Clear();

        if (spriteRenderers == null) return;

        foreach (SpriteRenderer sr in spriteRenderers)
            if (sr != null) renderersValidos.Add(sr);
    }

    private IEnumerator RutinaCarrusel()
    {
        while (true)
        {
            yield return Esperar(tiempoVisible);

            int siguiente = (indiceActual + 1) % renderersValidos.Count;

            // Si no es en bucle y ya dimos la vuelta completa, detente en la última.
            if (!enBucle && siguiente == 0)
                yield break;

            yield return Fade(renderersValidos[indiceActual], renderersValidos[siguiente]);

            indiceActual = siguiente;
        }
    }

    // Desvanece "saliente" (1 -> 0) mientras aparece "entrante" (0 -> 1) al mismo tiempo.
    private IEnumerator Fade(SpriteRenderer saliente, SpriteRenderer entrante)
    {
        if (duracionFade <= 0f)
        {
            SetAlpha(saliente, 0f);
            SetAlpha(entrante, 1f);
            yield break;
        }

        float t = 0f;

        while (t < duracionFade)
        {
            t += usarTiempoNoEscalado ? Time.unscaledDeltaTime : Time.deltaTime;
            float p = Mathf.Clamp01(t / duracionFade);

            SetAlpha(saliente, 1f - p);
            SetAlpha(entrante, p);

            yield return null;
        }

        SetAlpha(saliente, 0f);
        SetAlpha(entrante, 1f);
    }

    private IEnumerator Esperar(float segundos)
    {
        if (segundos <= 0f)
            yield break;

        if (usarTiempoNoEscalado)
            yield return new WaitForSecondsRealtime(segundos);
        else
            yield return new WaitForSeconds(segundos);
    }

    // Cambia SOLO el alpha, conservando el color y el tamaño del sprite.
    private void SetAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;

        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}
