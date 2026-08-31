using UnityEngine;
using UnityEngine.EventSystems;

public class CursorLockManager : MonoBehaviour
{
    [Header("Estado inicial")]
    [SerializeField] private bool lockCursorOnStart = true;

    [Header("Pruebas")]
    [Tooltip("Simula móvil dentro del Editor para probar el modo touch.")]
    [SerializeField] private bool forzarMovilEnEditor = false;

    private bool interfaceMode;
    private bool cursorLocked;
    private bool esMovil;

    private void Awake()
    {
        // Se calcula una sola vez y antes que cualquier Start de los menús.
        esMovil = DispositivoUtil.EsMovilOTablet(forzarMovilEnEditor);
    }

    private void Start()
    {
        // En móvil el cursor SIEMPRE queda libre/visible para que el touch funcione.
        if (esMovil)
        {
            UnlockCursor();
            return;
        }

        if (lockCursorOnStart)
            LockCursor();
        else
            UnlockCursor();
    }

    private void Update()
    {
        // En móvil no bloqueamos nunca; garantizamos cursor visible para UI/touch.
        if (esMovil)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                UnlockCursor();
            return;
        }

        // Si una interfaz está abierta, SIEMPRE deja el cursor libre.
        if (interfaceMode)
        {
            UnlockCursor();
            return;
        }

        // ESC libera el cursor.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
            return;
        }

        // Click en pantalla vuelve a bloquear el cursor,
        // pero solo si NO estás encima de UI.
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
                return;

            LockCursor();
        }
    }

    public void SetInterfaceMode(bool enabled)
    {
        interfaceMode = enabled;

        // En móvil el cursor siempre visible, sin importar el modo interfaz.
        if (esMovil)
        {
            UnlockCursor();
            return;
        }

        if (interfaceMode)
            UnlockCursor();
        else
            LockCursor();
    }

    public void LockCursor()
    {
        // En móvil nunca se bloquea (rompería el touch).
        if (esMovil)
        {
            UnlockCursor();
            return;
        }

        cursorLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        cursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    public bool IsCursorLocked()
    {
        return cursorLocked;
    }

    public bool IsInInterfaceMode()
    {
        return interfaceMode;
    }
}