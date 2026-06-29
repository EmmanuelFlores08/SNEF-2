using UnityEngine;
using Controller;

public class CustomizationPanelController : MonoBehaviour
{
    [Header("Panel de personalización")]
    [SerializeField] private GameObject customizationPanel;
    [SerializeField] private CharacterCustomizationUI customizationUI;

    [Header("Cursor")]
    [SerializeField] private CursorLockManager cursorLockManager;

    [Header("Preview")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private CharacterPreviewRotator previewRotator;

    [Header("Tecla")]
    [SerializeField] private KeyCode toggleKey = KeyCode.E;

    private PlayerCharacterCustomized character;
    private MovePlayerInput playerInput;

    private bool isOpen = false;
    private bool ready = false;

    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private void Start()
    {
        if (customizationPanel != null) customizationPanel.SetActive(false);
        if (previewCamera != null) previewCamera.gameObject.SetActive(false);
    }

    public void Bind(PlayerCharacterCustomized newCharacter, MovePlayerInput newInput)
    {
        character = newCharacter;
        playerInput = newInput;

        if (customizationUI != null && character != null)
            customizationUI.SetCharacter(character);

        ready = true;
    }

    private void Update()
    {
        if (!ready) return;

        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen) ClosePanel();
            else OpenPanel();
        }
    }

    public void OpenPanel()
    {
        isOpen = true;
        if (customizationPanel != null) customizationPanel.SetActive(true);

        // Deja de leer input de juego
        if (playerInput != null) playerInput.enabled = false;

        // Fuerza idle y detiene el mover (también corta el LookAt IK hacia la cámara)
        if (character != null)
        {
            CharacterMover mover = character.GetComponent<CharacterMover>();
            if (mover != null)
            {
                mover.ResetToIdle();
                mover.enabled = false;
            }
        }

        // Teletransporta al personaje a la estación de preview (guardando su sitio en el mundo)
        if (character != null && previewAnchor != null)
        {
            savedPosition = character.transform.position;
            savedRotation = character.transform.rotation;

            CharacterController cc = character.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            character.transform.SetPositionAndRotation(previewAnchor.position, previewAnchor.rotation);

            if (cc != null) cc.enabled = true;
        }

        // Conecta el rotator al personaje que ahora está en preview
        if (previewRotator != null && character != null)
            previewRotator.SetTarget(character.transform);

        if (previewCamera != null) previewCamera.gameObject.SetActive(true);

        // Cede el cursor al panel
        if (cursorLockManager != null) cursorLockManager.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ClosePanel()
    {
        isOpen = false;
        if (customizationPanel != null) customizationPanel.SetActive(false);

        if (previewCamera != null) previewCamera.gameObject.SetActive(false);

        if (previewRotator != null) previewRotator.SetTarget(null);

        // Regresa al personaje a su sitio en el mundo
        if (character != null && previewAnchor != null)
        {
            CharacterController cc = character.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            character.transform.SetPositionAndRotation(savedPosition, savedRotation);

            if (cc != null) cc.enabled = true;
        }

        // Reactiva movimiento y animación
        if (character != null)
        {
            CharacterMover mover = character.GetComponent<CharacterMover>();
            if (mover != null) mover.enabled = true;
        }

        if (playerInput != null) playerInput.enabled = true;

        // Devuelve el control del cursor a tu manejador
        if (cursorLockManager != null) cursorLockManager.enabled = true;
    }
}