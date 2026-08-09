using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class AirHockeyGameManager : MonoBehaviour
{
    [Header("Cámaras")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera hockeyCamera;

    [Header("Interfaz del minijuego")]
    [Tooltip("Panel o Canvas que contiene el marcador y botón Cerrar.")]
    [SerializeField] private GameObject gameUI;

    [Tooltip("Texto del marcador del jugador.")]
    [SerializeField] private TMP_Text playerScoreText;

    [Tooltip("Texto del marcador de la IA.")]
    [SerializeField] private TMP_Text aiScoreText;

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
    [Tooltip("Posición del disco al iniciar una partida nueva.")]
    [SerializeField] private Transform initialPuckSpawn;

    [Tooltip("Posición del disco después de que anota el jugador.")]
    [SerializeField] private Transform playerPuckSpawn;

    [Tooltip("Posición del disco después de que anota la IA.")]
    [SerializeField] private Transform aiPuckSpawn;

    [Header("Configuración")]
    [SerializeField] private KeyCode exitKey =
        KeyCode.Escape;

    [Tooltip("Tiempo entre el gol y el nuevo saque.")]
    [SerializeField] private float goalResetDelay = 0.8f;

    [Tooltip("Tiempo que espera la IA cuando el disco aparece en su lado.")]
    [SerializeField] private float aiServeDelay = 2f;

    public bool IsPlaying { get; private set; }

    public event Action GameStarted;
    public event Action GameStopped;

    private int playerScore;
    private int aiScore;

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

        if (gameUI != null)
        {
            gameUI.SetActive(false);
        }

        SetPlayerControllerActive(false);
        SetAIControllerActive(false);

        ResetScores();
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

    // =========================================================
    // INICIAR PARTIDA
    // =========================================================

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
         * Cada vez que entramos al minijuego
         * empezamos 0 - 0.
         */
        ResetScores();

        if (gameUI != null)
        {
            gameUI.SetActive(true);
        }

        /*
         * El primer disco aparece en la posición
         * inicial de la mesa.
         */
        PrepareServe(initialPuckSpawn);

        SetPlayerControllerActive(true);
        SetAIControllerActive(true);

        GameStarted?.Invoke();
    }

    // =========================================================
    // SALIR DE PARTIDA
    // =========================================================

    public void StopGame()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;

        StopResetCoroutine();

        SetPlayerControllerActive(false);
        SetAIControllerActive(false);

        /*
         * Dejamos las piezas preparadas para
         * la siguiente partida.
         */
        PrepareServe(initialPuckSpawn);

        if (gameUI != null)
        {
            gameUI.SetActive(false);
        }

        RestorePlayerCamera();
        RestorePlayerBehaviours();
        RestoreCursorState();

        GameStopped?.Invoke();
    }

    /*
     * Esta función está pensada específicamente
     * para conectarla al botón Cerrar del Canvas.
     */
    public void CloseGameFromUI()
    {
        StopGame();
    }

    // =========================================================
    // GOLES Y MARCADOR
    // =========================================================

    /// <summary>
    /// playerScored = true si anotó el jugador.
    /// playerScored = false si anotó la IA.
    /// </summary>
    public void RegisterGoal(bool playerScored)
    {
        if (!IsPlaying)
            return;

        if (resetCoroutine != null)
            return;

        if (playerScored)
        {
            playerScore++;

            Debug.Log(
                "Gol del jugador. Marcador: " +
                playerScore + " - " + aiScore
            );
        }
        else
        {
            aiScore++;

            Debug.Log(
                "Gol de la IA. Marcador: " +
                playerScore + " - " + aiScore
            );
        }

        UpdateScoreUI();

        resetCoroutine = StartCoroutine(
            ResetAfterGoal(playerScored)
        );
    }

    private void ResetScores()
    {
        playerScore = 0;
        aiScore = 0;

        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (playerScoreText != null)
        {
            playerScoreText.text =
                playerScore.ToString();
        }

        if (aiScoreText != null)
        {
            aiScoreText.text =
                aiScore.ToString();
        }
    }

    // =========================================================
    // REINICIO DESPUÉS DE GOL
    // =========================================================

    private IEnumerator ResetAfterGoal(
        bool playerScored
    )
    {
        /*
         * Congelamos temporalmente ambos controles.
         */
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
         * Como definimos anteriormente:
         *
         * Jugador anotó
         * → disco aparece en el lado del jugador.
         *
         * IA anotó
         * → disco aparece en el lado de la IA.
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
         * El jugador recupera siempre el control
         * inmediatamente.
         */
        SetPlayerControllerActive(true);

        bool puckIsOnAISide =
            !playerScored;

        if (puckIsOnAISide)
        {
            /*
             * La IA anotó.
             * El disco está ahora de su lado.
             *
             * Esperamos 2 segundos antes de
             * permitirle comenzar.
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
             * El disco está del lado del jugador.
             * La IA puede volver a estar activa,
             * pero su controlador debería mantenerse
             * en posición de guardia hasta que el
             * disco entre a su mitad.
             */
            SetAIControllerActive(true);
        }

        resetCoroutine = null;
    }

    // =========================================================
    // POSICIONES DE PIEZAS
    // =========================================================

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

        ResetPuck(
            selectedPuckSpawn
        );

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

        if (finalSpawn == null)
        {
            finalSpawn =
                initialPuckSpawn;
        }

        if (finalSpawn == null)
        {
            Debug.LogWarning(
                "AirHockeyGameManager: " +
                "No hay ningún spawn asignado al disco."
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
        if (body == null ||
            home == null)
        {
            return;
        }

        body.linearVelocity =
            Vector3.zero;

        body.angularVelocity =
            Vector3.zero;

        body.position =
            home.position;

        body.rotation =
            home.rotation;

        body.transform.position =
            home.position;

        body.transform.rotation =
            home.rotation;
    }

    // =========================================================
    // CONTROLADORES HOCKEY
    // =========================================================

    private void SetPlayerControllerActive(
        bool active
    )
    {
        if (playerMalletController != null)
        {
            playerMalletController
                .SetControlEnabled(active);
        }
    }

    private void SetAIControllerActive(
        bool active
    )
    {
        if (aiMalletController != null)
        {
            aiMalletController
                .SetControlEnabled(active);
        }
    }

    // =========================================================
    // COROUTINES
    // =========================================================

    private void StopResetCoroutine()
    {
        if (resetCoroutine == null)
            return;

        StopCoroutine(
            resetCoroutine
        );

        resetCoroutine = null;
    }

    // =========================================================
    // PERSONAJE
    // =========================================================

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

            behaviour.enabled =
                false;
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

    // =========================================================
    // CÁMARAS
    // =========================================================

    private void ChangeToHockeyCamera()
    {
        if (playerCamera != null)
        {
            playerCameraWasActive =
                playerCamera
                    .gameObject
                    .activeSelf;

            playerCamera
                .gameObject
                .SetActive(false);
        }

        if (hockeyCamera != null)
        {
            hockeyCameraWasActive =
                hockeyCamera
                    .gameObject
                    .activeSelf;

            hockeyCamera
                .gameObject
                .SetActive(true);
        }
    }

    private void RestorePlayerCamera()
    {
        if (hockeyCamera != null)
        {
            hockeyCamera
                .gameObject
                .SetActive(
                    hockeyCameraWasActive
                );
        }

        if (playerCamera != null)
        {
            playerCamera
                .gameObject
                .SetActive(
                    playerCameraWasActive
                );
        }
    }

    // =========================================================
    // CURSOR
    // =========================================================

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

    // =========================================================
    // UNITY
    // =========================================================

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
            Mathf.Max(
                0f,
                goalResetDelay
            );

        aiServeDelay =
            Mathf.Max(
                0f,
                aiServeDelay
            );
    }
}