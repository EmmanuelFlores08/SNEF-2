using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AirHockeyMalletImpact : MonoBehaviour
{
    [Header("Golpe")]
    [Tooltip("Impulso mínimo aplicado al disco.")]
    [SerializeField] private float minimumImpulse = 0.07f;

    [Tooltip("Cantidad de impulso añadido según la velocidad del mazo.")]
    [SerializeField] private float velocityMultiplier = 0.035f;

    [Tooltip("Impulso máximo para evitar golpes exagerados.")]
    [SerializeField] private float maximumImpulse = 0.38f;

    [Header("Ayuda en esquinas")]
    [SerializeField] private float cornerAssistImpulse = 0.18f;

    [Tooltip("Frecuencia del impulso auxiliar mientras permanece atorado.")]
    [SerializeField] private float stayAssistInterval = 0.08f;

    private Rigidbody body;

    private Vector3 previousPosition;
    private Vector3 estimatedVelocity;

    private float nextStayAssistTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        previousPosition = body.position;
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        estimatedVelocity =
            (body.position - previousPosition) /
            Mathf.Max(Time.fixedDeltaTime, 0.0001f);

        estimatedVelocity.y = 0f;

        previousPosition = body.position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        AirHockeyPuck puck =
            collision.collider.GetComponentInParent<AirHockeyPuck>();

        if (puck == null)
            return;

        float relativeSpeed =
            collision.relativeVelocity.magnitude;

        float malletSpeed =
            estimatedVelocity.magnitude;

        float finalSpeed =
            Mathf.Max(relativeSpeed, malletSpeed);

        float impulse = Mathf.Clamp(
            minimumImpulse +
            finalSpeed * velocityMultiplier,
            minimumImpulse,
            maximumImpulse
        );

        puck.ReceiveMalletHit(
            transform.position,
            impulse
        );
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time < nextStayAssistTime)
            return;

        AirHockeyPuck puck =
            collision.collider.GetComponentInParent<AirHockeyPuck>();

        if (puck == null || !puck.IsNearWall)
            return;

        nextStayAssistTime =
            Time.time + stayAssistInterval;

        puck.ReceiveMalletHit(
            transform.position,
            cornerAssistImpulse
        );
    }

    private void OnValidate()
    {
        minimumImpulse = Mathf.Max(0f, minimumImpulse);

        velocityMultiplier =
            Mathf.Max(0f, velocityMultiplier);

        maximumImpulse = Mathf.Max(
            minimumImpulse,
            maximumImpulse
        );

        cornerAssistImpulse =
            Mathf.Max(0f, cornerAssistImpulse);

        stayAssistInterval =
            Mathf.Max(0.02f, stayAssistInterval);
    }
}