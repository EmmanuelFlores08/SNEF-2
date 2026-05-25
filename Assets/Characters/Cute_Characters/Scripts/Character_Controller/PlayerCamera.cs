using UnityEngine;

namespace Controller
{
    public abstract class PlayerCamera : MonoBehaviour
    {
        // ── Distancia fija (zoom eliminado) ─────────────────────────────────
        // Ya no hay MIN/MAX_DISTANCE variables porque la distancia no cambia.
        // Si quieres ajustar la distancia, hazlo desde ThirdPersonCamera.m_Distance.

        private const float TARGET_DISTANCE = 20f;

        [SerializeField]
        protected Transform m_Player;

        [Header("Sensibilidad del mouse")]
        [SerializeField, Range(0f, 1f)]
        private float m_SensitivityX = 0.1f;
        [SerializeField, Range(0f, 1f)]
        private float m_SensitivityY = 0.1f;

        // ── Ángulos verticales ampliados ─────────────────────────────────────
        // Separados en min (mirar arriba = negativo) y max (mirar abajo = positivo)
        // para permitir rango asimétrico libre arriba/abajo.
        [Header("Límites verticales")]
        [Tooltip("Límite hacia ARRIBA en grados (valor positivo → más arriba)")]
        [SerializeField, Range(0f, 89f)]
        private float m_MinAngle = 60f;   // cuánto puede mirar hacia arriba

        [Tooltip("Límite hacia ABAJO en grados (valor positivo → más abajo)")]
        [SerializeField, Range(0f, 89f)]
        private float m_MaxAngle = 60f;   // cuánto puede mirar hacia abajo

        protected Transform m_Target;
        protected Transform m_Transform;

        protected Vector2 m_Angles;   // x = pitch (vertical), y = yaw (horizontal)
        protected float m_Distance;   // fijado por la subclase, no cambia en runtime

        public Transform Player => m_Player;
        public Vector3 Target => m_Target.position;
        public float TargetDistance => TARGET_DISTANCE;

        protected virtual void Awake()
        {
            m_Transform = transform;

            m_Target = new GameObject($"Target_{gameObject.name}").transform;
            if (m_Transform.parent != null)
                m_Target.transform.parent = m_Transform.parent;

            if (m_Player == null)
                Debug.Log($"Please set the player transform to the camera. GameObject name ({gameObject.name})");
        }

        public virtual void SetInput(in Vector2 delta, float scroll)
        {
            // Acumular rotación — scroll ignorado completamente (fix #1)
            // delta.y negado: mouse hacia arriba → pitch negativo → cámara sube
            m_Angles += new Vector2(-delta.y * m_SensitivityY, delta.x * m_SensitivityX) * 360f;

            // Clamp vertical: negativo = arriba, positivo = abajo (fix #2)
            m_Angles.x = Mathf.Clamp(m_Angles.x, -m_MinAngle, m_MaxAngle);

            // m_Distance no se toca aquí; la subclase lo fija en su Awake/Inspector
        }

        public void BindPlayer(Transform player)
        {
            m_Player = player;
        }
    }
}