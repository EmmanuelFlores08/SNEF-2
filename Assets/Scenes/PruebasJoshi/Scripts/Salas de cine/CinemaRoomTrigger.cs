using System.Collections.Generic;
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

    [Header("Metricas")]
    [SerializeField] private string sponsorId;

    private bool playerInside;
    private readonly HashSet<Transform> playerRootsInside = new HashSet<Transform>();
    private float salaEnterTime;
    private bool salaMetricActive;
    private bool missingSponsorWarningShown;


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

        HidePrompt();
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

        Transform playerRoot = GetPlayerRoot(other);
        if (playerRoot != null && !playerRootsInside.Add(playerRoot))
            return;

        if (!playerInside)
            SendSalaEnterMetric();

        playerInside = true;

        UpdatePromptVisibility();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other))
            return;

        Transform playerRoot = GetPlayerRoot(other);
        if (playerRoot != null)
        {
            playerRootsInside.Remove(playerRoot);

            if (playerRootsInside.Count > 0)
                return;
        }

        SendSalaExitMetric();

        playerInside = false;

        // Al salir, ocultamos el letrero explícitamente (aunque otra sala
        // comparta el mismo objeto, quien esté dentro lo volverá a mostrar).
        HidePrompt();
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

    private Transform GetPlayerRoot(Collider other)
    {
        if (other == null)
            return null;

        Transform raiz = other.transform.root;

        if (raiz != null && raiz.CompareTag(playerTag))
            return raiz;

        return other.CompareTag(playerTag)
            ? other.transform
            : null;
    }

    private void SendSalaEnterMetric()
    {
        if (!HasValidSponsorId("sala_enter"))
            return;

        salaEnterTime = Time.realtimeSinceStartup;
        salaMetricActive = true;
        SnefMetrics.Send("sala_enter", sponsorId);
    }

    private void SendSalaExitMetric()
    {
        if (!salaMetricActive)
            return;

        int seconds = Mathf.Max(
            0,
            Mathf.FloorToInt(Time.realtimeSinceStartup - salaEnterTime)
        );

        salaMetricActive = false;
        SnefMetrics.Send("sala_exit", $"{sponsorId}~{seconds}");
    }

    private bool HasValidSponsorId(string metricName)
    {
        if (!string.IsNullOrWhiteSpace(sponsorId))
            return true;

        WarnMissingSponsorId(metricName);
        return false;
    }

    private void WarnMissingSponsorId(string metricName)
    {
        if (missingSponsorWarningShown)
            return;

        missingSponsorWarningShown = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning(
            $"{name}: No se envio {metricName}; falta configurar sponsorId en CinemaRoomTrigger.",
            this
        );
#endif
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

        // Solo gestionamos el letrero cuando el jugador está en NUESTRA área.
        // Si dos salas comparten el mismo objeto de letrero, la sala en la que
        // NO estás no debe apagarlo (antes se peleaban y no aparecía).
        if (!playerInside)
            return;

        bool playerIsFree =
            movieSelectorController == null ||
            !movieSelectorController.IsCinemaInteractionBusy;

        if (interactionPrompt.activeSelf != playerIsFree)
            interactionPrompt.SetActive(playerIsFree);

        if (interactionButton != null)
            interactionButton.interactable = playerIsFree;
    }

    // Oculta el letrero (al salir del área o al iniciar).
    private void HidePrompt()
    {
        if (interactionPrompt != null && interactionPrompt.activeSelf)
            interactionPrompt.SetActive(false);

        if (interactionButton != null)
            interactionButton.interactable = false;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        SendSalaExitMetric();
        playerRootsInside.Clear();

        if (interactionButton != null)
        {
            interactionButton.onClick.RemoveListener(
                OpenMovieSelectorFromButton
            );
        }
    }
}
