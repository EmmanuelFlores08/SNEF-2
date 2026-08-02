using System;
using System.Collections;
using UnityEngine;

public class AirHockeyGameManager : MonoBehaviour
{
    [Header("Cámaras")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera hockeyCamera;

    [Header("Scripts del personaje que se desactivan")]
    [SerializeField]
    private Behaviour[] playerBehavioursToDisable;

    [Header("Controladores de hockey")]
    [SerializeField]
    private AirHockeyPlayerController playerMalletController;

    [SerializeField]
    private AirHockeyAIController aiMalletController;

    [Header("Elementos físicos")]
    [SerializeField] private AirHockeyPuck puck;

    [SerializeField]
    private Rigidbody playerMalletBody;

    [SerializeField]
    private Rigidbody aiMalletBody;

    [Header("Posiciones iniciales de los mazos")]
    [SerializeField] private Transform playerHome;
    [SerializeField] private Transform aiHome;

    [Header("Posiciones del disco")]
    [Tooltip(
        "Posición original del disco al comenzar una nueva partida."
    )]
    [SerializeField] private Transform initialPuckSpawn;

    [Tooltip(
        "Posición del disco cuando el último punto fue del jugador."
    )]
    [SerializeField] private Transform playerPuckSpawn;

    [Tooltip(
        "Posición del disco cuando el último punto fue de la IA."
    )]
    [SerializeField] private Transform aiPuckSpawn;

    [Header("Configuración")]
    [SerializeField] private KeyCode exitKey =
        KeyCode.Escape;

    [Tooltip(
        "Tiempo entre el gol y la colocación del nuevo saque."
    )]
    [SerializeField] private float goalResetDelay = 0.8f;

    [Tooltip(
        "Tiempo que espera la IA cuando el disco aparece en su lado."
    )]
    [SerializeField] private float aiServeDelay = 2f;

    public bool IsPlaying { get; private set; }

    public event Action GameStarted;
    public event Action GameStopped;

    private bool[] previousBehaviourStates;

    private bool playerCameraWasActive;
    private bool hockeyCameraWasActive;

    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private Coroutine resetCoroutine;

    private void Awake()
    {
        if (hockeyCamera != null)
        {
            hockeyCamera.gameObject.SetActive(false);
        }

        SetPlayerControllerActive(false);
        SetAIControllerActive(false);
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        if (Input.GetKeyDown(exitKey))
        {
            StopGame();
        }
    }

    public void StartGame()
    {
        if (IsPlaying)
            return;

        IsPlaying = true;

        StopResetCoroutine();

        SaveCursorState();
        DisablePlayerBehaviours();
        ChangeToHockeyCamera();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        /*
         * Toda partida nueva comienza con el disco
         * en su posición inicial original.
         */
        PrepareServe(initialPuckSpawn);

        SetPlayerControllerActive(true);
        SetAIControllerActive(true);

        GameStarted?.Invoke();
    }

    public void StopGame()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;

        StopResetCoroutine();

        SetPlayerControllerActive(false);
        SetAIControllerActive(false);

        /*
         * Al salir dejamos la mesa preparada para
         * que la siguiente partida comience desde
         * la posición inicial.
         */
        PrepareServe(initialPuckSpawn);

        RestorePlayerCamera();
        RestorePlayerBehaviours();
        RestoreCursorState();

        GameStopped?.Invoke();
    }

    /// <summary>
    /// playerScored es true cuando anotó el jugador.
    /// Es false cuando anotó la IA.
    /// </summary>
    public void RegisterGoal(bool playerScored)
    {
        if (!IsPlaying)
            return;

        if (resetCoroutine != null)
            return;

        if (playerScored)
        {
            Debug.Log(
                "Gol del jugador. El disco aparecerá en el lado del jugador."
            );
        }
        else
        {
            Debug.Log(
                "Gol de la IA. El disco aparecerá en el lado de la IA."
            );
        }

        resetCoroutine = StartCoroutine(
            ResetAfterGoal(playerScored)
        );
    }

    private IEnumerator ResetAfterGoal(
        bool playerScored
    )
    {
        SetPlayerControllerActive(false);
        SetAIControllerActive(false);

        if (puck != null)
        {
            puck.StopPuck();
        }

        yield return new WaitForSeconds(
            goalResetDelay
        );

        /*
         * Si anotó el jugador, el disco queda del
         * lado del jugador.
         *
         * Si anotó la IA, queda del lado de la IA.
         */
        Transform nextSpawn = playerScored
            ? playerPuckSpawn
            : aiPuckSpawn;

        PrepareServe(nextSpawn);

        yield return new WaitForFixedUpdate();

        if (!IsPlaying)
        {
            resetCoroutine = null;
            yield break;
        }

        /*
         * El jugador recupera el control
         * inmediatamente.
         */
        SetPlayerControllerActive(true);

        bool puckIsOnAISide = !playerScored;

        if (puckIsOnAISide)
        {
            /*
             * La IA anotó y el disco apareció en
             * su lado. Esperamos dos segundos.
             */
            yield return new WaitForSeconds(
                aiServeDelay
            );

            if (IsPlaying)
            {
                SetAIControllerActive(true);
            }
        }
        else
        {
            /*
             * El disco apareció del lado del jugador.
             * La IA puede estar activa, pero solamente
             * permanecerá en su mitad hasta que el
             * disco cruce la línea central.
             */
            SetAIControllerActive(true);
        }

        resetCoroutine = null;
    }

    private void PrepareServe(
        Transform selectedPuckSpawn
    )
    {
        ResetMallet(
            playerMalletBody,
            playerHome
        );

        ResetMallet(
            aiMalletBody,
            aiHome
        );

        ResetPuck(selectedPuckSpawn);

        Physics.SyncTransforms();
    }

    private void ResetPuck(
        Transform selectedSpawn
    )
    {
        if (puck == null)
            return;

        Transform finalSpawn =
            selectedSpawn;

        /*
         * Si falta accidentalmente un spawn de saque,
         * utilizamos primero la posición inicial.
         */
        if (finalSpawn == null)
        {
            finalSpawn = initialPuckSpawn;
        }

        if (finalSpawn == null)
        {
            Debug.LogWarning(
                "AirHockeyGameManager: no hay un spawn asignado para el disco."
            );

            puck.StopPuck();
            return;
        }

        puck.ResetPuck(
            finalSpawn.position
        );
    }

    private static void ResetMallet(
        Rigidbody body,
        Transform home
    )
    {
        if (body == null || home == null)
            return;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        body.position = home.position;
        body.rotation = home.rotation;

        body.transform.position =
            home.position;

        body.transform.rotation =
            home.rotation;
    }

    private void SetPlayerControllerActive(
        bool active
    )
    {
        if (playerMalletController != null)
        {
            playerMalletController.SetControlEnabled(
                active
            );
        }
    }

    private void SetAIControllerActive(
        bool active
    )
    {
        if (aiMalletController != null)
        {
            aiMalletController.SetControlEnabled(
                active
            );
        }
    }

    private void StopResetCoroutine()
    {
        if (resetCoroutine == null)
            return;

        StopCoroutine(resetCoroutine);
        resetCoroutine = null;
    }

    private void DisablePlayerBehaviours()
    {
        if (playerBehavioursToDisable == null)
            return;

        previousBehaviourStates =
            new bool[
                playerBehavioursToDisable.Length
            ];

        for (
            int i = 0;
            i < playerBehavioursToDisable.Length;
            i++
        )
        {
            Behaviour behaviour =
                playerBehavioursToDisable[i];

            if (behaviour == null)
                continue;

            previousBehaviourStates[i] =
                behaviour.enabled;

            behaviour.enabled = false;
        }
    }

    private void RestorePlayerBehaviours()
    {
        if (playerBehavioursToDisable == null ||
            previousBehaviourStates == null)
        {
            return;
        }

        for (
            int i = 0;
            i < playerBehavioursToDisable.Length;
            i++
        )
        {
            Behaviour behaviour =
                playerBehavioursToDisable[i];

            if (behaviour == null)
                continue;

            behaviour.enabled =
                previousBehaviourStates[i];
        }
    }

    private void ChangeToHockeyCamera()
    {
        if (playerCamera != null)
        {
            playerCameraWasActive =
                playerCamera.gameObject.activeSelf;

            playerCamera.gameObject.SetActive(
                false
            );
        }

        if (hockeyCamera != null)
        {
            hockeyCameraWasActive =
                hockeyCamera.gameObject.activeSelf;

            hockeyCamera.gameObject.SetActive(
                true
            );
        }
    }

    private void RestorePlayerCamera()
    {
        if (hockeyCamera != null)
        {
            hockeyCamera.gameObject.SetActive(
                hockeyCameraWasActive
            );
        }

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(
                playerCameraWasActive
            );
        }
    }

    private void SaveCursorState()
    {
        previousCursorLockMode =
            Cursor.lockState;

        previousCursorVisible =
            Cursor.visible;
    }

    private void RestoreCursorState()
    {
        Cursor.lockState =
            previousCursorLockMode;

        Cursor.visible =
            previousCursorVisible;
    }

    private void OnDisable()
    {
        if (IsPlaying)
        {
            StopGame();
        }
    }

    private void OnValidate()
    {
        goalResetDelay =
            Mathf.Max(0f, goalResetDelay);

        aiServeDelay =
            Mathf.Max(0f, aiServeDelay);
    }
}