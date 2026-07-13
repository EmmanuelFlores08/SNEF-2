using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BotonUIAnimado : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Elemento a animar")]
    [Tooltip("Déjalo vacío para animar el RectTransform de este mismo objeto.")]
    [SerializeField] private RectTransform objetivo;

    [Header("Tipo de botón")]
    [Tooltip("Actívalo únicamente en los botones que funcionan como tabs.")]
    [SerializeField] private bool funcionaComoTab = false;

    [Tooltip("Hace que la tab seleccionada se renderice encima de las demás.")]
    [SerializeField] private bool colocarTabActivaAlFrente = true;

    [Header("Desplazamientos")]
    [Tooltip("Cantidad que baja el botón cuando el cursor está encima.")]
    [SerializeField] private float desplazamientoHover = 4f;

    [Tooltip("Cantidad adicional que baja al mantener presionado.")]
    [SerializeField] private float desplazamientoPresionado = 2f;

    [Tooltip("Cantidad que sube una tab cuando está seleccionada.")]
    [SerializeField] private float desplazamientoTabActiva = 9f;

    [Header("Animación")]
    [SerializeField] private float duracionAnimacion = 0.12f;

    [SerializeField] private AnimationCurve curvaAnimacion =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector2 posicionBase;

    private bool estaSeleccionado;
    private bool punteroEncima;
    private bool estaPresionado;
    private bool inicializado;

    private Coroutine animacionActual;

    private void Awake()
    {
        Inicializar();
    }

    private void OnEnable()
    {
        Inicializar();

        punteroEncima = false;
        estaPresionado = false;

        AplicarPosicionInmediata();
    }

    private void Inicializar()
    {
        if (inicializado)
            return;

        if (objetivo == null)
            objetivo = transform as RectTransform;

        if (objetivo == null)
        {
            Debug.LogError(
                "BotonUIAnimado necesita estar en un objeto con RectTransform.",
                this
            );

            return;
        }

        posicionBase = objetivo.anchoredPosition;
        inicializado = true;
    }

    public void SetSeleccionado(bool seleccionado)
    {
        Inicializar();

        if (!funcionaComoTab)
            seleccionado = false;

        estaSeleccionado = seleccionado;

        if (estaSeleccionado && colocarTabActivaAlFrente)
            objetivo.SetAsLastSibling();

        AnimarHaciaEstadoActual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Inicializar();

        punteroEncima = true;
        AnimarHaciaEstadoActual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Inicializar();

        punteroEncima = false;
        estaPresionado = false;

        AnimarHaciaEstadoActual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Inicializar();

        estaPresionado = true;
        AnimarHaciaEstadoActual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Inicializar();

        estaPresionado = false;
        AnimarHaciaEstadoActual();
    }

    private Vector2 ObtenerPosicionObjetivo()
    {
        Vector2 nuevaPosicion = posicionBase;

        if (funcionaComoTab && estaSeleccionado)
            nuevaPosicion.y += desplazamientoTabActiva;

        if (punteroEncima)
            nuevaPosicion.y -= desplazamientoHover;

        if (estaPresionado)
            nuevaPosicion.y -= desplazamientoPresionado;

        return nuevaPosicion;
    }

    private void AnimarHaciaEstadoActual()
    {
        if (!inicializado || objetivo == null)
            return;

        if (animacionActual != null)
            StopCoroutine(animacionActual);

        animacionActual = StartCoroutine(
            AnimarPosicion(ObtenerPosicionObjetivo())
        );
    }

    private IEnumerator AnimarPosicion(Vector2 posicionFinal)
    {
        Vector2 posicionInicial = objetivo.anchoredPosition;

        if (duracionAnimacion <= 0f)
        {
            objetivo.anchoredPosition = posicionFinal;
            animacionActual = null;
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = Mathf.Clamp01(
                tiempo / duracionAnimacion
            );

            float progresoSuavizado =
                curvaAnimacion.Evaluate(progreso);

            objetivo.anchoredPosition = Vector2.LerpUnclamped(
                posicionInicial,
                posicionFinal,
                progresoSuavizado
            );

            yield return null;
        }

        objetivo.anchoredPosition = posicionFinal;
        animacionActual = null;
    }

    private void AplicarPosicionInmediata()
    {
        if (!inicializado || objetivo == null)
            return;

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
            animacionActual = null;
        }

        objetivo.anchoredPosition = ObtenerPosicionObjetivo();
    }

    private void OnDisable()
    {
        if (!inicializado || objetivo == null)
            return;

        punteroEncima = false;
        estaPresionado = false;

        if (animacionActual != null)
        {
            StopCoroutine(animacionActual);
            animacionActual = null;
        }

        objetivo.anchoredPosition = ObtenerPosicionObjetivo();
    }
}