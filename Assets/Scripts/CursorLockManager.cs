using UnityEngine;
using UnityEngine.EventSystems;

public class CursorLockManager : MonoBehaviour
{
    [Header("Estado inicial")]
    [SerializeField] private bool lockCursorOnStart = true;

    private bool interfaceMode;
    private bool cursorLocked;

    private void Start()
    {
        if (lockCursorOnStart)
            LockCursor();
        else
            UnlockCursor();
    }

    private void Update()
    {
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

        if (interfaceMode)
            UnlockCursor();
        else
            LockCursor();
    }

    public void LockCursor()
    {
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