using Controller;
using UnityEngine;
using UnityEngine.UI;

public class MovieSelectorController : MonoBehaviour
{
    [Header("Panel principal")]
    [SerializeField] private GameObject selectorPanel;

    [Header("Cards")]
    [SerializeField] private MovieCardUI[] movieCards;

    [Header("Botón ver")]
    [SerializeField] private Button watchButton;

    [Header("Botón dejar de ver")]
    [SerializeField] private GameObject stopWatchingButton;

    [Header("Pantalla de cine")]
    [SerializeField] private MovieScreenPlayer movieScreenPlayer;

    [Header("Trivia")]
    [SerializeField] private MovieTriviaController movieTriviaController;

    [Header("Bloqueo de controles del jugador")]
    [SerializeField] private MonoBehaviour[] componentsToDisableWhileOpen;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Controles móviles")]
    [Tooltip("Objeto raíz que contiene joystick y controles táctiles del jugador.")]
    [SerializeField] private GameObject controlesTactiles;

    [Header("Opciones")]
    [SerializeField] private bool closeWithEscape = true;

    [Header("Cursor")]
    [SerializeField] private CursorLockManager cursorLockManager;

    [Header("Cámara cine / trivia")]
    [SerializeField] private CinemaMainCameraMover cinemaCameraMover;


    private MovieCardUI selectedMovie;

    private bool isSelectorOpen;
    private bool isWatching;
    private bool isTriviaOpen;

    public bool IsCinemaInteractionBusy =>
        isSelectorOpen ||
        isWatching ||
        isTriviaOpen;


    private MonoBehaviour boundPlayerInput;
    private CharacterMover boundMover;
    private Behaviour boundCameraComponent;

    private Button stopWatchingButtonComponent;


    // =========================================================
    // CONTROLES TÁCTILES
    // =========================================================

    private bool controlesTactilesEstabanActivos;
    private bool estadoControlesTactilesGuardado;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        InitCards();
        InitButtons();

        if (movieCards != null && movieCards.Length > 0)
            SelectMovie(movieCards[0]);

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);

        if (movieTriviaController != null)
            movieTriviaController.CloseInstant();

        if (movieScreenPlayer != null)
            movieScreenPlayer.ClearScreen();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (isSelectorOpen &&
            closeWithEscape &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSelector();
        }

        if ((isWatching || isTriviaOpen) &&
            closeWithEscape &&
            Input.GetKeyDown(KeyCode.Escape))
        {
            StopWatching();
        }
    }


    // =========================================================
    // BOTONES
    // =========================================================

    private void InitButtons()
    {
        if (watchButton != null)
        {
            watchButton.onClick.RemoveListener(WatchSelectedMovie);
            watchButton.onClick.AddListener(WatchSelectedMovie);
        }

        if (stopWatchingButton != null)
        {
            stopWatchingButtonComponent =
                stopWatchingButton.GetComponent<Button>();

            if (stopWatchingButtonComponent == null)
            {
                stopWatchingButtonComponent =
                    stopWatchingButton.GetComponentInChildren<Button>(true);
            }

            if (stopWatchingButtonComponent != null)
            {
                stopWatchingButtonComponent.onClick.RemoveListener(
                    StopWatching
                );

                stopWatchingButtonComponent.onClick.AddListener(
                    StopWatching
                );
            }
            else
            {
                Debug.LogWarning(
                    "MovieSelectorController: El objeto asignado " +
                    "en Stop Watching Button no tiene componente Button."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "MovieSelectorController: No se asignó " +
                "Stop Watching Button en el inspector."
            );
        }
    }


    // =========================================================
    // CARDS
    // =========================================================

    private void InitCards()
    {
        if (movieCards == null)
            return;

        foreach (MovieCardUI card in movieCards)
        {
            if (card == null)
                continue;

            card.Init(this);
            card.SetSelected(false);
        }
    }


    // =========================================================
    // JUGADOR
    // =========================================================

    public void BindPlayerInput(MonoBehaviour playerInput)
    {
        boundPlayerInput = playerInput;

        if (playerInput != null)
        {
            boundMover =
                playerInput.GetComponent<CharacterMover>();

            MovePlayerInput mpi =
                playerInput as MovePlayerInput;

            if (mpi != null && mpi.Camera != null)
                boundCameraComponent = mpi.Camera;

            Debug.Log(
                $"BindPlayerInput: " +
                $"mpi={(mpi != null ? "OK" : "NULL")} | " +
                $"camara={(boundCameraComponent != null ? boundCameraComponent.name : "NULL")}"
            );
        }
    }


    // =========================================================
    // ABRIR SELECTOR
    // =========================================================

    public void OpenSelector()
    {
        if (isTriviaOpen)
            return;

        // Ocultamos joystick y controles móviles.
        OcultarControlesTactiles();

        isSelectorOpen = true;

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayAbrirMenu();

        if (selectorPanel != null)
            selectorPanel.SetActive(true);

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);

        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        Debug.Log(
            $"OpenSelector llamado. " +
            $"boundPlayerInput={(boundPlayerInput != null ? "OK" : "NULL")} | " +
            $"components={(componentsToDisableWhileOpen != null ? componentsToDisableWhileOpen.Length : 0)}"
        );
    }


    // =========================================================
    // CERRAR SELECTOR
    // =========================================================

    public void CloseSelector()
    {
        isSelectorOpen = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (!isWatching && !isTriviaOpen)
        {
            SetPlayerControlsEnabled(true);
            ShowCursor(false);

            // Ya regresamos completamente al juego.
            RestaurarControlesTactiles();
        }

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayCerrarMenu();
    }


    // =========================================================
    // SELECCIONAR PELÍCULA
    // =========================================================

    public void SelectMovie(MovieCardUI movieCard)
    {
        if (movieCard == null)
            return;

        if (selectedMovie != null)
            selectedMovie.SetSelected(false);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlaySeleccion();

        selectedMovie = movieCard;

        selectedMovie.SetSelected(true);
        selectedMovie.PlaySelectionAnimation();
    }


    // =========================================================
    // VER PELÍCULA
    // =========================================================

    private void WatchSelectedMovie()
    {
        if (selectedMovie == null)
        {
            Debug.LogWarning(
                "MovieSelectorController: No hay película seleccionada."
            );

            return;
        }

        // Nos aseguramos de que los controles móviles
        // permanezcan ocultos durante la película.
        OcultarControlesTactiles();

        if (movieScreenPlayer != null)
            movieScreenPlayer.PlayMovie(selectedMovie);

        isWatching = true;
        isSelectorOpen = false;
        isTriviaOpen = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (movieTriviaController != null)
            movieTriviaController.CloseInstant();

        if (boundMover != null)
            boundMover.ResetToIdle();

        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        if (cinemaCameraMover != null)
            cinemaCameraMover.ActivateCinemaCamera();

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(true);

        if (stopWatchingButtonComponent != null)
            stopWatchingButtonComponent.interactable = true;

        Debug.Log("WatchSelectedMovie llamado");

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PausarMusica();
    }


    // =========================================================
    // ABRIR TRIVIA
    // =========================================================

    public void OpenTriviaForMovie(MovieCardUI movieCard)
    {
        if (movieCard == null)
            return;

        if (movieTriviaController == null)
        {
            Debug.LogWarning(
                "MovieSelectorController: Falta asignar MovieTriviaController."
            );

            return;
        }

        if (movieCard.TriviaData == null ||
            movieCard.TriviaData.questions == null ||
            movieCard.TriviaData.questions.Count == 0)
        {
            Debug.LogWarning(
                $"MovieSelectorController: La película " +
                $"{movieCard.MovieId} no tiene trivia configurada."
            );

            return;
        }

        // La trivia también cuenta como interfaz.
        OcultarControlesTactiles();

        SelectMovie(movieCard);

        isTriviaOpen = true;
        isSelectorOpen = false;
        isWatching = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);

        if (movieScreenPlayer != null)
            movieScreenPlayer.Pause();

        if (boundMover != null)
            boundMover.ResetToIdle();

        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        if (cinemaCameraMover != null)
            cinemaCameraMover.ActivateCinemaCamera();

        movieTriviaController.OpenTrivia(
            movieCard.TriviaData,
            movieCard.MovieTitle,
            HandleTriviaFinished
        );
    }


    // =========================================================
    // TERMINAR TRIVIA
    // =========================================================

    private void HandleTriviaFinished(
        int correctAnswers,
        int totalQuestions
    )
    {
        Debug.Log(
            $"MovieSelectorController: Trivia terminada. " +
            $"Resultado: {correctAnswers}/{totalQuestions}"
        );

        isTriviaOpen = false;
        isWatching = false;
        isSelectorOpen = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);

        if (movieScreenPlayer != null)
            movieScreenPlayer.Pause();

        if (cinemaCameraMover != null)
            cinemaCameraMover.ActivatePlayerCamera();

        SetPlayerControlsEnabled(true);
        ShowCursor(false);

        // Regresamos al juego normal.
        RestaurarControlesTactiles();

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.ReanudarMusica();

        // Aquí después conectamos backend:
        // - Guardar quiz completado
        // - Enviar aciertos
        // - Otorgar Ditas si es la primera vez
        // - Registrar evento de métricas
    }


    // =========================================================
    // DEJAR DE VER / SALIR
    // =========================================================

    public void StopWatching()
    {
        Debug.Log(
            "MovieSelectorController: StopWatching ejecutado."
        );

        isWatching = false;
        isTriviaOpen = false;
        isSelectorOpen = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (movieTriviaController != null)
            movieTriviaController.CloseInstant();

        if (movieScreenPlayer != null)
        {
            movieScreenPlayer.Stop();
            movieScreenPlayer.ClearScreen();
        }

        if (cinemaCameraMover != null)
            cinemaCameraMover.ActivatePlayerCamera();

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);

        if (UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayCerrarMenu();
            UISoundManager.Instance.ReanudarMusica();
        }

        SetPlayerControlsEnabled(true);
        ShowCursor(false);

        // Ya terminó completamente la interacción del cine.
        RestaurarControlesTactiles();
    }


    // =========================================================
    // CONTROLES TÁCTILES
    // =========================================================

    private void OcultarControlesTactiles()
    {
        if (controlesTactiles == null)
            return;

        /*
         * Guardamos el estado solamente la primera vez.
         *
         * Esto es importante porque podemos pasar:
         * selector → película → trivia
         *
         * sin sobrescribir el estado original.
         */
        if (!estadoControlesTactilesGuardado)
        {
            controlesTactilesEstabanActivos =
                controlesTactiles.activeSelf;

            estadoControlesTactilesGuardado = true;
        }

        controlesTactiles.SetActive(false);
    }


    private void RestaurarControlesTactiles()
    {
        if (controlesTactiles == null)
            return;

        if (!estadoControlesTactilesGuardado)
            return;

        controlesTactiles.SetActive(
            controlesTactilesEstabanActivos
        );

        estadoControlesTactilesGuardado = false;
    }


    // =========================================================
    // CURSOR
    // =========================================================

    private void ShowCursor(bool show)
    {
        if (cursorLockManager != null)
        {
            cursorLockManager.SetInterfaceMode(show);
        }
        else
        {
            Cursor.visible = show;

            Cursor.lockState = show
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
    }


    // =========================================================
    // CONTROLES DEL JUGADOR
    // =========================================================

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (componentsToDisableWhileOpen != null)
        {
            foreach (
                MonoBehaviour component
                in componentsToDisableWhileOpen
            )
            {
                if (component != null)
                    component.enabled = enabled;
            }
        }

        if (boundPlayerInput != null)
            boundPlayerInput.enabled = enabled;

        if (boundCameraComponent != null)
            boundCameraComponent.enabled = enabled;

        Debug.Log(
            $"SetPlayerControlsEnabled({enabled}): " +
            $"input={(boundPlayerInput != null ? "OK" : "NULL")} | " +
            $"camara={(boundCameraComponent != null ? "OK" : "NULL")}"
        );

        if (playerRigidbody != null && !enabled)
        {
#if UNITY_6000_0_OR_NEWER
            playerRigidbody.linearVelocity = Vector3.zero;
#else
            playerRigidbody.velocity = Vector3.zero;
#endif

            playerRigidbody.angularVelocity =
                Vector3.zero;
        }
    }
}