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

    [Header("Bloqueo de controles del jugador")]
    [SerializeField] private MonoBehaviour[] componentsToDisableWhileOpen;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Opciones")]
    [SerializeField] private bool closeWithEscape = true;

    [Header("Cursor")]
    [SerializeField] private CursorLockManager cursorLockManager;

    [Header("Cámara")]
    [SerializeField] private RoomCameraManager roomCameraManager;

    private MovieCardUI selectedMovie;
    private bool isSelectorOpen;
    private bool isWatching;

    // Referencias del personaje instanciado, conectadas en runtime
    private MonoBehaviour boundPlayerInput;
    private CharacterMover boundMover;

    private void Start()
    {
        InitCards();

        if (watchButton != null)
            watchButton.onClick.AddListener(WatchSelectedMovie);

        if (movieCards.Length > 0)
            SelectMovie(movieCards[0]);

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);

        // Asegura que la pantalla empiece en negro
        if (movieScreenPlayer != null)
            movieScreenPlayer.ClearScreen();
    }

    private void Update()
    {
        if (isSelectorOpen && closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
            CloseSelector();
    }

    private void InitCards()
    {
        foreach (MovieCardUI card in movieCards)
        {
            if (card == null) continue;
            card.Init(this);
            card.SetSelected(false);
        }
    }

    // Llamado por el setup tras instanciar el personaje, para poder congelar y frenar su movimiento
    public void BindPlayerInput(MonoBehaviour playerInput)
    {
        boundPlayerInput = playerInput;

        // Guardamos también el CharacterMover del mismo personaje para poder frenarlo
        if (playerInput != null)
            boundMover = playerInput.GetComponent<CharacterMover>();
    }

    public void OpenSelector()
    {
        isSelectorOpen = true;
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayAbrirMenu();
        if (selectorPanel != null)
            selectorPanel.SetActive(true);

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);

        SetPlayerControlsEnabled(false);
        ShowCursor(true);
    }

    public void CloseSelector()
    {
        isSelectorOpen = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        // Solo devolvemos el control si NO estamos viendo película
        if (!isWatching)
        {
            SetPlayerControlsEnabled(true);
            ShowCursor(false);
        }
        
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayCerrarMenu();
    }

    public void SelectMovie(MovieCardUI movieCard)
    {
        if (movieCard == null) return;

        if (selectedMovie != null)
            selectedMovie.SetSelected(false);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlaySeleccion();
        selectedMovie = movieCard;
        selectedMovie.SetSelected(true);
        selectedMovie.PlaySelectionAnimation();
    }

    private void WatchSelectedMovie()
    {
        if (selectedMovie == null)
        {
            Debug.LogWarning("MovieSelectorController: No hay película seleccionada.");
            return;
        }

        if (movieScreenPlayer != null)
            movieScreenPlayer.PlayMovie(selectedMovie);

        // Entramos a modo "viendo película"
        isWatching = true;
        isSelectorOpen = false;

        // Oculta el panel de selección pero MANTIENE el control bloqueado y el cursor visible
        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        // Frena el movimiento residual del personaje antes de congelar
        if (boundMover != null)
            boundMover.ResetToIdle();

        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        if (roomCameraManager != null)
            roomCameraManager.ActivateZoneCamera();

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(true);
        
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PausarMusica(); 
    }

    // Botón "salir": vuelve al control normal del personaje y cámara libre
    public void StopWatching()
    {
        isWatching = false;

        if (movieScreenPlayer != null)
        {
            movieScreenPlayer.Stop();
            movieScreenPlayer.ClearScreen(); // deja la pantalla en negro de nuevo
        }

        if (roomCameraManager != null)
            roomCameraManager.ActivateFollowCamera();

        if (stopWatchingButton != null)
            stopWatchingButton.SetActive(false);
        
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayCerrarMenu();
        
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.ReanudarMusica();
        // Devuelve el control normal y oculta el cursor
        SetPlayerControlsEnabled(true);
        ShowCursor(false);
    }

    private void ShowCursor(bool show)
    {
        if (cursorLockManager != null)
        {
            cursorLockManager.SetInterfaceMode(show);
        }
        else
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        // Controles asignados en el editor (si los hay)
        foreach (MonoBehaviour component in componentsToDisableWhileOpen)
        {
            if (component != null)
                component.enabled = enabled;
        }

        // El input del personaje instanciado (incluye la cámara: al desactivarlo, no se mueve)
        if (boundPlayerInput != null)
            boundPlayerInput.enabled = enabled;

        if (playerRigidbody != null && !enabled)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
