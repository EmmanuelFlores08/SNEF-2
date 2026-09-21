using UnityEngine;
using Controller;

public class MostrarAyudaPorInactividad : MonoBehaviour
{
    [Header("Elemento de ayuda")]
    [Tooltip("Objeto que contiene W Caminar / Q Ver instrucciones.")]
    [SerializeField] private GameObject elementoAyuda;

    [Header("Configuración")]
    [Tooltip("Segundos sin presionar ninguna tecla antes de mostrar la ayuda.")]
    [SerializeField] private float tiempoParaMostrar = 4f;

    [Header("Estado de exploración")]
    [Tooltip("Input principal del jugador. Si está deshabilitado, la ayuda no se muestra.")]
    [SerializeField] private MovePlayerInput playerInput;

    [Tooltip("Movimiento principal del personaje. Si está deshabilitado, la ayuda no se muestra.")]
    [SerializeField] private CharacterMover characterMover;

    [Tooltip("Gestor de cursor. Si está en modo interfaz, la ayuda no se muestra.")]
    [SerializeField] private CursorLockManager cursorLockManager;

    [Tooltip("Componentes adicionales que deben estar activos para considerar exploración normal.")]
    [SerializeField] private Behaviour[] componentesRequeridosActivos;

    [Header("Detección")]
    [Tooltip("Si está activo, no muestra la ayuda hasta tener una referencia al input del jugador.")]
    [SerializeField] private bool requiereInputJugadorParaMostrar = true;

    [Tooltip("Busca las referencias del jugador si no fueron asignadas desde el Inspector.")]
    [SerializeField] private bool autoResolverReferenciasAlInicio = true;

    [Tooltip("Segundos entre reintentos de búsqueda mientras falte el input del jugador.")]
    [SerializeField] private float intervaloAutoResolucion = 0.5f;

    [Tooltip("Permite simular móvil/tablet desde el Editor.")]
    [SerializeField] private bool forzarMovilEnEditor = false;

    private float tiempoSinActividad;
    private float proximoIntentoAutoResolver;
    private bool esMovilOTablet;

    private void Start()
    {
        esMovilOTablet = DispositivoUtil.EsMovilOTablet(forzarMovilEnEditor);
        tiempoSinActividad = 0f;

        if (autoResolverReferenciasAlInicio)
            ResolverReferenciasFaltantes();

        OcultarAyuda();
    }

    private void Update()
    {
        IntentarResolverReferenciasPendientes();

        if (esMovilOTablet || !JugadorEstaEnModoNormal())
        {
            ReiniciarContadorYOcultar();
            return;
        }

        // Si el jugador presiona cualquier tecla,
        // ocultamos la ayuda y reiniciamos el contador.
        if (Input.anyKeyDown)
        {
            ReiniciarContadorYOcultar();
            return;
        }

        // Contamos el tiempo sin actividad.
        tiempoSinActividad += Time.unscaledDeltaTime;

        // Después del tiempo establecido,
        // mostramos las indicaciones.
        if (tiempoSinActividad >= tiempoParaMostrar)
        {
            MostrarAyuda();
        }
    }

    private bool JugadorEstaEnModoNormal()
    {
        if (requiereInputJugadorParaMostrar && playerInput == null)
            return false;

        if (playerInput != null && !playerInput.enabled)
            return false;

        if (characterMover != null && !characterMover.enabled)
            return false;

        if (cursorLockManager != null && cursorLockManager.IsInInterfaceMode())
            return false;

        if (componentesRequeridosActivos != null)
        {
            foreach (Behaviour componente in componentesRequeridosActivos)
            {
                if (componente != null && !componente.enabled)
                    return false;
            }
        }

        return true;
    }

    public void BindPlayerInput(MovePlayerInput input)
    {
        playerInput = input;
        characterMover = input != null
            ? input.GetComponent<CharacterMover>()
            : null;

        if (cursorLockManager == null && input != null)
            cursorLockManager = input.GetComponent<CursorLockManager>();

        ReiniciarContadorYOcultar();
    }

    private void ReiniciarContadorYOcultar()
    {
        tiempoSinActividad = 0f;
        OcultarAyuda();
    }

    private void MostrarAyuda()
    {
        if (elementoAyuda != null && !elementoAyuda.activeSelf)
            elementoAyuda.SetActive(true);
    }

    private void OcultarAyuda()
    {
        if (elementoAyuda != null && elementoAyuda.activeSelf)
            elementoAyuda.SetActive(false);
    }

    private void ResolverReferenciasFaltantes()
    {
        if (playerInput == null)
            playerInput = BuscarPrimero<MovePlayerInput>();

        if (characterMover == null && playerInput != null)
            characterMover = playerInput.GetComponent<CharacterMover>();

        if (characterMover == null)
            characterMover = BuscarPrimero<CharacterMover>();

        if (cursorLockManager == null)
            cursorLockManager = BuscarPrimero<CursorLockManager>();
    }

    private void IntentarResolverReferenciasPendientes()
    {
        if (!autoResolverReferenciasAlInicio || playerInput != null)
            return;

        if (Time.unscaledTime < proximoIntentoAutoResolver)
            return;

        ResolverReferenciasFaltantes();
        proximoIntentoAutoResolver =
            Time.unscaledTime + Mathf.Max(0.1f, intervaloAutoResolucion);
    }

    private static T BuscarPrimero<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
