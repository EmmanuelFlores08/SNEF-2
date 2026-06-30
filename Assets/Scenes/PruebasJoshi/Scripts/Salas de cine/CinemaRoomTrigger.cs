using UnityEngine;

public class CinemaRoomTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private MovieSelectorController movieSelectorController;

    [Header("Input")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private bool playerInside;

    private void Start()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside)
            return;

        if (Input.GetKeyDown(interactionKey))
        {
            OpenMovieSelector();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void OpenMovieSelector()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        if (movieSelectorController != null)
            movieSelectorController.OpenSelector();
    }
}