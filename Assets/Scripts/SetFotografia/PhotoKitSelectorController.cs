using Controller;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("Botones del set")]
    [SerializeField] private GameObject exitButton;
    [SerializeField] private GameObject changeKitButton;
    [SerializeField] private GameObject takePhotoButton;

    [Header("Panel de fotografía")]
    [Tooltip("Objeto raíz del menú de previsualización: fondoFotografia.")]
    [SerializeField] private GameObject photoPanel;

    [Header("Posición del avatar en el set")]
    [SerializeField] private Transform avatarPhotoAnchor;

    [Header("Controles táctiles")]
[SerializeField] private GameObject controlesTactiles;

private bool controlesTactilesEstabanActivos;
private bool estadoControlesTactilesGuardado;

    [Header("Cursor / Cámara")]
    [SerializeField] private CursorLockManager cursorLockManager;
    [SerializeField] private RoomCameraManager roomCameraManager;

    private MonoBehaviour boundPlayerInput;
    private CharacterMover boundMover;
    private Transform boundCharacter;

    // Relaciona cada card visible con el índice real del kit en el catálogo.
    private readonly List<int> ownedKitIndices = new List<int>();

    private int selectedKitIndex = -1;
    private bool isSelectorOpen;
    private bool isInSet;

    // Posición del avatar antes de entrar al set.
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private bool hasSavedPosition;

    public bool IsInSet => isInSet;
    public bool IsSelectorOpen => isSelectorOpen;

    private void Start()
    {
        if (useKitButton != null)
            useKitButton.onClick.AddListener(UseSelectedKit);

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (photoPanel != null)
            photoPanel.SetActive(false);

        SetSetButtonsVisible(false);
    }

    public void BindPlayerInput(MonoBehaviour playerInput)
    {
        boundPlayerInput = playerInput;

        if (playerInput == null)
        {
            boundMover = null;
            boundCharacter = null;
            return;
        }

        boundMover = playerInput.GetComponent<CharacterMover>();
        boundCharacter = playerInput.transform;
    }

public void OpenSelector()
{
    OcultarControlesTactiles();

    isSelectorOpen = true;

    if (selectorPanel != null)
        selectorPanel.SetActive(true);

    if (photoPanel != null)
        photoPanel.SetActive(false);

    SetSetButtonsVisible(false);

    RefreshKitCards();

    SetPlayerControlsEnabled(false);
    ShowCursor(true);

    if (UISoundManager.Instance != null)
        UISoundManager.Instance.PlayAbrirMenu();
}

public void CloseSelector()
{
    isSelectorOpen = false;

    if (selectorPanel != null)
        selectorPanel.SetActive(false);

    if (isInSet)
    {
        // Seguimos dentro del set.
        // NO restauramos joystick ni controles táctiles.

        SetSetButtonsVisible(true);
        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        OcultarControlesTactiles();
    }
    else
    {
        // Cerramos completamente la interfaz
        // y regresamos al juego normal.

        SetSetButtonsVisible(false);
        SetPlayerControlsEnabled(true);
        ShowCursor(false);

        RestaurarControlesTactiles();
    }

    if (UISoundManager.Instance != null)
        UISoundManager.Instance.PlayCerrarMenu();
}

    private void RefreshKitCards()
    {
        ownedKitIndices.Clear();

        int total = catalog != null && catalog.kits != null
            ? catalog.kits.Length
            : 0;

        for (int i = 0; i < total; i++)
        {
            PhotoKitCatalog.PhotoKit kit = catalog.GetKit(i);

            if (kit == null)
                continue;

            bool owned =
                kit.gratuito ||
                (PlayerInventory.Instance != null &&
                 PlayerInventory.Instance.IsOwned(kit.kitId));

            if (owned)
                ownedKitIndices.Add(i);
        }

        if (kitCards == null)
        {
            selectedKitIndex = -1;
            return;
        }

        for (int cardIndex = 0; cardIndex < kitCards.Length; cardIndex++)
        {
            PhotoKitCardUI card = kitCards[cardIndex];

            if (card == null)
                continue;

            if (cardIndex < ownedKitIndices.Count)
            {
                int catalogIndex = ownedKitIndices[cardIndex];
                PhotoKitCatalog.PhotoKit kit = catalog.GetKit(catalogIndex);

                if (kit == null)
                {
                    card.Hide();
                    continue;
                }

                card.Init(this, catalogIndex);
                card.Setup(catalogIndex, kit.previewSprite);
            }
            else
            {
                card.Hide();
            }
        }

        selectedKitIndex = -1;
    }

    public void SelectKit(int catalogIndex)
    {
        if (catalog == null || catalog.GetKit(catalogIndex) == null)
            return;

        selectedKitIndex = catalogIndex;

        if (kitCards != null)
        {
            for (int cardIndex = 0; cardIndex < kitCards.Length; cardIndex++)
            {
                PhotoKitCardUI card = kitCards[cardIndex];

                if (card == null)
                    continue;

                bool isSelectedCard =
                    cardIndex < ownedKitIndices.Count &&
                    ownedKitIndices[cardIndex] == catalogIndex;

                card.SetSelected(isSelectedCard);
            }
        }

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlaySeleccion();
    }

    private void UseSelectedKit()
    {
        if (selectedKitIndex < 0 || catalog == null)
            return;

        PhotoKitCatalog.PhotoKit kit = catalog.GetKit(selectedKitIndex);

        if (kit == null)
            return;

        if (photoSetManager != null)
            photoSetManager.ApplyKit(kit);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayUsarKit();

        // Guarda la posición previa solamente la primera vez
        // que el personaje entra al set.
        if (!hasSavedPosition && boundCharacter != null)
        {
            savedPosition = boundCharacter.position;
            savedRotation = boundCharacter.rotation;
            hasSavedPosition = true;
        }

        // Detiene cualquier movimiento residual.
        if (boundMover != null)
            boundMover.ResetToIdle();

        MoveCharacterToPhotoAnchor();
        
OcultarControlesTactiles();

        isInSet = true;
        isSelectorOpen = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (photoPanel != null)
            photoPanel.SetActive(false);

        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        if (roomCameraManager != null)
            roomCameraManager.ActivateZoneCamera();

        SetSetButtonsVisible(true);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayMusicaSet();
    }

    public void ChangeKit()
    {
        if (!isInSet)
            return;
         OcultarControlesTactiles();
        isSelectorOpen = true;

        if (selectorPanel != null)
            selectorPanel.SetActive(true);

        if (photoPanel != null)
            photoPanel.SetActive(false);

        RefreshKitCards();

        // Mientras el usuario elige otro kit,
        // oculta las acciones principales del set.
        SetSetButtonsVisible(false);

        SetPlayerControlsEnabled(false);
        ShowCursor(true);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayAbrirMenu();
    }

    public void ExitSet()
    {
        isInSet = false;
        isSelectorOpen = false;

        if (selectorPanel != null)
            selectorPanel.SetActive(false);

        if (photoPanel != null)
            photoPanel.SetActive(false);

        SetSetButtonsVisible(false);

        if (photoSetManager != null)
            photoSetManager.ClearCurrentKit();

        RestoreCharacterPosition();

        if (roomCameraManager != null)
            roomCameraManager.ActivateFollowCamera();

        if (UISoundManager.Instance != null)
        {
            UISoundManager.Instance.PlayCerrarMenu();
            UISoundManager.Instance.PlayMusicaGeneral();
        }

        SetPlayerControlsEnabled(true);
        ShowCursor(false);
        RestaurarControlesTactiles();
    }

    private void MoveCharacterToPhotoAnchor()
    {
        if (boundCharacter == null || avatarPhotoAnchor == null)
            return;

        CharacterController characterController =
            boundCharacter.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        boundCharacter.SetPositionAndRotation(
            avatarPhotoAnchor.position,
            avatarPhotoAnchor.rotation
        );

        if (characterController != null)
            characterController.enabled = true;
    }

    private void RestoreCharacterPosition()
    {
        if (!hasSavedPosition || boundCharacter == null)
            return;

        CharacterController characterController =
            boundCharacter.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        boundCharacter.SetPositionAndRotation(
            savedPosition,
            savedRotation
        );

        if (characterController != null)
            characterController.enabled = true;

        hasSavedPosition = false;
    }

    /// <summary>
    /// Muestra u oculta los tres botones visibles mientras
    /// el jugador está usando el set de grabación.
    ///
    /// El futuro PhotoCaptureController puede utilizar este método
    /// para ocultarlos antes de capturar la fotografía.
    /// </summary>
    public void SetSetButtonsVisible(bool visible)
    {
        if (exitButton != null)
            exitButton.SetActive(visible);

        if (changeKitButton != null)
            changeKitButton.SetActive(visible);

        if (takePhotoButton != null)
            takePhotoButton.SetActive(visible);
    }

    /// <summary>
    /// Permite cerrar el panel de fotografía desde su botón X.
    /// Después vuelve a mostrar las acciones del set.
    /// </summary>
    public void ClosePhotoPanel()
    {
        if (photoPanel != null)
            photoPanel.SetActive(false);

        if (isInSet && !isSelectorOpen)
            SetSetButtonsVisible(true);

        ShowCursor(isInSet);

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayCerrarMenu();
    }

    private void ShowCursor(bool show)
    {
        if (cursorLockManager != null)
        {
            cursorLockManager.SetInterfaceMode(show);
            return;
        }

        Cursor.visible = show;
        Cursor.lockState = show
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (boundPlayerInput != null)
            boundPlayerInput.enabled = enabled;
    }

    private void OnDestroy()
    {
        if (useKitButton != null)
            useKitButton.onClick.RemoveListener(UseSelectedKit);
    }

    private void OcultarControlesTactiles()
{
    if (controlesTactiles == null)
        return;

    // Guardamos el estado solamente la primera vez.
    // Así no lo sobrescribimos cuando cambiamos de kit,
    // abrimos la foto, etc.
    if (!estadoControlesTactilesGuardado)
    {
        controlesTactilesEstabanActivos =
            controlesTactiles.activeSelf;

        estadoControlesTactilesGuardado = true;
    }

    controlesTactiles.SetActive(false);
}

private void RestaurarControlesTactiles()
{
    if (controlesTactiles == null)
        return;

    if (!estadoControlesTactilesGuardado)
        return;

    controlesTactiles.SetActive(
        controlesTactilesEstabanActivos
    );

    estadoControlesTactilesGuardado = false;
}
}