using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AirHockeyPlayerController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera hockeyCamera;

    [Tooltip("BoxCollider que cubre solamente la mitad permitida del jugador.")]
    [SerializeField] private BoxCollider movementArea;

    [Tooltip("Collider principal del mazo.")]
    [SerializeField] private Collider malletCollider;

    [Header("Movimiento")]
    [SerializeField] private float maxSpeed = 10f;

    [Tooltip("Separación adicional respecto a los límites del área.")]
    [SerializeField] private float extraPadding = 0.02f;

    private Rigidbody body;

    private Vector3 desiredPosition;
    private float movementHeight;

    private bool controlEnabled;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;

        movementHeight = body.position.y;
        desiredPosition = body.position;

        FindMalletColliderIfMissing();
    }

    private void Update()
    {
        if (!controlEnabled)
            return;

        if (hockeyCamera == null || movementArea == null)
            return;

        Ray mouseRay = hockeyCamera.ScreenPointToRay(
            Input.mousePosition
        );

        Plane movementPlane = new Plane(
            Vector3.up,
            new Vector3(0f, movementHeight, 0f)
        );

        if (!movementPlane.Raycast(mouseRay, out float distance))
            return;

        Vector3 mouseWorldPosition = mouseRay.GetPoint(distance);

        desiredPosition = ClampPositionToArea(
            mouseWorldPosition
        );
    }

    private void FixedUpdate()
    {
        if (!controlEnabled)
            return;

        Vector3 nextPosition = Vector3.MoveTowards(
            body.position,
            desiredPosition,
            maxSpeed * Time.fixedDeltaTime
        );

        nextPosition = ClampPositionToArea(nextPosition);
        nextPosition.y = movementHeight;

        body.MovePosition(nextPosition);
    }

    public void SetControlEnabled(bool enabled)
    {
        controlEnabled = enabled;

        if (body == null)
            return;

        desiredPosition = ClampPositionToArea(
            body.position
        );

        if (enabled)
        {
            body.position = desiredPosition;
            transform.position = desiredPosition;
        }
    }

    private Vector3 ClampPositionToArea(Vector3 worldPosition)
    {
        worldPosition.y = movementHeight;

        if (movementArea == null)
            return worldPosition;

        Bounds areaBounds = movementArea.bounds;

        Vector3 malletExtents = GetMalletWorldExtents();

        float minimumX =
            areaBounds.min.x +
            malletExtents.x +
            extraPadding;

        float maximumX =
            areaBounds.max.x -
            malletExtents.x -
            extraPadding;

        float minimumZ =
            areaBounds.min.z +
            malletExtents.z +
            extraPadding;

        float maximumZ =
            areaBounds.max.z -
            malletExtents.z -
            extraPadding;

        // Protección por si el área es más pequeña que el mazo.
        if (minimumX > maximumX)
        {
            float centerX = areaBounds.center.x;
            minimumX = centerX;
            maximumX = centerX;
        }

        if (minimumZ > maximumZ)
        {
            float centerZ = areaBounds.center.z;
            minimumZ = centerZ;
            maximumZ = centerZ;
        }

        worldPosition.x = Mathf.Clamp(
            worldPosition.x,
            minimumX,
            maximumX
        );

        worldPosition.z = Mathf.Clamp(
            worldPosition.z,
            minimumZ,
            maximumZ
        );

        worldPosition.y = movementHeight;

        return worldPosition;
    }

    private Vector3 GetMalletWorldExtents()
    {
        if (malletCollider == null)
            return new Vector3(0.2f, 0f, 0.2f);

        return malletCollider.bounds.extents;
    }

    private void FindMalletColliderIfMissing()
    {
        if (malletCollider != null)
            return;

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider currentCollider in colliders)
        {
            if (currentCollider == null)
                continue;

            if (currentCollider.isTrigger)
                continue;

            malletCollider = currentCollider;
            break;
        }
    }

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0f, maxSpeed);
        extraPadding = Mathf.Max(0f, extraPadding);

        if (malletCollider == null)
            FindMalletColliderIfMissing();
    }

    private void OnDrawGizmosSelected()
    {
        if (movementArea == null)
            return;

        Bounds areaBounds = movementArea.bounds;
        Vector3 malletExtents = GetMalletWorldExtents();

        Vector3 usableCenter = areaBounds.center;

        Vector3 usableSize = new Vector3(
            Mathf.Max(
                0.01f,
                areaBounds.size.x -
                (malletExtents.x + extraPadding) * 2f
            ),
            0.02f,
            Mathf.Max(
                0.01f,
                areaBounds.size.z -
                (malletExtents.z + extraPadding) * 2f
            )
        );

        usableCenter.y = transform.position.y;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(usableCenter, usableSize);
    }
}