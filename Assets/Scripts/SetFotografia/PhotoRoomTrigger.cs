using UnityEngine;

public class PhotoRoomTrigger : MonoBehaviour
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private PhotoKitSelectorController selectorController;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    private bool playerInside;

    private void Start()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside) return;
        if (Input.GetKeyDown(interactionKey))
        {
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            if (selectorController != null) selectorController.OpenSelector();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        if (interactionPrompt != null) interactionPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }
}