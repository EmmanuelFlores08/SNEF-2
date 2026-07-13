using UnityEngine;
using UnityEngine.UI;

public class TiendaObjetosUI : MonoBehaviour
{
    [Header("Interfaz completa")]
    [SerializeField] private GameObject uiTaquilla;

    [Header("Paneles de contenido")]
    [SerializeField] private GameObject panelObjetosPersonaje;
    [SerializeField] private GameObject panelObjetosSetDeGrabacion;

    [Header("Botones")]
    [SerializeField] private Button buttonPersonaje;
    [SerializeField] private Button buttonSetDeGrabacion;
    [SerializeField] private Button buttonCerrar;

    [Header("Animaciones de tabs")]
    [SerializeField] private BotonUIAnimado animacionButtonPersonaje;
    [SerializeField] private BotonUIAnimado animacionButtonSetDeGrabacion;

    [Header("Control del jugador")]
    [Tooltip("Agrega aquí los scripts de movimiento, cámara o control del jugador que deben desactivarse mientras la tienda está abierta.")]
    [SerializeField] private Behaviour[] scriptsJugadorADesactivar;

    [Header("Configuración")]
    [SerializeField] private bool abrirSiempreEnPersonaje = true;
    [SerializeField] private bool permitirCerrarConEscape = true;

    public bool EstaAbierta { get; private set; }

    private CursorLockMode cursorLockAnterior;
    private bool cursorVisibleAnterior;

    private bool[] estadosAnterioresScripts;
    private bool estadoJugadorGuardado;

    private enum TipoPanel
    {
        Personaje,
        SetDeGrabacion
    }

    private TipoPanel panelActual = TipoPanel.Personaje;

    private void Awake()
    {
        ObtenerAnimacionesAutomaticamente();

        if (buttonPersonaje != null)
            buttonPersonaje.onClick.AddListener(
                MostrarPanelPersonaje
            );

        if (buttonSetDeGrabacion != null)
            buttonSetDeGrabacion.onClick.AddListener(
                MostrarPanelSetDeGrabacion
            );

        if (buttonCerrar != null)
            buttonCerrar.onClick.AddListener(CerrarTienda);
    }

    private void Start()
    {
        EstaAbierta = false;
        panelActual = TipoPanel.Personaje;

        PrepararPanelInicial();

        if (uiTaquilla != null)
            uiTaquilla.SetActive(false);
    }

    private void Update()
    {
        if (!EstaAbierta)
            return;

        if (permitirCerrarConEscape &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CerrarTienda();
        }
    }

    private void ObtenerAnimacionesAutomaticamente()
    {
        if (animacionButtonPersonaje == null &&
            buttonPersonaje != null)
        {
            animacionButtonPersonaje =
                buttonPersonaje.GetComponent<BotonUIAnimado>();
        }

        if (animacionButtonSetDeGrabacion == null &&
            buttonSetDeGrabacion != null)
        {
            animacionButtonSetDeGrabacion =
                buttonSetDeGrabacion.GetComponent<BotonUIAnimado>();
        }
    }

    private void PrepararPanelInicial()
    {
        if (panelObjetosPersonaje != null)
            panelObjetosPersonaje.SetActive(true);

        if (panelObjetosSetDeGrabacion != null)
            panelObjetosSetDeGrabacion.SetActive(false);

        ActualizarEstadoVisualTabs();
    }

    public void AbrirTienda()
    {
        if (EstaAbierta)
            return;

        if (uiTaquilla == null)
        {
            Debug.LogError(
                "No se asignó el objeto UI Taquilla.",
                this
            );

            return;
        }

        GuardarEstadoJugador();

        EstaAbierta = true;
        uiTaquilla.SetActive(true);

        if (abrirSiempreEnPersonaje)
        {
            MostrarPanelPersonaje();
        }
        else
        {
            MostrarPanelActual();
        }

        DesactivarControlesJugador();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarTienda()
    {
        if (!EstaAbierta)
            return;

        EstaAbierta = false;

        if (uiTaquilla != null)
            uiTaquilla.SetActive(false);

        RestaurarControlesJugador();
        RestaurarCursor();
    }

    public void MostrarPanelPersonaje()
    {
        panelActual = TipoPanel.Personaje;

        if (panelObjetosPersonaje != null)
            panelObjetosPersonaje.SetActive(true);

        if (panelObjetosSetDeGrabacion != null)
            panelObjetosSetDeGrabacion.SetActive(false);

        ActualizarEstadoVisualTabs();
    }

    public void MostrarPanelSetDeGrabacion()
    {
        panelActual = TipoPanel.SetDeGrabacion;

        if (panelObjetosPersonaje != null)
            panelObjetosPersonaje.SetActive(false);

        if (panelObjetosSetDeGrabacion != null)
            panelObjetosSetDeGrabacion.SetActive(true);

        ActualizarEstadoVisualTabs();
    }

    private void ActualizarEstadoVisualTabs()
    {
        bool personajeActivo =
            panelActual == TipoPanel.Personaje;

        if (animacionButtonPersonaje != null)
        {
            animacionButtonPersonaje.SetSeleccionado(
                personajeActivo
            );
        }

        if (animacionButtonSetDeGrabacion != null)
        {
            animacionButtonSetDeGrabacion.SetSeleccionado(
                !personajeActivo
            );
        }
    }

    private void MostrarPanelActual()
    {
        switch (panelActual)
        {
            case TipoPanel.Personaje:
                MostrarPanelPersonaje();
                break;

            case TipoPanel.SetDeGrabacion:
                MostrarPanelSetDeGrabacion();
                break;
        }
    }

    private void GuardarEstadoJugador()
    {
        cursorLockAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;

        if (scriptsJugadorADesactivar == null)
        {
            estadoJugadorGuardado = true;
            return;
        }

        estadosAnterioresScripts =
            new bool[scriptsJugadorADesactivar.Length];

        for (int i = 0;
             i < scriptsJugadorADesactivar.Length;
             i++)
        {
            Behaviour script =
                scriptsJugadorADesactivar[i];

            if (script == null)
                continue;

            estadosAnterioresScripts[i] =
                script.enabled;
        }

        estadoJugadorGuardado = true;
    }

    private void DesactivarControlesJugador()
    {
        if (scriptsJugadorADesactivar == null)
            return;

        foreach (Behaviour script in
                 scriptsJugadorADesactivar)
        {
            if (script != null)
                script.enabled = false;
        }
    }

    private void RestaurarControlesJugador()
    {
        if (!estadoJugadorGuardado)
            return;

        if (scriptsJugadorADesactivar == null ||
            estadosAnterioresScripts == null)
        {
            return;
        }

        int cantidad = Mathf.Min(
            scriptsJugadorADesactivar.Length,
            estadosAnterioresScripts.Length
        );

        for (int i = 0; i < cantidad; i++)
        {
            Behaviour script =
                scriptsJugadorADesactivar[i];

            if (script != null)
            {
                script.enabled =
                    estadosAnterioresScripts[i];
            }
        }
    }

    private void RestaurarCursor()
    {
        if (!estadoJugadorGuardado)
            return;

        Cursor.lockState = cursorLockAnterior;
        Cursor.visible = cursorVisibleAnterior;

        estadoJugadorGuardado = false;
    }

    private void OnDestroy()
    {
        if (buttonPersonaje != null)
        {
            buttonPersonaje.onClick.RemoveListener(
                MostrarPanelPersonaje
            );
        }

        if (buttonSetDeGrabacion != null)
        {
            buttonSetDeGrabacion.onClick.RemoveListener(
                MostrarPanelSetDeGrabacion
            );
        }

        if (buttonCerrar != null)
            buttonCerrar.onClick.RemoveListener(CerrarTienda);
    }
}