using UnityEngine;

namespace Controller
{
    [RequireComponent(typeof(CharacterMover))]
    public class MovePlayerInput : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private string m_HorizontalAxis = "Horizontal";
        [SerializeField] private string m_VerticalAxis = "Vertical";
        [SerializeField] private string m_JumpButton = "Jump";
        [SerializeField] private KeyCode m_RunKey = KeyCode.LeftShift;

        [Header("Camera")]
        [SerializeField] private PlayerCamera m_Camera;
        [SerializeField] private string m_MouseX = "Mouse X";
        [SerializeField] private string m_MouseY = "Mouse Y";
        [SerializeField] private string m_MouseScroll = "Mouse ScrollWheel";

        [Header("Controles táctiles (móvil)")]
        [SerializeField] private VirtualJoystick m_Joystick;
        [SerializeField] private TouchCameraArea m_TouchCamera;
        [SerializeField] private bool m_ForceTouch = false;

        [Tooltip("Qué tan al fondo debe estar la palanca para correr (0.5 a 0.9). Más alto = hay que empujar más para correr.")]
        [SerializeField, Range(0.5f, 0.95f)] private float m_RunThreshold = 0.8f;

        private CharacterMover m_Mover;

        private Vector2 m_Axis;
        private bool m_IsRun;
        private bool m_IsJump;

        private Vector3 m_Target;
        private Vector2 m_MouseDelta;
        private float m_Scroll;

        private bool m_UseTouch;

        public PlayerCamera Camera => m_Camera;

        private void Awake()
        {
            m_Mover = GetComponent<CharacterMover>();

            // Detecta si usamos controles táctiles
            m_UseTouch = m_ForceTouch || Application.isMobilePlatform;

            if (m_Camera == null)
                Debug.Log($"CharacterMover needs a camera. GameObject name ({gameObject.name})");
        }

        private void Update()
        {
            GatherInput();
            SetInput();
        }

        public void GatherInput()
        {
            if (m_UseTouch)
            {
                // Movimiento por joystick
                Vector2 joystickInput = (m_Joystick != null) ? m_Joystick.Input : Vector2.zero;
                m_Axis = joystickInput;

                // La magnitud del joystick decide caminar o correr:
                // cerca del centro = camina, al fondo = corre
                float magnitud = joystickInput.magnitude;
                m_IsRun = magnitud > m_RunThreshold;

                // Cámara por arrastre en zona derecha
                m_MouseDelta = (m_TouchCamera != null) ? m_TouchCamera.ConsumeDelta() : Vector2.zero;
                m_Scroll = 0f;
            }
            else
            {
                // Teclado y mouse (PC)
                m_Axis = new Vector2(Input.GetAxis(m_HorizontalAxis), Input.GetAxis(m_VerticalAxis));
                m_IsRun = Input.GetKey(m_RunKey);

                m_MouseDelta = new Vector2(Input.GetAxis(m_MouseX), Input.GetAxis(m_MouseY));
                m_Scroll = Input.GetAxis(m_MouseScroll);
            }

            m_IsJump = false; // salto desactivado siempre

            m_Target = (m_Camera == null) ? Vector3.zero : m_Camera.Target;
        }

        public void BindCamera(PlayerCamera currentCamera)
        {
            m_Camera = currentCamera;
        }

        public void BindMover(CharacterMover mover)
        {
            m_Mover = mover;
        }

        public void SetInput()
        {
            if (m_Mover != null)
                m_Mover.SetInput(in m_Axis, in m_Target, in m_IsRun, m_IsJump);

            if (m_Camera != null)
                m_Camera.SetInput(in m_MouseDelta, m_Scroll);
        }
        public void BindTouchControls(VirtualJoystick joystick, TouchCameraArea touchCamera)
        {
            m_Joystick = joystick;
            m_TouchCamera = touchCamera;
        }
    }
}