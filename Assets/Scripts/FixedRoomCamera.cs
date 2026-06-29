using UnityEngine;

namespace Controller
{
    public class FixedRoomCamera : PlayerCamera
    {
        [Header("Dirección de pantalla (control WASD)")]
        [Tooltip("Opcional: marca hacia dónde es 'arriba en pantalla' (W). Si está vacío, usa la dirección de la cámara.")]
        [SerializeField] private Transform forwardReference;

        [Tooltip("Qué tan lejos se proyecta el target")]
        [SerializeField] private float targetDistance = 100f;

        protected override void Awake()
        {
            base.Awake();
            UpdateTarget();
        }

        public override void SetInput(in Vector2 delta, float scroll)
        {
            // Cámara fija: ignora el mouse. El target se mantiene constante según la cámara.
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            if (m_Target == null) return;

            // "Adelante" = la dirección que mira la cámara (o el forwardReference), aplanada
            Vector3 forwardDir;
            if (forwardReference != null)
                forwardDir = forwardReference.position - transform.position;
            else
                forwardDir = transform.forward;

            forwardDir.y = 0f;
            if (forwardDir.sqrMagnitude < 0.0001f)
                forwardDir = Vector3.forward;
            forwardDir.Normalize();

            // Relativo al personaje y lejano, para dirección estable
            Vector3 basePos = (m_Player != null) ? m_Player.position : transform.position;
            m_Target.position = basePos + forwardDir * targetDistance;
        }
    }
}