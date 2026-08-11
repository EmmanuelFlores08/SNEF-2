using UnityEngine;
using UnityEngine.UI;

public class PhotoRoomTrigger : MonoBehaviour
{
    [Header("Interacción")]
    [SerializeField] private GameObject interactionPrompt;

    [Tooltip("Botón del mismo prompt. En móvil se toca para abrir.")]
    [SerializeField] private Button interactionButton;

    [SerializeField] private PhotoKitSelectorController selectorController;

    [Header("Configuración")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;

    private void Awake()
    {
        // Si no se conecta manualmente,
        // intenta obtener el Button del propio prompt.
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
            interactionButton.onClick.AddListener(AbrirDesdePrompt);
    }

    private void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside)
            return;

        // PC / teclado
        if (Input.GetKeyDown(interactionKey))
            AbrirSelector();
    }

    /// <summary>
    /// Se ejecuta al tocar el prompt en móvil/tablet.
    /// </summary>
    public void AbrirDesdePrompt()
    {
        if (!playerInside)
            return;

        AbrirSelector();
    }

    private void AbrirSelector()
    {
        if (selectorController == null)
            return;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        selectorController.OpenSelector();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other))
            return;

        playerInside = true;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other))
            return;

        playerInside = false;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private bool EsJugador(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Transform raiz = other.transform.root;

        return raiz != null &&
               raiz.CompareTag(playerTag);
    }

    private void OnDestroy()
    {
        if (interactionButton != null)
            interactionButton.onClick.RemoveListener(AbrirDesdePrompt);
    }
}