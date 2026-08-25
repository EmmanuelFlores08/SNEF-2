using UnityEngine;
using UnityEngine.UI;

public class AirHockeyInteraction : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AirHockeyGameManager gameManager;

    [Tooltip("Objeto completo del prompt de interacción.")]
    [SerializeField] private GameObject promptRoot;

    [Tooltip("Button del propio prompt. En móvil se toca para iniciar.")]
    [SerializeField] private Button promptButton;

    [Header("Interacción")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private int playerCollidersInside;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // Si no conectamos manualmente el Button,
        // lo buscamos automáticamente en el prompt.
        if (promptButton == null && promptRoot != null)
        {
            promptButton = promptRoot.GetComponent<Button>();

            if (promptButton == null)
            {
                promptButton =
                    promptRoot.GetComponentInChildren<Button>(true);
            }
        }

        if (promptButton != null)
        {
            promptButton.onClick.RemoveListener(StartGameFromButton);
            promptButton.onClick.AddListener(StartGameFromButton);
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        UpdatePrompt();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdatePrompt();

        if (!CanInteract())
            return;

        // PC / teclado
        if (Input.GetKeyDown(interactionKey))
        {
            StartGame();
        }
    }


    // =========================================================
    // BOTÓN MÓVIL
    // =========================================================

    public void StartGameFromButton()
    {
        if (!CanInteract())
            return;

        StartGame();
    }


    // =========================================================
    // INICIAR JUEGO
    // =========================================================

    private void StartGame()
    {
        if (gameManager == null)
            return;

        // Ocultamos inmediatamente el prompt.
        if (promptRoot != null)
            promptRoot.SetActive(false);

        gameManager.StartGame();

        UpdatePrompt();
    }


    // =========================================================
    // VALIDACIÓN
    // =========================================================

    private bool CanInteract()
    {
        if (playerCollidersInside <= 0)
            return false;

        if (gameManager == null)
            return false;

        if (gameManager.IsPlaying)
            return false;

        return true;
    }


    // =========================================================
    // TRIGGERS
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside++;

        UpdatePrompt();
    }


    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerCollidersInside =
            Mathf.Max(0, playerCollidersInside - 1);

        UpdatePrompt();
    }


    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;

        Transform root = other.transform.root;

        return root != null &&
               root.CompareTag("Player");
    }


    // =========================================================
    // PROMPT
    // =========================================================

    private void UpdatePrompt()
    {
        if (promptRoot == null)
            return;

        bool shouldShow =
            playerCollidersInside > 0 &&
            gameManager != null &&
            !gameManager.IsPlaying;

        if (promptRoot.activeSelf != shouldShow)
            promptRoot.SetActive(shouldShow);

        if (promptButton != null)
            promptButton.interactable = shouldShow;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDisable()
    {
        if (promptRoot != null)
            promptRoot.SetActive(false);
    }


    private void OnDestroy()
    {
        if (promptButton != null)
        {
            promptButton.onClick.RemoveListener(
                StartGameFromButton
            );
        }
    }
}