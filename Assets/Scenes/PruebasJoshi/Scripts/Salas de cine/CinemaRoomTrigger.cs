using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CinemaRoomTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private MovieSelectorController movieSelectorController;

    [Header("Auto configuración")]
    [SerializeField] private bool autoFindControllerInParents = true;

    [Header("Input")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Jugador")]
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;

    private void Awake()
    {
        if (autoFindControllerInParents)
            RefreshLocalController();
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        RefreshLocalController();
    }

    private void Start()
    {
        if (autoFindControllerInParents)
            RefreshLocalController();

        UpdatePromptVisibility();
    }

    private void Update()
    {
        UpdatePromptVisibility();

        if (!playerInside)
            return;

        if (movieSelectorController != null && movieSelectorController.IsCinemaInteractionBusy)
            return;

        if (Input.GetKeyDown(interactionKey))
        {
            OpenMovieSelector();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = true;
        UpdatePromptVisibility();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = false;
        UpdatePromptVisibility();
    }

    [ContextMenu("Buscar MovieSelectorController local")]
    private void RefreshLocalController()
    {
        MovieSelectorController localController = GetComponentInParent<MovieSelectorController>();

        if (localController != null)
            movieSelectorController = localController;
    }

    private void OpenMovieSelector()
    {
        if (movieSelectorController == null && autoFindControllerInParents)
            RefreshLocalController();

        if (movieSelectorController != null)
            movieSelectorController.OpenSelector();
        else
            Debug.LogWarning($"{name}: No hay MovieSelectorController asignado.");

        UpdatePromptVisibility();
    }

    private void UpdatePromptVisibility()
    {
        if (interactionPrompt == null)
            return;

        bool playerIsFree = true;

        if (movieSelectorController != null)
            playerIsFree = !movieSelectorController.IsCinemaInteractionBusy;

        interactionPrompt.SetActive(playerInside && playerIsFree);
    }
}