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

    [Header("Pantalla de cine")]
    [SerializeField] private MovieScreenPlayer movieScreenPlayer;

    [Header("Bloqueo de controles del jugador")]
    [SerializeField] private MonoBehaviour[] componentsToDisableWhileOpen;
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("Opciones")]
    [SerializeField] private bool closeWithEscape = true;

[Header("Cursor")]
[SerializeField] private CursorLockManager cursorLockManager;
    private MovieCardUI selectedMovie;
    private bool isSelectorOpen;

    private void Start()
    {
        InitCards();

        if (watchButton != null)
            watchButton.onClick.AddListener(WatchSelectedMovie);

        if (movieCards.Length > 0)
            SelectMovie(movieCards[0]);

        CloseSelector();
    }

    private void Update()
    {
        if (!isSelectorOpen)
            return;

        if (closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
            CloseSelector();
    }

    private void InitCards()
    {
        foreach (MovieCardUI card in movieCards)
        {
            if (card == null)
                continue;

            card.Init(this);
            card.SetSelected(false);
        }
    }

public void OpenSelector()
{
    isSelectorOpen = true;

    if (selectorPanel != null)
        selectorPanel.SetActive(true);

    SetPlayerControlsEnabled(false);

    if (cursorLockManager != null)
        cursorLockManager.SetInterfaceMode(true);
    else
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

public void CloseSelector()
{
    isSelectorOpen = false;

    if (selectorPanel != null)
        selectorPanel.SetActive(false);

    SetPlayerControlsEnabled(true);

    if (cursorLockManager != null)
        cursorLockManager.SetInterfaceMode(false);
    else
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}

    public void SelectMovie(MovieCardUI movieCard)
    {
        if (movieCard == null)
            return;

        if (selectedMovie != null)
            selectedMovie.SetSelected(false);

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
        {
            movieScreenPlayer.PlayMovie(selectedMovie);
        }
        else
        {
            Debug.LogWarning("MovieSelectorController: No se asignó MovieScreenPlayer.");
        }

        CloseSelector();

        // Aquí después registraremos métrica/backend:
        // event: movie_started_inside_cinema
        // movieId: selectedMovie.MovieId
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        foreach (MonoBehaviour component in componentsToDisableWhileOpen)
        {
            if (component != null)
                component.enabled = enabled;
        }

        if (playerRigidbody != null && !enabled)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }
}