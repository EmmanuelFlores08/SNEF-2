using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Manejo del cursor consciente del dispositivo.
/// - MÓVIL/TABLET: el cursor SIEMPRE queda visible (para que el touch funcione).
/// - PC: al dar click en la pantalla el cursor se oculta/bloquea (para mirar con
///   el mouse); ESC lo libera, y en los menús vuelve a mostrarse.
/// </summary>
public class CursorSiempreVisiblePruebas : MonoBehaviour
{
    [Header("Pruebas")]
    [Tooltip("Simula móvil dentro del Editor para probar el modo touch.")]
    [SerializeField] private bool forzarMovilEnEditor = false;

    private bool esMovil;
    private bool interfaceMode; // un menú puede forzar el cursor visible

    private void Awake()
    {
        // Se calcula una sola vez (usa el mismo detector que el resto del juego,
        // incluido el puente de WebGL).
        esMovil = DispositivoUtil.EsMovilOTablet(forzarMovilEnEditor);
    }

    private void Start()
    {
        // Arranca visible en ambos casos.
        MostrarCursor();
    }

    private void Update()
    {
        // En móvil no se maneja aquí: se impone visible en LateUpdate.
        if (esMovil) return;

        // Con un menú abierto, el cursor siempre visible.
        if (interfaceMode)
        {
            MostrarCursor();
            return;
        }

        // ESC libera el cursor.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MostrarCursor();
            return;
        }

        // Click en la pantalla (que NO sea sobre UI) oculta y bloquea el cursor,
        // para poder mirar con el mouse.
        if (Input.GetMouseButtonDown(0) && !EstaSobreUI())
        {
            OcultarCursor();
        }
    }

    private void LateUpdate()
    {
        // SOLO en móvil imponemos el cursor visible, por encima de cualquier otro
        // script que intente bloquearlo (así el touch siempre funciona).
        if (esMovil)
            MostrarCursor();
    }

    private void OnApplicationFocus(bool tieneFoco)
    {
        if (tieneFoco && esMovil)
            MostrarCursor();
    }

    /// <summary>
    /// Un menú puede llamar esto para forzar el cursor visible mientras está abierto,
    /// y ocultarlo de nuevo al cerrarse (en PC).
    /// </summary>
    public void SetInterfaceMode(bool menuAbierto)
    {
        interfaceMode = menuAbierto;

        if (esMovil || menuAbierto)
            MostrarCursor();
        else
            OcultarCursor();
    }

    private void MostrarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OcultarCursor()
    {
        if (esMovil) return; // en móvil nunca se oculta

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool EstaSobreUI()
    {
        return EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }
}
