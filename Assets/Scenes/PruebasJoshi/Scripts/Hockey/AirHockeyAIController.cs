using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AirHockeyAIController : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    private enum AIState
    {
        Guard,
        Approach,
        Strike,
        RecoverSide,
        RecoverBehind,
        EmergencyBlock
    }

    [Header("Dificultad")]
    [SerializeField] private Difficulty difficulty = Difficulty.Medium;

    [Header("Referencias")]
    [SerializeField] private Transform puck;
    [SerializeField] private Rigidbody puckBody;
    [SerializeField] private AirHockeyPuck puckController;

    [Tooltip("Área completa en la que se puede mover el mazo de la IA.")]
    [SerializeField] private BoxCollider movementArea;

    [Tooltip("Collider principal del mazo de la IA.")]
    [SerializeField] private Collider malletCollider;

    [Tooltip("Posición de guardia cerca de la portería de la IA.")]
    [SerializeField] private Transform homePoint;

    [Tooltip("Centro de la portería de la IA.")]
    [SerializeField] private Transform ownGoal;

    [Tooltip("Centro de la portería del jugador.")]
    [SerializeField] private Transform opponentGoal;

    [Tooltip("Punto central de la mesa. Puedes usar DiscoSpawn.")]
    [SerializeField] private Transform centerPoint;

    [Header("Límites")]
    [SerializeField] private float extraPadding = 0.02f;

    [Header("Depuración")]
    [SerializeField] private bool drawDebugTarget = true;

    // =========================================================
    // VALORES INTERNOS SEGÚN DIFICULTAD
    // =========================================================

    private float guardSpeed;
    private float approachSpeed;
    private float recoverySpeed;
    private float strikeSpeed;

    private float acceleration;
    private float braking;

    private float reactionTime;
    private float predictionTime;
    private float aimError;

    private float guardTracking;
    private float guardRangeRatio;
    private float guardIdleAmplitude;
    private float guardIdleFrequency;

    private float behindDistance;
    private float strikeTriggerDistance;
    private float followThroughDistance;
    private float strikeDuration;

    private float recoverySideOffset;
    private float recoveryArrivalDistance;

    private float goalSideMargin;
    private float emergencyDistanceRatio;
    private float emergencyBehindDistance;

    private float wallAimDistance;
    private float stoppingDistance;
    private float slowingDistance;

    // =========================================================
    // ESTADO
    // =========================================================

    private Rigidbody body;
    private float movementHeight;

    private bool controlEnabled;

    private Difficulty appliedDifficulty;
    private AIState currentState = AIState.Guard;

    private Vector3 currentTarget;
    private Vector3 movementVelocity;

    private Vector3 perceivedPuckPosition;
    private Vector3 perceivedPuckVelocity;

    private float nextReactionTime;
    private float currentAimError;

    private float strikeTimer;
    private Vector3 lockedStrikeDirection;

    private float recoverySideSign = 1f;

    private Vector3 attackAxis;
    private Vector3 lateralAxis;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        body.useGravity = false;
        body.isKinematic = true;

        body.interpolation =
            RigidbodyInterpolation.Interpolate;

        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        movementHeight = body.position.y;

        FindMalletColliderIfMissing();
        FindPuckReferencesIfMissing();

        UpdateTableAxes();
        ApplyDifficultySettings();

        RefreshPerception(true);
    }

    private void FixedUpdate()
    {
        if (difficulty != appliedDifficulty)
            ApplyDifficultySettings();

        if (!controlEnabled)
            return;

        if (!HasRequiredReferences())
            return;

        UpdateTableAxes();
        RefreshPerception(false);
        UpdateStateMachine();

        currentTarget = CalculateTargetForCurrentState();
        currentTarget = ClampPositionToArea(currentTarget);

        MoveTowardTarget(currentTarget);
    }

    // =========================================================
    // DIFICULTADES
    // =========================================================

    private void ApplyDifficultySettings()
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                ApplyEasySettings();
                break;

            case Difficulty.Hard:
                ApplyHardSettings();
                break;

            default:
                ApplyMediumSettings();
                break;
        }

        appliedDifficulty = difficulty;
    }

    private void ApplyEasySettings()
    {
        guardSpeed = 1.8f;
        approachSpeed = 2.7f;
        recoverySpeed = 3.3f;
        strikeSpeed = 4.5f;

        acceleration = 8f;
        braking = 11f;

        reactionTime = 0.28f;
        predictionTime = 0.02f;
        aimError = 0.18f;

        guardTracking = 0.45f;
        guardRangeRatio = 0.25f;
        guardIdleAmplitude = 0.045f;
        guardIdleFrequency = 1.1f;

        behindDistance = 0.42f;
        strikeTriggerDistance = 0.16f;
        followThroughDistance = 0.50f;
        strikeDuration = 0.24f;

        recoverySideOffset = 0.52f;
        recoveryArrivalDistance = 0.14f;

        goalSideMargin = 0.16f;
        emergencyDistanceRatio = 0.28f;
        emergencyBehindDistance = 0.28f;

        wallAimDistance = 0.40f;

        stoppingDistance = 0.025f;
        slowingDistance = 0.35f;
    }

    private void ApplyMediumSettings()
    {
        guardSpeed = 2.5f;
        approachSpeed = 4.0f;
        recoverySpeed = 4.8f;
        strikeSpeed = 6.0f;

        acceleration = 17f;
        braking = 23f;

        reactionTime = 0.13f;
        predictionTime = 0.10f;
        aimError = 0.08f;

        guardTracking = 0.68f;
        guardRangeRatio = 0.30f;
        guardIdleAmplitude = 0.035f;
        guardIdleFrequency = 1.4f;

        behindDistance = 0.44f;
        strikeTriggerDistance = 0.18f;
        followThroughDistance = 0.58f;
        strikeDuration = 0.21f;

        recoverySideOffset = 0.58f;
        recoveryArrivalDistance = 0.14f;

        goalSideMargin = 0.16f;
        emergencyDistanceRatio = 0.32f;
        emergencyBehindDistance = 0.30f;

        wallAimDistance = 0.45f;

        stoppingDistance = 0.025f;
        slowingDistance = 0.38f;
    }

    private void ApplyHardSettings()
    {
        guardSpeed = 3.3f;
        approachSpeed = 5.4f;
        recoverySpeed = 6.2f;
        strikeSpeed = 7.8f;

        acceleration = 28f;
        braking = 34f;

        reactionTime = 0.055f;
        predictionTime = 0.18f;
        aimError = 0.025f;

        guardTracking = 0.88f;
        guardRangeRatio = 0.35f;
        guardIdleAmplitude = 0.025f;
        guardIdleFrequency = 1.7f;

        behindDistance = 0.46f;
        strikeTriggerDistance = 0.20f;
        followThroughDistance = 0.66f;
        strikeDuration = 0.18f;

        recoverySideOffset = 0.62f;
        recoveryArrivalDistance = 0.13f;

        goalSideMargin = 0.17f;
        emergencyDistanceRatio = 0.36f;
        emergencyBehindDistance = 0.32f;

        wallAimDistance = 0.50f;

        stoppingDistance = 0.02f;
        slowingDistance = 0.42f;
    }

    public void SetDifficulty(Difficulty newDifficulty)
    {
        difficulty = newDifficulty;
        ApplyDifficultySettings();
    }

    // =========================================================
    // PERCEPCIÓN
    // =========================================================

    private void RefreshPerception(bool force)
    {
        if (puck == null)
            return;

        if (!force && Time.time < nextReactionTime)
            return;

        perceivedPuckPosition = puck.position;

        if (puckBody != null)
        {
            perceivedPuckVelocity = puckBody.linearVelocity;
            perceivedPuckVelocity.y = 0f;
        }
        else
        {
            perceivedPuckVelocity = Vector3.zero;
        }

        currentAimError = Random.Range(
            -aimError,
            aimError
        );

        nextReactionTime =
            Time.time + reactionTime;
    }

    // =========================================================
    // MÁQUINA DE ESTADOS
    // =========================================================

    private void UpdateStateMachine()
    {
        bool puckOnAISide =
            IsPuckOnAISide(puck.position);

        if (!puckOnAISide)
        {
            SetState(AIState.Guard);
            return;
        }

        if (currentState == AIState.Strike)
        {
            strikeTimer -= Time.fixedDeltaTime;

            if (strikeTimer > 0f)
                return;

            if (!IsMalletGoalSideOfPuck())
                BeginRecovery();
            else if (IsEmergencySituation())
                SetState(AIState.EmergencyBlock);
            else
                SetState(AIState.Approach);

            return;
        }

        if (currentState == AIState.RecoverSide ||
            currentState == AIState.RecoverBehind)
        {
            return;
        }

        /*
         * Si el disco está entre el mazo y la portería
         * de la IA, primero debe rodearlo.
         */
        if (!IsMalletGoalSideOfPuck())
        {
            BeginRecovery();
            return;
        }

        if (IsEmergencySituation())
        {
            SetState(AIState.EmergencyBlock);
            return;
        }

        SetState(AIState.Approach);
    }

    private void SetState(AIState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;

        if (newState == AIState.Guard)
        {
            strikeTimer = 0f;
        }

        if (newState == AIState.Approach)
        {
            currentAimError = Random.Range(
                -aimError,
                aimError
            );
        }
    }

    // =========================================================
    // CÁLCULO DE OBJETIVOS
    // =========================================================

    private Vector3 CalculateTargetForCurrentState()
    {
        switch (currentState)
        {
            case AIState.Approach:
                return CalculateApproachTarget();

            case AIState.Strike:
                return CalculateStrikeTarget();

            case AIState.RecoverSide:
                return CalculateRecoverySideTarget();

            case AIState.RecoverBehind:
                return CalculateRecoveryBehindTarget();

            case AIState.EmergencyBlock:
                return CalculateEmergencyTarget();

            default:
                return CalculateGuardTarget();
        }
    }

    // =========================================================
    // GUARDIA
    // =========================================================

    private Vector3 CalculateGuardTarget()
    {
        Vector3 target = homePoint.position;

        float homeLateral =
            GetLateralPosition(homePoint.position);

        float puckLateral =
            GetLateralPosition(perceivedPuckPosition);

        float maximumOffset =
            movementArea.bounds.size.z *
            guardRangeRatio;

        float desiredOffset = Mathf.Clamp(
            puckLateral - homeLateral,
            -maximumOffset,
            maximumOffset
        );

        desiredOffset *= guardTracking;

        float idleMovement =
            Mathf.Sin(
                Time.time * guardIdleFrequency
            ) * guardIdleAmplitude;

        desiredOffset += idleMovement;

        target +=
            lateralAxis * desiredOffset;

        target.y = movementHeight;

        return target;
    }

    // =========================================================
    // ATAQUE
    // =========================================================

    private Vector3 CalculateApproachTarget()
    {
        Vector3 predictedPuck =
            perceivedPuckPosition +
            perceivedPuckVelocity *
            predictionTime;

        Vector3 strikeDirection =
            CalculateSafeStrikeDirection(
                predictedPuck
            );

        Vector3 behindPosition =
            predictedPuck -
            strikeDirection *
            behindDistance;

        behindPosition.y =
            movementHeight;

        Vector3 clampedBehind =
            ClampPositionToArea(
                behindPosition
            );

        float distanceToBehind =
            FlatDistance(
                body.position,
                clampedBehind
            );

        if (distanceToBehind <=
            strikeTriggerDistance &&
            IsMalletGoalSideOfPuck())
        {
            BeginStrike(strikeDirection);

            return CalculateStrikeTarget();
        }

        return clampedBehind;
    }

    private void BeginStrike(
        Vector3 strikeDirection
    )
    {
        lockedStrikeDirection =
            strikeDirection.normalized;

        /*
         * Garantía adicional:
         * el golpe nunca puede apuntar hacia
         * la propia portería.
         */
        if (Vector3.Dot(
                lockedStrikeDirection,
                attackAxis
            ) < 0.35f)
        {
            lockedStrikeDirection =
                attackAxis;
        }

        strikeTimer = strikeDuration;
        SetState(AIState.Strike);
    }

    private Vector3 CalculateStrikeTarget()
    {
        Vector3 target =
            puck.position +
            lockedStrikeDirection *
            followThroughDistance;

        target.y = movementHeight;

        return target;
    }

    private Vector3 CalculateSafeStrikeDirection(
        Vector3 puckPosition
    )
    {
        Vector3 targetPoint =
            opponentGoal.position;

        Bounds areaBounds =
            movementArea.bounds;

        float distanceToLowerWall =
            puckPosition.z -
            areaBounds.min.z;

        float distanceToUpperWall =
            areaBounds.max.z -
            puckPosition.z;

        bool closeToSideWall =
            distanceToLowerWall <
            wallAimDistance ||
            distanceToUpperWall <
            wallAimDistance;

        if (closeToSideWall)
        {
            /*
             * Si el disco está cerca de una pared,
             * primero intenta enviarlo hacia el centro.
             */
            targetPoint.z =
                centerPoint.position.z;
        }
        else
        {
            targetPoint +=
                lateralAxis *
                currentAimError;
        }

        Vector3 direction =
            targetPoint -
            puckPosition;

        direction.y = 0f;

        if (direction.sqrMagnitude <
            0.001f)
        {
            direction = attackAxis;
        }

        direction.Normalize();

        /*
         * Limitamos la componente lateral.
         * Así ningún error de puntería puede convertir
         * el golpe en un disparo hacia su propia meta.
         */
        float lateralAmount =
            Vector3.Dot(
                direction,
                lateralAxis
            );

        lateralAmount = Mathf.Clamp(
            lateralAmount,
            -0.65f,
            0.65f
        );

        direction =
            attackAxis +
            lateralAxis *
            lateralAmount;

        direction.y = 0f;

        return direction.normalized;
    }

    // =========================================================
    // RECUPERACIÓN CUANDO EL DISCO QUEDA DETRÁS
    // =========================================================

    private void BeginRecovery()
    {
        recoverySideSign =
            ChooseRecoverySide();

        SetState(AIState.RecoverSide);
    }

    private float ChooseRecoverySide()
    {
        Vector3 plusCandidate =
            puck.position +
            lateralAxis *
            recoverySideOffset;

        Vector3 minusCandidate =
            puck.position -
            lateralAxis *
            recoverySideOffset;

        plusCandidate =
            ClampPositionToArea(
                plusCandidate
            );

        minusCandidate =
            ClampPositionToArea(
                minusCandidate
            );

        float plusDistance =
            FlatDistance(
                plusCandidate,
                puck.position
            );

        float minusDistance =
            FlatDistance(
                minusCandidate,
                puck.position
            );

        return plusDistance >= minusDistance
            ? 1f
            : -1f;
    }

    private Vector3 CalculateRecoverySideTarget()
    {
        float currentProgress =
            GetForwardPosition(
                body.position
            );

        float desiredLateral =
            GetLateralPosition(
                puck.position
            ) +
            recoverySideSign *
            recoverySideOffset;

        Vector3 target =
            ComposePosition(
                currentProgress,
                desiredLateral
            );

        float currentLateralSeparation =
            Mathf.Abs(
                GetLateralPosition(
                    body.position
                ) -
                GetLateralPosition(
                    puck.position
                )
            );

        if (currentLateralSeparation >=
            recoverySideOffset * 0.75f)
        {
            SetState(
                AIState.RecoverBehind
            );

            return
                CalculateRecoveryBehindTarget();
        }

        return target;
    }

    private Vector3 CalculateRecoveryBehindTarget()
    {
        Vector3 target =
            puck.position -
            attackAxis *
            behindDistance +
            lateralAxis *
            recoverySideSign *
            recoverySideOffset;

        target.y =
            movementHeight;

        target =
            ClampPositionToArea(target);

        bool reachedBehind =
            FlatDistance(
                body.position,
                target
            ) <= recoveryArrivalDistance;

        if (reachedBehind &&
            IsMalletGoalSideOfPuck())
        {
            if (IsEmergencySituation())
            {
                SetState(
                    AIState.EmergencyBlock
                );

                return
                    CalculateEmergencyTarget();
            }

            SetState(AIState.Approach);

            return
                CalculateApproachTarget();
        }

        return target;
    }

    // =========================================================
    // DEFENSA DE EMERGENCIA
    // =========================================================

    private bool IsEmergencySituation()
    {
        float puckProgress =
            GetForwardPosition(
                puck.position
            );

        float halfTableLength =
            Mathf.Abs(
                GetForwardPosition(
                    centerPoint.position
                )
            );

        float emergencyDistance =
            halfTableLength *
            emergencyDistanceRatio;

        float velocityTowardOpponent =
            Vector3.Dot(
                perceivedPuckVelocity,
                attackAxis
            );

        bool puckCloseToOwnGoal =
            puckProgress <=
            emergencyDistance;

        bool puckMovingTowardOwnGoal =
            velocityTowardOpponent < -0.20f &&
            puckProgress <=
            emergencyDistance * 1.55f;

        return
            puckCloseToOwnGoal ||
            puckMovingTowardOwnGoal;
    }

    private Vector3 CalculateEmergencyTarget()
    {
        /*
         * La IA se coloca entre su portería y el disco.
         * Nunca intenta entrar por el lado delantero.
         */
        Vector3 blockPosition =
            puck.position -
            attackAxis *
            emergencyBehindDistance;

        blockPosition.y =
            movementHeight;

        blockPosition =
            ClampPositionToArea(
                blockPosition
            );

        float distanceToBlock =
            FlatDistance(
                body.position,
                blockPosition
            );

        if (distanceToBlock <=
            strikeTriggerDistance &&
            IsMalletGoalSideOfPuck())
        {
            Vector3 clearDirection =
                CalculateSafeStrikeDirection(
                    puck.position
                );

            BeginStrike(clearDirection);

            return CalculateStrikeTarget();
        }

        return blockPosition;
    }

    // =========================================================
    // MOVIMIENTO SUAVE
    // =========================================================

    private void MoveTowardTarget(
        Vector3 target
    )
    {
        Vector3 position =
            body.position;

        Vector3 toTarget =
            target - position;

        toTarget.y = 0f;

        float distance =
            toTarget.magnitude;

        float stateSpeed =
            GetCurrentStateSpeed();

        Vector3 desiredVelocity;

        if (distance <= stoppingDistance)
        {
            desiredVelocity =
                Vector3.zero;
        }
        else
        {
            float slowingFactor =
                Mathf.Clamp01(
                    distance /
                    slowingDistance
                );

            desiredVelocity =
                toTarget.normalized *
                stateSpeed *
                slowingFactor;
        }

        float usedAcceleration =
            desiredVelocity.sqrMagnitude >
            movementVelocity.sqrMagnitude
                ? acceleration
                : braking;

        movementVelocity =
            Vector3.MoveTowards(
                movementVelocity,
                desiredVelocity,
                usedAcceleration *
                Time.fixedDeltaTime
            );

        if (distance <= stoppingDistance &&
            movementVelocity.magnitude <
            0.04f)
        {
            movementVelocity =
                Vector3.zero;
        }

        Vector3 nextPosition =
            position +
            movementVelocity *
            Time.fixedDeltaTime;

        /*
         * Evita pasarse del objetivo y regresar,
         * que era una de las causas del temblor.
         */
        if (distance > 0f)
        {
            Vector3 remaining =
                target - nextPosition;

            remaining.y = 0f;

            if (Vector3.Dot(
                    remaining,
                    toTarget
                ) < 0f)
            {
                nextPosition =
                    target;

                movementVelocity =
                    Vector3.zero;
            }
        }

        nextPosition =
            ClampPositionToArea(
                nextPosition
            );

        nextPosition.y =
            movementHeight;

        body.MovePosition(
            nextPosition
        );
    }

    private float GetCurrentStateSpeed()
    {
        switch (currentState)
        {
            case AIState.Strike:
                return strikeSpeed;

            case AIState.RecoverSide:
            case AIState.RecoverBehind:
                return recoverySpeed;

            case AIState.Approach:
            case AIState.EmergencyBlock:
                return approachSpeed;

            default:
                return guardSpeed;
        }
    }

    // =========================================================
    // POSICIONES RELATIVAS
    // =========================================================

    private void UpdateTableAxes()
    {
        if (ownGoal == null ||
            opponentGoal == null)
        {
            return;
        }

        attackAxis =
            opponentGoal.position -
            ownGoal.position;

        attackAxis.y = 0f;

        if (attackAxis.sqrMagnitude <
            0.001f)
        {
            attackAxis =
                Vector3.right;
        }

        attackAxis.Normalize();

        lateralAxis =
            Vector3.Cross(
                Vector3.up,
                attackAxis
            );

        lateralAxis.y = 0f;
        lateralAxis.Normalize();
    }

    private bool IsPuckOnAISide(
        Vector3 puckPosition
    )
    {
        Vector3 fromCenter =
            puckPosition -
            centerPoint.position;

        fromCenter.y = 0f;

        float forwardValue =
            Vector3.Dot(
                fromCenter,
                attackAxis
            );

        return forwardValue <= 0.04f;
    }

    private bool IsMalletGoalSideOfPuck()
    {
        float malletProgress =
            GetForwardPosition(
                body.position
            );

        float puckProgress =
            GetForwardPosition(
                puck.position
            );

        return malletProgress <=
               puckProgress -
               goalSideMargin;
    }

    private float GetForwardPosition(
        Vector3 worldPosition
    )
    {
        Vector3 relative =
            worldPosition -
            ownGoal.position;

        relative.y = 0f;

        return Vector3.Dot(
            relative,
            attackAxis
        );
    }

    private float GetLateralPosition(
        Vector3 worldPosition
    )
    {
        Vector3 relative =
            worldPosition -
            ownGoal.position;

        relative.y = 0f;

        return Vector3.Dot(
            relative,
            lateralAxis
        );
    }

    private Vector3 ComposePosition(
        float forward,
        float lateral
    )
    {
        Vector3 result =
            ownGoal.position +
            attackAxis * forward +
            lateralAxis * lateral;

        result.y =
            movementHeight;

        return result;
    }

    private static float FlatDistance(
        Vector3 first,
        Vector3 second
    )
    {
        first.y = 0f;
        second.y = 0f;

        return Vector3.Distance(
            first,
            second
        );
    }

    // =========================================================
    // LÍMITES
    // =========================================================

    private Vector3 ClampPositionToArea(
        Vector3 worldPosition
    )
    {
        worldPosition.y =
            movementHeight;

        if (movementArea == null)
            return worldPosition;

        Bounds areaBounds =
            movementArea.bounds;

        Vector3 malletExtents =
            GetMalletWorldExtents();

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

        if (minimumX > maximumX)
        {
            minimumX =
                areaBounds.center.x;

            maximumX =
                areaBounds.center.x;
        }

        if (minimumZ > maximumZ)
        {
            minimumZ =
                areaBounds.center.z;

            maximumZ =
                areaBounds.center.z;
        }

        worldPosition.x =
            Mathf.Clamp(
                worldPosition.x,
                minimumX,
                maximumX
            );

        worldPosition.z =
            Mathf.Clamp(
                worldPosition.z,
                minimumZ,
                maximumZ
            );

        worldPosition.y =
            movementHeight;

        return worldPosition;
    }

    private Vector3 GetMalletWorldExtents()
    {
        if (malletCollider == null)
        {
            return new Vector3(
                0.2f,
                0f,
                0.2f
            );
        }

        return
            malletCollider.bounds.extents;
    }

    // =========================================================
    // ACTIVACIÓN
    // =========================================================

    public void SetControlEnabled(
        bool enabled
    )
    {
        controlEnabled = enabled;

        movementVelocity =
            Vector3.zero;

        strikeTimer = 0f;

        SetState(AIState.Guard);

        if (body == null)
            return;

        Vector3 correctedPosition =
            ClampPositionToArea(
                body.position
            );

        if (enabled)
        {
            body.position =
                correctedPosition;

            transform.position =
                correctedPosition;

            RefreshPerception(true);

            Physics.SyncTransforms();
        }
    }

    // =========================================================
    // REFERENCIAS Y VALIDACIÓN
    // =========================================================

    private bool HasRequiredReferences()
    {
        return
            puck != null &&
            movementArea != null &&
            homePoint != null &&
            ownGoal != null &&
            opponentGoal != null &&
            centerPoint != null;
    }

    private void FindPuckReferencesIfMissing()
    {
        if (puckBody == null &&
            puck != null)
        {
            puckBody =
                puck.GetComponent<Rigidbody>();
        }

        if (puckController == null &&
            puck != null)
        {
            puckController =
                puck.GetComponent<AirHockeyPuck>();
        }
    }

    private void FindMalletColliderIfMissing()
    {
        if (malletCollider != null)
            return;

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (
            Collider currentCollider
            in colliders
        )
        {
            if (currentCollider == null)
                continue;

            if (currentCollider.isTrigger)
                continue;

            malletCollider =
                currentCollider;

            break;
        }
    }

    private void OnValidate()
    {
        extraPadding =
            Mathf.Max(
                0f,
                extraPadding
            );

        FindMalletColliderIfMissing();
        FindPuckReferencesIfMissing();

        ApplyDifficultySettings();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugTarget)
            return;

        Gizmos.color =
            Color.red;

        Gizmos.DrawSphere(
            currentTarget,
            0.045f
        );

        Gizmos.DrawLine(
            transform.position,
            currentTarget
        );

        if (homePoint != null)
        {
            Gizmos.color =
                Color.cyan;

            Gizmos.DrawWireSphere(
                homePoint.position,
                0.08f
            );
        }
    }
}