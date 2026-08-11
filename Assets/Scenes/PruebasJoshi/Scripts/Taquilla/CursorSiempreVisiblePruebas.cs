using UnityEngine;

public class CursorSiempreVisiblePruebas : MonoBehaviour
{
    private void Start()
    {
        MostrarCursor();
    }

    private void LateUpdate()
    {
        // Lo hacemos en LateUpdate para imponernos sobre otros
        // scripts que intenten volver a bloquear el cursor.
        MostrarCursor();
    }

    private void OnApplicationFocus(bool tieneFoco)
    {
        if (tieneFoco)
            MostrarCursor();
    }

    private void MostrarCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}