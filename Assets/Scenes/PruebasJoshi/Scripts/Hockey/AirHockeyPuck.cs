using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AirHockeyPuck : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Área correspondiente al jugador.")]
    [SerializeField] private BoxCollider playerArea;

    [Tooltip("Área correspondiente a la IA.")]
    [SerializeField] private BoxCollider aiArea;

    [Tooltip("Collider principal del disco.")]
    [SerializeField] private Collider puckCollider;

    [Tooltip("Colliders físicos de todas las paredes de la mesa.")]
    [SerializeField] private Collider[] wallColliders;

    [Header("Velocidad")]
    [SerializeField] private float maximumSpeed = 12f;

    [Header("Rebote contra paredes")]
    [Tooltip("Porcentaje de velocidad conservado después del rebote.")]
    [Range(0.1f, 1.2f)]
    [SerializeField] private float wallBounceMultiplier = 0.95f;

    [Tooltip("Velocidad mínima con la que sale el disco después de rebotar.")]
    [SerializeField] private float minimumReboundSpeed = 1.2f;

    [Tooltip("Impactos más débiles que este valor no generan un nuevo rebote.")]
    [SerializeField] private float minimumImpactSpeed = 0.05f;

    [Tooltip("Evita reflejar varias veces la velocidad en el mismo contacto.")]
    [SerializeField] private float wallBounceCooldown = 0.035f;

    [Header("Detección de bloqueo")]
    [Tooltip("Velocidad por debajo de la cual se considera inmóvil.")]
    [SerializeField] private float stuckSpeedThreshold = 0.18f;

    [Tooltip("Tiempo que debe permanecer inmóvil junto a una pared.")]
    [SerializeField] private float stuckDuration = 0.35f;

    [Tooltip("Distancia utilizada para detectar cercanía con una pared.")]
    [SerializeField] private float wallProximity = 0.12f;

    [Tooltip("Impulso automático para liberar el disco.")]
    [SerializeField] private float escapeImpulse = 0.38f;

    [Tooltip("Multiplicador cuando está atorado en una esquina.")]
    [SerializeField] private float cornerEscapeMultiplier = 1.5f;

    [Tooltip("Tiempo mínimo entre liberaciones automáticas.")]
    [SerializeField] private float releaseCooldown = 0.3f;

    [Header("Corrección de penetración")]
    [SerializeField] private float penetrationSkin = 0.01f;

    public Rigidbody Body { get; private set; }

    public bool IsNearWall { get; private set; }
    public bool IsNearCorner { get; private set; }
    public bool IsStuckNearWall { get; private set; }

    private float movementHeight;
    private float stuckTimer;
    private float nextAllowedReleaseTime;

    private float nextAllowedWallBounceTime;
    private Collider lastBouncedWall;

    /*
     * Guardamos la velocidad existente antes de que
     * Unity resuelva la colisión. Esto permite recuperar
     * correctamente la dirección de entrada.
     */
    private Vector3 velocityBeforePhysics;

    private Bounds playableBounds;
    private bool hasPlayableBounds;

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();

        Body.useGravity = false;
        Body.isKinematic = false;

        Body.interpolation =
            RigidbodyInterpolation.Interpolate;

        Body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        Body.maxDepenetrationVelocity = 15f;
        Body.solverIterations = 12;
        Body.solverVelocityIterations = 12;

        /*
         * Evita que el Rigidbody quede dormido
         * permanentemente en una esquina.
         */
        Body.sleepThreshold = 0f;

        movementHeight = Body.position.y;

        FindPuckColliderIfMissing();
        CalculatePlayableBounds();

        velocityBeforePhysics = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (Body == null)
            return;

        KeepPuckOnTablePlane();
        LimitMaximumSpeed();
        ResolveWallPenetration();
        UpdateWallState();
        CheckIfPuckIsStuck();

        /*
         * Esta velocidad se utilizará si durante el
         * siguiente paso físico ocurre una colisión.
         */
        velocityBeforePhysics = Body.linearVelocity;
        velocityBeforePhysics.y = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryBounceFromWall(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        /*
         * También comprobamos durante CollisionStay porque,
         * cuando el mazo empuja el disco contra una pared,
         * puede permanecer en contacto varios frames.
         */
        TryBounceFromWall(collision);
    }

    private void TryBounceFromWall(Collision collision)
    {
        if (Body == null || collision == null)
            return;

        Collider wallCollider =
            FindWallColliderInCollision(collision);

        if (wallCollider == null)
            return;

        /*
         * Evita invertir la velocidad repetidamente
         * entre varios frames del mismo contacto.
         */
        if (wallCollider == lastBouncedWall &&
            Time.time < nextAllowedWallBounceTime)
        {
            return;
        }

        Vector3 collisionNormal =
            GetWallCollisionNormal(
                collision,
                wallCollider
            );

        collisionNormal.y = 0f;

        if (collisionNormal.sqrMagnitude < 0.0001f)
        {
            collisionNormal =
                GetFallbackWallNormal(wallCollider);
        }

        if (collisionNormal.sqrMagnitude < 0.0001f)
            return;

        collisionNormal.Normalize();

        /*
         * Primero utilizamos la velocidad previa al
         * cálculo de la colisión.
         */
        Vector3 incomingVelocity =
            velocityBeforePhysics;

        incomingVelocity.y = 0f;

        /*
         * Si la velocidad anterior es casi cero,
         * utilizamos la velocidad actual.
         */
        if (incomingVelocity.sqrMagnitude < 0.0001f)
        {
            incomingVelocity = Body.linearVelocity;
            incomingVelocity.y = 0f;
        }

        float incomingSpeed =
            incomingVelocity.magnitude;

        if (incomingSpeed < minimumImpactSpeed)
            return;

        /*
         * La velocidad debe apuntar hacia la pared.
         * Si apunta hacia fuera, Unity probablemente
         * ya resolvió el rebote y no hacemos nada.
         */
        float velocityTowardWall =
            Vector3.Dot(
                incomingVelocity,
                collisionNormal
            );

        if (velocityTowardWall >= -minimumImpactSpeed)
        {
            Vector3 currentVelocity =
                Body.linearVelocity;

            currentVelocity.y = 0f;

            float currentTowardWall =
                Vector3.Dot(
                    currentVelocity,
                    collisionNormal
                );

            if (currentTowardWall >= -minimumImpactSpeed)
                return;

            incomingVelocity = currentVelocity;
            incomingSpeed = incomingVelocity.magnitude;
        }

        /*
         * Reflejamos la dirección usando la normal
         * de la pared.
         */
        Vector3 reflectedVelocity =
            Vector3.Reflect(
                incomingVelocity,
                collisionNormal
            );

        reflectedVelocity.y = 0f;

        if (reflectedVelocity.sqrMagnitude < 0.0001f)
        {
            reflectedVelocity = collisionNormal;
        }

        reflectedVelocity.Normalize();

        float reboundSpeed = Mathf.Max(
            incomingSpeed * wallBounceMultiplier,
            minimumReboundSpeed
        );

        reboundSpeed = Mathf.Min(
            reboundSpeed,
            maximumSpeed
        );

        Body.WakeUp();
        Body.linearVelocity =
            reflectedVelocity * reboundSpeed;

        lastBouncedWall = wallCollider;

        nextAllowedWallBounceTime =
            Time.time + wallBounceCooldown;

        stuckTimer = 0f;
        IsStuckNearWall = false;

        velocityBeforePhysics = Body.linearVelocity;
    }

    private Collider FindWallColliderInCollision(
        Collision collision
    )
    {
        if (wallColliders == null)
            return null;

        /*
         * Primero comprobamos el collider principal
         * reportado por la colisión.
         */
        Collider collisionCollider =
            collision.collider;

        Collider registeredWall =
            FindRegisteredWall(collisionCollider);

        if (registeredWall != null)
            return registeredWall;

        /*
         * Después revisamos todos los puntos de contacto
         * por si la pared tiene colliders hijos.
         */
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact =
                collision.GetContact(i);

            registeredWall =
                FindRegisteredWall(
                    contact.otherCollider
                );

            if (registeredWall != null)
                return registeredWall;
        }

        return null;
    }

    private Collider FindRegisteredWall(
        Collider candidate
    )
    {
        if (candidate == null ||
            wallColliders == null)
        {
            return null;
        }

        foreach (Collider registeredWall in wallColliders)
        {
            if (registeredWall == null)
                continue;

            if (candidate == registeredWall)
                return registeredWall;

            /*
             * Admite colliders colocados en objetos
             * hijos o padres de la pared registrada.
             */
            if (candidate.transform.IsChildOf(
                    registeredWall.transform
                ))
            {
                return registeredWall;
            }

            if (registeredWall.transform.IsChildOf(
                    candidate.transform
                ))
            {
                return registeredWall;
            }
        }

        return null;
    }

    private Vector3 GetWallCollisionNormal(
        Collision collision,
        Collider wallCollider
    )
    {
        Vector3 accumulatedNormal =
            Vector3.zero;

        int validContacts = 0;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact =
                collision.GetContact(i);

            Collider contactWall =
                FindRegisteredWall(
                    contact.otherCollider
                );

            if (contactWall == null)
                continue;

            /*
             * La normal del contacto apunta desde la
             * superficie hacia el objeto que colisiona.
             */
            accumulatedNormal +=
                contact.normal;

            validContacts++;
        }

        if (validContacts > 0)
        {
            return accumulatedNormal /
                   validContacts;
        }

        return GetFallbackWallNormal(
            wallCollider
        );
    }

    private Vector3 GetFallbackWallNormal(
        Collider wallCollider
    )
    {
        if (wallCollider == null)
            return Vector3.zero;

        Vector3 closestPoint =
            wallCollider.ClosestPoint(
                Body.position
            );

        Vector3 normal =
            Body.position - closestPoint;

        normal.y = 0f;

        if (normal.sqrMagnitude > 0.0001f)
            return normal.normalized;

        if (hasPlayableBounds)
        {
            normal =
                playableBounds.center -
                Body.position;

            normal.y = 0f;

            if (normal.sqrMagnitude > 0.0001f)
                return normal.normalized;
        }

        return Vector3.zero;
    }

    private void KeepPuckOnTablePlane()
    {
        Vector3 velocity = Body.linearVelocity;
        velocity.y = 0f;

        Body.linearVelocity = velocity;

        Vector3 position = Body.position;
        position.y = movementHeight;

        Body.position = position;
    }

    private void LimitMaximumSpeed()
    {
        Vector3 velocity = Body.linearVelocity;
        velocity.y = 0f;

        float currentSpeed =
            velocity.magnitude;

        if (currentSpeed <= maximumSpeed)
            return;

        Body.linearVelocity =
            velocity.normalized * maximumSpeed;
    }

    private void UpdateWallState()
    {
        if (!hasPlayableBounds ||
            puckCollider == null)
        {
            IsNearWall = false;
            IsNearCorner = false;
            return;
        }

        Bounds puckBounds =
            puckCollider.bounds;

        float distanceMinX =
            puckBounds.min.x -
            playableBounds.min.x;

        float distanceMaxX =
            playableBounds.max.x -
            puckBounds.max.x;

        float distanceMinZ =
            puckBounds.min.z -
            playableBounds.min.z;

        float distanceMaxZ =
            playableBounds.max.z -
            puckBounds.max.z;

        bool nearX =
            distanceMinX <= wallProximity ||
            distanceMaxX <= wallProximity;

        bool nearZ =
            distanceMinZ <= wallProximity ||
            distanceMaxZ <= wallProximity;

        IsNearWall = nearX || nearZ;
        IsNearCorner = nearX && nearZ;
    }

    private void CheckIfPuckIsStuck()
    {
        Vector3 flatVelocity =
            Body.linearVelocity;

        flatVelocity.y = 0f;

        bool movingVerySlowly =
            flatVelocity.magnitude <=
            stuckSpeedThreshold;

        if (IsNearWall && movingVerySlowly)
        {
            stuckTimer +=
                Time.fixedDeltaTime;
        }
        else
        {
            stuckTimer = 0f;
            IsStuckNearWall = false;
        }

        if (stuckTimer < stuckDuration)
            return;

        IsStuckNearWall = true;

        ForceReleaseTowardCenter();

        stuckTimer = 0f;
    }

    public void ForceReleaseTowardCenter(
        float customImpulse = -1f
    )
    {
        if (Body == null ||
            !hasPlayableBounds)
        {
            return;
        }

        if (Time.time < nextAllowedReleaseTime)
            return;

        nextAllowedReleaseTime =
            Time.time + releaseCooldown;

        Body.WakeUp();

        ResolveWallPenetration();
        UpdateWallState();

        Vector3 inwardDirection =
            CalculateInwardDirection();

        if (inwardDirection.sqrMagnitude < 0.001f)
        {
            inwardDirection =
                playableBounds.center -
                Body.position;

            inwardDirection.y = 0f;
        }

        if (inwardDirection.sqrMagnitude < 0.001f)
            inwardDirection = Vector3.right;

        inwardDirection.Normalize();

        float finalImpulse =
            customImpulse > 0f
                ? customImpulse
                : escapeImpulse;

        if (IsNearCorner)
        {
            finalImpulse *=
                cornerEscapeMultiplier;
        }

        Vector3 currentVelocity =
            Body.linearVelocity;

        currentVelocity.y = 0f;

        float outwardVelocity =
            Vector3.Dot(
                currentVelocity,
                inwardDirection
            );

        if (outwardVelocity < 0f)
        {
            currentVelocity -=
                inwardDirection *
                outwardVelocity;

            Body.linearVelocity =
                currentVelocity;
        }

        Body.AddForce(
            inwardDirection * finalImpulse,
            ForceMode.Impulse
        );

        velocityBeforePhysics =
            Body.linearVelocity;

        stuckTimer = 0f;
        IsStuckNearWall = false;
    }

    private Vector3 CalculateInwardDirection()
    {
        if (!hasPlayableBounds ||
            puckCollider == null)
        {
            return Vector3.zero;
        }

        Bounds puckBounds =
            puckCollider.bounds;

        float distanceMinX =
            puckBounds.min.x -
            playableBounds.min.x;

        float distanceMaxX =
            playableBounds.max.x -
            puckBounds.max.x;

        float distanceMinZ =
            puckBounds.min.z -
            playableBounds.min.z;

        float distanceMaxZ =
            playableBounds.max.z -
            puckBounds.max.z;

        Vector3 direction =
            Vector3.zero;

        if (distanceMinX <= wallProximity)
            direction.x += 1f;

        if (distanceMaxX <= wallProximity)
            direction.x -= 1f;

        if (distanceMinZ <= wallProximity)
            direction.z += 1f;

        if (distanceMaxZ <= wallProximity)
            direction.z -= 1f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction =
                playableBounds.center -
                Body.position;

            direction.y = 0f;
        }

        return direction.normalized;
    }

    public void ReceiveMalletHit(
        Vector3 malletPosition,
        float impulseStrength
    )
    {
        if (Body == null)
            return;

        Body.WakeUp();

        ResolveWallPenetration();
        UpdateWallState();

        Vector3 awayFromMallet =
            Body.position -
            malletPosition;

        awayFromMallet.y = 0f;

        if (awayFromMallet.sqrMagnitude < 0.001f)
            awayFromMallet = Vector3.right;

        awayFromMallet.Normalize();

        Vector3 finalDirection =
            awayFromMallet;

        if (IsNearWall)
        {
            Vector3 inwardDirection =
                CalculateInwardDirection();

            finalDirection =
                awayFromMallet +
                inwardDirection * 1.4f;

            if (finalDirection.sqrMagnitude < 0.001f)
                finalDirection = inwardDirection;

            finalDirection.Normalize();
        }

        Body.AddForce(
            finalDirection * impulseStrength,
            ForceMode.Impulse
        );

        velocityBeforePhysics =
            Body.linearVelocity;

        stuckTimer = 0f;
        IsStuckNearWall = false;
    }

    private void ResolveWallPenetration()
    {
        if (puckCollider == null ||
            wallColliders == null)
        {
            return;
        }

        Vector3 correctedPosition =
            Body.position;

        Quaternion puckRotation =
            Body.rotation;

        bool positionChanged = false;

        foreach (Collider wallCollider in wallColliders)
        {
            if (wallCollider == null ||
                !wallCollider.enabled ||
                wallCollider.isTrigger)
            {
                continue;
            }

            bool overlapping =
                Physics.ComputePenetration(
                    puckCollider,
                    correctedPosition,
                    puckRotation,
                    wallCollider,
                    wallCollider.transform.position,
                    wallCollider.transform.rotation,
                    out Vector3 separationDirection,
                    out float separationDistance
                );

            if (!overlapping)
                continue;

            separationDirection.y = 0f;

            if (separationDirection.sqrMagnitude <
                0.001f)
            {
                continue;
            }

            separationDirection.Normalize();

            correctedPosition +=
                separationDirection *
                (
                    separationDistance +
                    penetrationSkin
                );

            positionChanged = true;
        }

        if (!positionChanged)
            return;

        correctedPosition.y =
            movementHeight;

        Body.position =
            correctedPosition;

        transform.position =
            correctedPosition;

        Physics.SyncTransforms();
    }

    private void CalculatePlayableBounds()
    {
        hasPlayableBounds = false;

        if (playerArea != null)
        {
            playableBounds =
                playerArea.bounds;

            hasPlayableBounds = true;
        }

        if (aiArea != null)
        {
            if (hasPlayableBounds)
            {
                playableBounds.Encapsulate(
                    aiArea.bounds
                );
            }
            else
            {
                playableBounds =
                    aiArea.bounds;

                hasPlayableBounds = true;
            }
        }
    }

    private void FindPuckColliderIfMissing()
    {
        if (puckCollider != null)
            return;

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider currentCollider in colliders)
        {
            if (currentCollider == null ||
                currentCollider.isTrigger)
            {
                continue;
            }

            puckCollider = currentCollider;
            break;
        }
    }

    public void StopPuck()
    {
        if (Body == null)
            return;

        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;

        velocityBeforePhysics =
            Vector3.zero;

        stuckTimer = 0f;
        IsStuckNearWall = false;

        lastBouncedWall = null;
        nextAllowedWallBounceTime = 0f;
    }

    public void ResetPuck(Vector3 position)
    {
        if (Body == null)
            return;

        StopPuck();

        position.y = movementHeight;

        Body.position = position;
        transform.position = position;

        Body.WakeUp();

        Physics.SyncTransforms();
    }

    private void OnValidate()
    {
        maximumSpeed =
            Mathf.Max(0.1f, maximumSpeed);

        wallBounceMultiplier =
            Mathf.Clamp(
                wallBounceMultiplier,
                0.1f,
                1.2f
            );

        minimumReboundSpeed =
            Mathf.Max(
                0.05f,
                minimumReboundSpeed
            );

        minimumImpactSpeed =
            Mathf.Max(
                0.001f,
                minimumImpactSpeed
            );

        wallBounceCooldown =
            Mathf.Max(
                0.01f,
                wallBounceCooldown
            );

        stuckSpeedThreshold =
            Mathf.Max(
                0.01f,
                stuckSpeedThreshold
            );

        stuckDuration =
            Mathf.Max(
                0.05f,
                stuckDuration
            );

        wallProximity =
            Mathf.Max(
                0.01f,
                wallProximity
            );

        escapeImpulse =
            Mathf.Max(
                0.01f,
                escapeImpulse
            );

        cornerEscapeMultiplier =
            Mathf.Max(
                1f,
                cornerEscapeMultiplier
            );

        releaseCooldown =
            Mathf.Max(
                0.05f,
                releaseCooldown
            );

        penetrationSkin =
            Mathf.Max(
                0f,
                penetrationSkin
            );

        if (puckCollider == null)
            FindPuckColliderIfMissing();

        CalculatePlayableBounds();
    }
}