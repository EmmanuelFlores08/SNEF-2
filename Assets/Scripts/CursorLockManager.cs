using UnityEngine;

namespace Controller
{
    /// <summary>
    /// Maneja el bloqueo y ocultamiento del cursor para WebGL (y escritorio).
    ///
    /// SETUP:
    ///   - Adjunta este script a cualquier GameObject persistente en la escena
    ///     (por ejemplo el mismo que tiene MovePlayerInput).
    ///   - No requiere referencias adicionales.
    ///
    /// COMPORTAMIENTO:
    ///   - Al iniciar: bloquea y oculta el cursor.
    ///   - En WebGL: el navegador libera el lock si el usuario presiona Escape
    ///     o cambia de pestaña. Al hacer click en el canvas se vuelve a bloquear.
    ///   - Presionar Escape en escritorio alterna el cursor (útil para pausar/menú).
    /// </summary>
    public class CursorLockManager : MonoBehaviour
    {
        [Tooltip("Tecla para liberar/rebloquear el cursor manualmente (útil en editor y escritorio)")]
        [SerializeField]
        private KeyCode m_ToggleKey = KeyCode.Escape;

        private bool m_WantsLocked = true;

        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            // -- Escritorio: toggle manual con Escape -------------------------
#if !UNITY_WEBGL
            if (Input.GetKeyDown(m_ToggleKey))
            {
                m_WantsLocked = !m_WantsLocked;
                if (m_WantsLocked) LockCursor();
                else               UnlockCursor();
            }
#endif

            // -- WebGL: el navegador puede haber liberado el lock sin avisar --
            // Si queremos tenerlo bloqueado y el estado actual es libre,
            // esperamos un click para volver a pedirlo (requisito del navegador).
#if UNITY_WEBGL
            if (m_WantsLocked && Cursor.lockState != CursorLockMode.Locked)
            {
                // Mostrar el cursor mientras esperamos el click de reactivación
                Cursor.visible = true;

                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    LockCursor();
                }
            }
#endif
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            m_WantsLocked    = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            m_WantsLocked    = false;
        }

        // Cuando la ventana/pestaña recupera el foco, re-bloquear automáticamente
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && m_WantsLocked)
            {
                // Pequeño delay implícito: Unity lo procesa en el siguiente frame
                LockCursor();
            }
        }
    }
}