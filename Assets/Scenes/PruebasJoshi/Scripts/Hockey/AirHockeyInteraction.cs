using UnityEngine;

public class AirHockeyInteraction : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AirHockeyGameManager gameManager;
    [SerializeField] private GameObject promptRoot;

    [Header("Interacción")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private int playerCollidersInside;

    private void Start()
    {
        UpdatePrompt();
    }

    private void Update()
    {
        UpdatePrompt();

        if (playerCollidersInside <= 0)
            return;

        if (gameManager == null || gameManager.IsPlaying)
            return;

        if (Input.GetKeyDown(interactionKey))
            gameManager.StartGame();
    }

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
        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player");
    }

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
    }

    private void OnDisable()
    {
        if (promptRoot != null)
            promptRoot.SetActive(false);
    }
}