using UnityEngine;

namespace Controller
{
    public class CursorLockManager : MonoBehaviour
    {
        [Tooltip("Tecla para liberar/rebloquear el cursor manualmente")]
        [SerializeField] private KeyCode m_ToggleKey = KeyCode.Escape;

        private bool m_WantsLocked = true;
        private bool m_IsInterfaceOpen = false;

        private void Start()
        {
            LockCursor();
        }

        private void Update()
        {
            // Si hay una interfaz abierta, no intentamos bloquear el cursor.
            if (m_IsInterfaceOpen)
                return;

#if !UNITY_WEBGL
            if (Input.GetKeyDown(m_ToggleKey))
            {
                m_WantsLocked = !m_WantsLocked;

                if (m_WantsLocked)
                    LockCursor();
                else
                    UnlockCursor();
            }
#endif

#if UNITY_WEBGL
            if (m_WantsLocked && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.visible = true;

                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                    LockCursor();
            }
#endif
        }

        public void SetInterfaceMode(bool isOpen)
        {
            m_IsInterfaceOpen = isOpen;

            if (m_IsInterfaceOpen)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            m_WantsLocked = true;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            m_WantsLocked = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                return;

            if (m_IsInterfaceOpen)
                return;

            if (m_WantsLocked)
                LockCursor();
        }
    }
}