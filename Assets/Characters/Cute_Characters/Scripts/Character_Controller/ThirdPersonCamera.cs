using UnityEngine;
using UnityEngine.Rendering;          // URP / Core
using System.Collections.Generic;

namespace Controller
{
    public class ThirdPersonCamera : PlayerCamera
    {
        // ────────────────────────────────────────────────────────────────────
        //  Inspector
        // ────────────────────────────────────────────────────────────────────

        [Header("Posición")]
        [Tooltip("Altura del punto al que mira la cámara (0 = pies, ~1.5 = pecho)")]
        [SerializeField, Range(0f, 2f)]
        private float m_Offset = 1.5f;

        [Tooltip("Desplazamiento vertical de la CÁMARA. Negativo = más baja (sobre el hombro). Recomendado: -0.5 a -1.0")]
        [SerializeField, Range(-2f, 2f)]
        private float m_CameraVerticalOffset = -0.6f;

        [Tooltip("Desplazamiento lateral (positivo = hombro derecho, negativo = izquierdo)")]
        [SerializeField, Range(-1f, 1f)]
        private float m_CameraLateralOffset = 0.4f;

        [SerializeField, Range(0f, 360f)]
        private float m_CameraSpeed = 90f;

        /// <summary>Distancia fija cámara-jugador. El scroll NO la modifica.</summary>
        [SerializeField, Range(1f, 10f)]
        private float m_FixedDistance = 5f;

        // ── Colisión (fix #3) ────────────────────────────────────────────────
        [Header("Colisión con estructuras")]
        [SerializeField]
        private LayerMask m_CollisionLayers = ~0;

        [SerializeField, Range(0.05f, 0.5f)]
        private float m_CollisionRadius = 0.2f;

        [SerializeField, Range(0.3f, 2f)]
        private float m_MinDistance = 0.8f;

        [Tooltip("Qué tan rápido se recupera la distancia tras dejar de haber colisión")]
        [SerializeField, Range(1f, 20f)]
        private float m_CollisionRecovery = 8f;

        // ── Transparencia (fix #4) — URP ─────────────────────────────────────
        [Header("Transparencia del personaje (URP)")]
        [SerializeField]
        private Renderer[] m_CharacterRenderers;

        [Tooltip("Ángulo absoluto (pitch) a partir del cual empieza a desvanecerse")]
        [SerializeField, Range(0f, 89f)]
        private float m_FadeStartAngle = 35f;

        [Tooltip("Ángulo absoluto en que llega a la opacidad mínima")]
        [SerializeField, Range(0f, 89f)]
        private float m_FadeEndAngle = 60f;

        [Tooltip("Opacidad mínima (0 = invisible, 1 = opaco)")]
        [SerializeField, Range(0f, 1f)]
        private float m_MinAlpha = 0.15f;

        // ────────────────────────────────────────────────────────────────────
        //  Estado interno
        // ────────────────────────────────────────────────────────────────────

        private Vector3 m_LookPoint;
        private Vector3 m_TargetPos;
        private float   m_CurrentDist;   // distancia real (puede reducirse por colisión)

        // Materiales instanciados para fade (no tocamos los originales del proyecto)
        private Material[][] m_FadeMats;
        private float        m_LastAlpha = 1f;

        // ────────────────────────────────────────────────────────────────────
        //  Unity lifecycle
        // ────────────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            // Fijar la distancia una vez — no cambia nunca en runtime
            m_Distance    = m_FixedDistance;
            m_CurrentDist = m_FixedDistance;

            InitFadeMaterials();
        }

        private void LateUpdate()
        {
            Move(Time.deltaTime);
            HandleFade();
        }

        // ────────────────────────────────────────────────────────────────────
        //  Fix #1 y #2 — distancia fija + ángulos libres
        //  (la lógica central vive en PlayerCamera.SetInput)
        // ────────────────────────────────────────────────────────────────────

        public override void SetInput(in Vector2 delta, float scroll)
        {
            // Llamamos la base IGNORANDO el scroll (se pasa 0f siempre)
            base.SetInput(delta, 0f);   // ← fix #1: scroll ignorado

            var rot       = Quaternion.Euler(m_Angles.x, m_Angles.y, 0f);
            var playerPos = (m_Player == null) ? Vector3.zero : m_Player.position;

            m_LookPoint = playerPos + m_Offset * Vector3.up;

            // Offset de hombro: desplazar la cámara en local-space de la rotación horizontal
            var yawRot         = Quaternion.Euler(0f, m_Angles.y, 0f);
            var shoulderOffset = yawRot * new Vector3(m_CameraLateralOffset, m_CameraVerticalOffset, 0f);

            var dir     = rot * new Vector3(0f, 0f, -m_Distance);
            m_TargetPos = m_LookPoint + dir + shoulderOffset;
        }

        // ────────────────────────────────────────────────────────────────────
        //  Movimiento de cámara + fix #3 (colisión)
        // ────────────────────────────────────────────────────────────────────

        private void Move(float deltaTime)
        {
            // -- Colisión: calcular distancia segura ---------------------------
            float safeDist = m_FixedDistance;

            var rot          = Quaternion.Euler(m_Angles.x, m_Angles.y, 0f);
            var camDirection = rot * Vector3.back;   // desde LookPoint hacia cámara

            // Shoulder offset consistente con SetInput
            var yawRot         = Quaternion.Euler(0f, m_Angles.y, 0f);
            var shoulderOffset = yawRot * new Vector3(m_CameraLateralOffset, m_CameraVerticalOffset, 0f);
            var castOrigin     = m_LookPoint + shoulderOffset;

            if (Physics.SphereCast(
                    castOrigin,
                    m_CollisionRadius,
                    camDirection,
                    out RaycastHit hit,
                    m_FixedDistance,
                    m_CollisionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                safeDist = Mathf.Max(hit.distance - m_CollisionRadius, m_MinDistance);
            }

            // Lerp suave para evitar saltos bruscos al entrar/salir de colisión
            m_CurrentDist = Mathf.Lerp(m_CurrentDist, safeDist, deltaTime * m_CollisionRecovery);

            // Recalcular target real con distancia corregida
            Vector3 correctedTarget = m_LookPoint + camDirection * m_CurrentDist + shoulderOffset;

            // -- Mover cámara suavemente hacia correctedTarget ----------------
            var direction = correctedTarget - m_Transform.position;
            var step      = m_CameraSpeed * deltaTime;

            if (step * step > direction.sqrMagnitude)
                m_Transform.position = correctedTarget;
            else
                m_Transform.position += step * direction.normalized;

            m_Transform.LookAt(m_LookPoint);

            // -- Actualizar target (punto de mira del jugador) -----------------
            if (m_Target != null)
                m_Target.position = m_Transform.position + m_Transform.forward * TargetDistance;
        }

        // ────────────────────────────────────────────────────────────────────
        //  Fix #4 — Transparencia según pitch (URP)
        // ────────────────────────────────────────────────────────────────────

        private void HandleFade()
        {
            if (m_FadeMats == null || m_FadeMats.Length == 0) return;

            float absAngle = Mathf.Abs(m_Angles.x);
            float alpha;

            if (absAngle >= m_FadeEndAngle)
                alpha = m_MinAlpha;
            else if (absAngle > m_FadeStartAngle)
                alpha = Mathf.Lerp(1f, m_MinAlpha,
                            Mathf.InverseLerp(m_FadeStartAngle, m_FadeEndAngle, absAngle));
            else
                alpha = 1f;

            // Sólo actualizar materiales si el alpha cambió (evitar GC cada frame)
            if (Mathf.Approximately(alpha, m_LastAlpha)) return;
            m_LastAlpha = alpha;

            for (int i = 0; i < m_CharacterRenderers.Length; i++)
            {
                if (m_CharacterRenderers[i] == null) continue;
                var mats = m_FadeMats[i];

                foreach (var mat in mats)
                {
                    if (mat == null) continue;
                    SetURPAlpha(mat, alpha);
                }

                m_CharacterRenderers[i].materials = mats;
            }
        }

        // ── Helpers de materiales URP ────────────────────────────────────────

        private void InitFadeMaterials()
        {
            if (m_CharacterRenderers == null || m_CharacterRenderers.Length == 0) return;

            m_FadeMats = new Material[m_CharacterRenderers.Length][];

            for (int i = 0; i < m_CharacterRenderers.Length; i++)
            {
                if (m_CharacterRenderers[i] == null) continue;

                var originals = m_CharacterRenderers[i].sharedMaterials;
                var copies    = new Material[originals.Length];

                for (int j = 0; j < originals.Length; j++)
                {
                    copies[j] = new Material(originals[j]);   // instancia propia
                    // Habilitar transparencia desde el inicio para evitar
                    // artefactos al primer cambio de alpha
                    EnableURPTransparency(copies[j]);
                }

                m_FadeMats[i] = copies;
                m_CharacterRenderers[i].materials = copies;
            }
        }

        /// <summary>
        /// Configura un material URP (Lit / Simple Lit) para modo Transparent.
        /// Llamar una sola vez por material al inicializar.
        /// </summary>
        private static void EnableURPTransparency(Material mat)
        {
            // Surface Type = 1 → Transparent  (0 = Opaque)
            mat.SetFloat("_Surface", 1f);
            // Blend mode Alpha
            mat.SetFloat("_Blend", 0f);

            mat.SetInt("_SrcBlend",  (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");

            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>
        /// Cambia sólo el canal alpha del color base del material URP.
        /// </summary>
        private static void SetURPAlpha(Material mat, float alpha)
        {
            // _BaseColor es la propiedad de color en shaders URP Lit / Simple Lit
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))   // fallback shader legado
            {
                Color c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }

        // ────────────────────────────────────────────────────────────────────
        //  Gizmos
        // ────────────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (m_Player == null) return;

            var pivot = m_Player.position + m_Offset * Vector3.up;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pivot, 0.12f);
            Gizmos.DrawLine(pivot, transform.position);

            // Visualizar radio de colisión
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_CollisionRadius);
        }
    }
}