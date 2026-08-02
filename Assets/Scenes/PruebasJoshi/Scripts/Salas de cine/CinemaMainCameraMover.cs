using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Controller;

public class CinemaMainCameraMover : MonoBehaviour
{
    [Header("Cámara principal")]
    [SerializeField] private Transform mainCamera;

    [Header("Punto fijo frente a la pantalla del cine")]
    [SerializeField] private Transform cinemaViewPoint;

    [Header("Movimiento")]
    [SerializeField] private float moveDuration = 0.45f;

    [Header("Comportamiento")]
    [SerializeField] private bool detachCameraWhileInCinema = true;
    [SerializeField] private bool lockCameraEveryFrame = true;
    [SerializeField] private bool animateReturnToPlayer = false;

    [Header("Desactivar cámara de jugador")]
    [Tooltip("Actívalo para desactivar automáticamente cualquier PlayerCamera/ThirdPersonCamera que esté en la Main Camera.")]
    [SerializeField] private bool disablePlayerCameraAutomatically = true;

    [Tooltip("Opcional: aquí puedes arrastrar otros scripts que muevan o roten la cámara.")]
    [SerializeField] private MonoBehaviour[] extraComponentsToDisableWhileInCinema;

    private Transform originalParent;
    private int originalSiblingIndex;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private Vector3 originalWorldPosition;
    private Quaternion originalWorldRotation;

    private bool hasSavedCameraState;
    private bool isInCinemaView;
    private bool isTransitioning;

    private Coroutine moveRoutine;
    private Coroutine hardLockRoutine;

    private readonly List<MonoBehaviour> disabledComponents = new List<MonoBehaviour>();

    private void Awake()
    {
        if (mainCamera == null && Camera.main != null)
            mainCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (!isInCinemaView)
            return;

        if (!lockCameraEveryFrame)
            return;

        if (isTransitioning)
            return;

        if (mainCamera == null || cinemaViewPoint == null)
            return;

        mainCamera.SetPositionAndRotation(cinemaViewPoint.position, cinemaViewPoint.rotation);
    }

    public void ActivateCinemaCamera()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("CinemaMainCameraMover: No hay Main Camera asignada.");
            return;
        }

        if (cinemaViewPoint == null)
        {
            Debug.LogWarning("CinemaMainCameraMover: No hay Cinema View Point asignado.");
            return;
        }

        if (!isInCinemaView)
            SaveCameraState();

        isInCinemaView = true;

        DisableCameraControlComponents();

        if (detachCameraWhileInCinema)
            mainCamera.SetParent(null, true);

        MoveCameraTo(
            cinemaViewPoint.position,
            cinemaViewPoint.rotation,
            true,
            StartHardLock
        );
    }

    public void ActivatePlayerCamera()
    {
        if (mainCamera == null)
            return;

        if (!hasSavedCameraState)
            return;

        isInCinemaView = false;

        StopHardLock();

        if (animateReturnToPlayer)
        {
            MoveCameraTo(
                originalWorldPosition,
                originalWorldRotation,
                true,
                RestoreCameraParentAndControls
            );
        }
        else
        {
            StopMoveRoutine();
            RestoreCameraParentAndControls();
        }
    }

    private void SaveCameraState()
    {
        originalParent = mainCamera.parent;
        originalSiblingIndex = mainCamera.GetSiblingIndex();

        originalLocalPosition = mainCamera.localPosition;
        originalLocalRotation = mainCamera.localRotation;

        originalWorldPosition = mainCamera.position;
        originalWorldRotation = mainCamera.rotation;

        hasSavedCameraState = true;
    }

    private void RestoreCameraParentAndControls()
    {
        if (mainCamera == null)
            return;

        if (detachCameraWhileInCinema && originalParent != null)
        {
            mainCamera.SetParent(originalParent, true);
            mainCamera.SetSiblingIndex(originalSiblingIndex);

            mainCamera.localPosition = originalLocalPosition;
            mainCamera.localRotation = originalLocalRotation;
        }
        else
        {
            mainCamera.SetPositionAndRotation(originalWorldPosition, originalWorldRotation);
        }

        EnableCameraControlComponents();
    }

    private void DisableCameraControlComponents()
    {
        disabledComponents.Clear();

        if (mainCamera == null)
            return;

        if (disablePlayerCameraAutomatically)
        {
            PlayerCamera[] playerCameraComponents = mainCamera.GetComponents<PlayerCamera>();

            foreach (PlayerCamera playerCamera in playerCameraComponents)
            {
                DisableComponentIfValid(playerCamera);
            }
        }

        if (extraComponentsToDisableWhileInCinema != null)
        {
            foreach (MonoBehaviour component in extraComponentsToDisableWhileInCinema)
            {
                DisableComponentIfValid(component);
            }
        }
    }

    private void DisableComponentIfValid(MonoBehaviour component)
    {
        if (component == null)
            return;

        if (component == this)
            return;

        if (!component.enabled)
            return;

        component.enabled = false;
        disabledComponents.Add(component);
    }

    private void EnableCameraControlComponents()
    {
        foreach (MonoBehaviour component in disabledComponents)
        {
            if (component != null)
                component.enabled = true;
        }

        disabledComponents.Clear();
    }

    private void MoveCameraTo(Vector3 targetPosition, Quaternion targetRotation, bool animated, System.Action onComplete)
    {
        StopMoveRoutine();

        if (!animated || moveDuration <= 0f)
        {
            mainCamera.SetPositionAndRotation(targetPosition, targetRotation);
            onComplete?.Invoke();
            return;
        }

        moveRoutine = StartCoroutine(MoveCameraRoutine(targetPosition, targetRotation, onComplete));
    }

    private IEnumerator MoveCameraRoutine(Vector3 targetPosition, Quaternion targetRotation, System.Action onComplete)
    {
        isTransitioning = true;

        Vector3 startPosition = mainCamera.position;
        Quaternion startRotation = mainCamera.rotation;

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / moveDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            mainCamera.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);

            yield return null;
        }

        mainCamera.SetPositionAndRotation(targetPosition, targetRotation);

        isTransitioning = false;
        moveRoutine = null;

        onComplete?.Invoke();
    }

    private void StartHardLock()
    {
        if (!lockCameraEveryFrame)
            return;

        StopHardLock();
        hardLockRoutine = StartCoroutine(HardLockEndOfFrameRoutine());
    }

    private IEnumerator HardLockEndOfFrameRoutine()
    {
        WaitForEndOfFrame wait = new WaitForEndOfFrame();

        while (isInCinemaView)
        {
            yield return wait;

            if (mainCamera != null && cinemaViewPoint != null)
                mainCamera.SetPositionAndRotation(cinemaViewPoint.position, cinemaViewPoint.rotation);
        }
    }

    private void StopHardLock()
    {
        if (hardLockRoutine != null)
        {
            StopCoroutine(hardLockRoutine);
            hardLockRoutine = null;
        }
    }

    private void StopMoveRoutine()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isTransitioning = false;
    }
}