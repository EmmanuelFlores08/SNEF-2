using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class CinemaRoomTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;

    [Tooltip("Botón del propio prompt. En móvil se toca para abrir el selector.")]
    [SerializeField] private Button interactionButton;

    [SerializeField] private MovieSelectorController movieSelectorController;

    [Header("Auto configuración")]
    [SerializeField] private bool autoFindControllerInParents = true;

    [Header("Input")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Jugador")]
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (autoFindControllerInParents)
            RefreshLocalController();

        // Si no asignamos el botón manualmente,
        // intentamos encontrarlo en el propio prompt.
        if (interactionButton == null && interactionPrompt != null)
        {
            interactionButton =
                interactionPrompt.GetComponent<Button>();

            if (interactionButton == null)
            {
                interactionButton =
                    interactionPrompt.GetComponentInChildren<Button>(true);
            }
        }

        if (interactionButton != null)
        {
            interactionButton.onClick.RemoveListener(OpenMovieSelectorFromButton);
            interactionButton.onClick.AddListener(OpenMovieSelectorFromButton);
        }
    }


    // =========================================================
    // RESET
    // =========================================================

    private void Reset()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;

        RefreshLocalController();
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (autoFindControllerInParents)
            RefreshLocalController();

        UpdatePromptVisibility();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdatePromptVisibility();

        if (!PuedeInteractuar())
            return;

        // PC / teclado
        if (Input.GetKeyDown(interactionKey))
        {
            OpenMovieSelector();
        }
    }


    // =========================================================
    // BOTÓN MÓVIL
    // =========================================================

    public void OpenMovieSelectorFromButton()
    {
        if (!PuedeInteractuar())
            return;

        OpenMovieSelector();
    }


    // =========================================================
    // TRIGGER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other))
            return;

        playerInside = true;

        UpdatePromptVisibility();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other))
            return;

        playerInside = false;

        UpdatePromptVisibility();
    }


    // =========================================================
    // VALIDACIONES
    // =========================================================

    private bool PuedeInteractuar()
    {
        if (!playerInside)
            return false;

        if (movieSelectorController == null && autoFindControllerInParents)
            RefreshLocalController();

        if (movieSelectorController == null)
            return false;

        if (movieSelectorController.IsCinemaInteractionBusy)
            return false;

        return true;
    }

    private bool EsJugador(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Transform raiz = other.transform.root;

        return raiz != null &&
               raiz.CompareTag(playerTag);
    }


    // =========================================================
    // BUSCAR CONTROLADOR
    // =========================================================

    [ContextMenu("Buscar MovieSelectorController local")]
    private void RefreshLocalController()
    {
        MovieSelectorController localController =
            GetComponentInParent<MovieSelectorController>();

        if (localController != null)
            movieSelectorController = localController;
    }


    // =========================================================
    // ABRIR SELECTOR
    // =========================================================

    private void OpenMovieSelector()
    {
        if (movieSelectorController == null &&
            autoFindControllerInParents)
        {
            RefreshLocalController();
        }

        if (movieSelectorController != null)
        {
            // Ocultamos el prompt inmediatamente.
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            movieSelectorController.OpenSelector();
        }
        else
        {
            Debug.LogWarning(
                $"{name}: No hay MovieSelectorController asignado."
            );
        }

        UpdatePromptVisibility();
    }


    // =========================================================
    // PROMPT
    // =========================================================

    private void UpdatePromptVisibility()
    {
        if (interactionPrompt == null)
            return;

        bool playerIsFree = true;

        if (movieSelectorController != null)
        {
            playerIsFree =
                !movieSelectorController.IsCinemaInteractionBusy;
        }

        bool debeMostrarse =
            playerInside &&
            playerIsFree;

        if (interactionPrompt.activeSelf != debeMostrarse)
            interactionPrompt.SetActive(debeMostrarse);

        if (interactionButton != null)
            interactionButton.interactable = debeMostrarse;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (interactionButton != null)
        {
            interactionButton.onClick.RemoveListener(
                OpenMovieSelectorFromButton
            );
        }
    }
}