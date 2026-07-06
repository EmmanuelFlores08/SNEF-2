using Controller;
using UnityEngine;
using UnityEngine.UI;

public class PhotoKitSelectorController : MonoBehaviour
{
    [Header("Panel principal")]
    [SerializeField] private GameObject selectorPanel;

    [Header("Catálogo y set")]
    [SerializeField] private PhotoKitCatalog catalog;
    [SerializeField] private PhotoSetManager photoSetManager;

    [Header("Cards de kit")]
    [SerializeField] private PhotoKitCardUI[] kitCards;

    [Header("Botón usar kit")]
    [SerializeField] private Button useKitButton;

    [Header("Botón salir del set")]
    [SerializeField] private GameObject exitButton;

    [Header("Botón cambiar kit")]
    [SerializeField] private GameObject changeKitButton;

    [Header("Posición del avatar en el set")]
    [SerializeField] private Transform avatarPhotoAnchor; // dónde se para el avatar al elegir kit

    [Header("Cursor / Cámara")]
    [SerializeField] private CursorLockManager cursorLockManager;
    [SerializeField] private RoomCameraManager roomCameraManager;

    private MonoBehaviour boundPlayerInput;
    private CharacterMover boundMover;
    private Transform boundCharacter;

    private int selectedKitIndex = -1;
    private bool isSelectorOpen;
    private bool isInSet;

    // Para devolver al avatar a donde estaba antes de entrar al set
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private bool hasSavedPosition;

    private void Start()
    {
        for (int i = 0; i < kitCards.Length; i++)
        {
            if (kitCards[i] != null) kitCards[i].Init(this, i);
        }

        if (useKitButton != null)
            useKitButton.onClick.AddListener(UseSelectedKit);

        if (selectorPanel != null) selectorPanel.SetActive(false);
        if (exitButton != null) exitButton.SetActive(false);
        if (changeKitButton != null) changeKitButton.SetActive(false);
    }

    public void BindPlayerInput(MonoBehaviour playerInput)
    {
        boundPlayerInput = playerInput;
        if (playerInput != null)
        {
            boundMover = playerInput.GetComponent<CharacterMover>();
            boundCharacter = playerInput.transform;
        }
    }

    public void OpenSelector()
    {
        isSelectorOpen = true;
        if (selectorPanel != null) selectorPanel.SetActive(true);
        if (exitButton != null) exitButton.SetActive(false);
        if (changeKitButton != null) changeKitButton.SetActive(false);

        SetPlayerControlsEnabled(false);
        ShowCursor(true);
    }

    public void CloseSelector()
    {
        isSelectorOpen = false;
        if (selectorPanel != null) selectorPanel.SetActive(false);

        if (!isInSet)
        {
            SetPlayerControlsEnabled(true);
            ShowCursor(false);
        }
    }

    // Llamado por cada card al hacer clic
    public void SelectKit(int index)
    {
        selectedKitIndex = index;
        for (int i = 0; i < kitCards.Length; i++)
        {
            if (kitCards[i] != null) kitCards[i].SetSelected(i == index);
        }
    }

    private void UseSelectedKit()
    {
        if (selectedKitIndex < 0) return;

        var kit = catalog.GetKit(selectedKitIndex);
        if (kit == null) return;

        if (photoSetManager != null)
            photoSetManager.ApplyKit(kit);

        // Guarda la posición previa del avatar la primera vez que entra al set
        if (!hasSavedPosition && boundCharacter != null)
        {
            savedPosition = boundCharacter.position;
            savedRotation = boundCharacter.rotation;
            hasSavedPosition = true;
        }

        // Frena el movimiento residual
        if (boundMover != null) boundMover.ResetToIdle();

        // Coloca al avatar en la posición del set
        if (boundCharacter != null && avatarPhotoAnchor != null)
        {
            CharacterController cc = boundCharacter.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            boundCharacter.SetPositionAndRotation(avatarPhotoAnchor.position, avatarPhotoAnchor.rotation);

            if (cc != null) cc.enabled = true;
        }

        // Entra al set: cámara fija, sin control, cursor visible para los botones
        isInSet = true;
        isSelectorOpen = false;

        if (selectorPanel != null) selectorPanel.SetActive(false);

        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        if (roomCameraManager != null)
            roomCameraManager.ActivateZoneCamera();

        if (exitButton != null) exitButton.SetActive(true);
        if (changeKitButton != null) changeKitButton.SetActive(true);
    }

    // Botón "cambiar kit": reabre el menú sin salir del set
    public void ChangeKit()
    {
        isSelectorOpen = true;

        if (selectorPanel != null) selectorPanel.SetActive(true);

        // Oculta los botones del modo "viendo" mientras eliges de nuevo
        if (exitButton != null) exitButton.SetActive(false);
        if (changeKitButton != null) changeKitButton.SetActive(false);

        // El cursor sigue visible y el control bloqueado; no hace falta tocarlos
    }

    // Botón "salir": limpia el kit, vuelve el avatar a su sitio y devuelve el control
    public void ExitSet()
    {
        isInSet = false;

        if (photoSetManager != null)
            photoSetManager.ClearCurrentKit();

        // Devuelve al avatar a la posición que tenía antes de entrar al set
        if (hasSavedPosition && boundCharacter != null)
        {
            CharacterController cc = boundCharacter.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            boundCharacter.SetPositionAndRotation(savedPosition, savedRotation);

            if (cc != null) cc.enabled = true;

            hasSavedPosition = false;
        }

        if (roomCameraManager != null)
            roomCameraManager.ActivateFollowCamera();

        if (exitButton != null) exitButton.SetActive(false);
        if (changeKitButton != null) changeKitButton.SetActive(false);

        SetPlayerControlsEnabled(true);
        ShowCursor(false);
    }

    private void ShowCursor(bool show)
    {
        if (cursorLockManager != null)
            cursorLockManager.SetInterfaceMode(show);
        else
        {
            Cursor.visible = show;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (boundPlayerInput != null)
            boundPlayerInput.enabled = enabled;
    }
}