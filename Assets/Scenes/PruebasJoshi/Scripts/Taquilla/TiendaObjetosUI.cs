using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Controller;
using System.Collections;
using System.Collections.Generic;

public class TiendaObjetosUI : MonoBehaviour
{
    [Header("Interfaz completa")]
    [SerializeField] private GameObject uiTaquilla;

    [Header("Texto del botón comprar/usar prenda")]
    [SerializeField] private TextMeshProUGUI textoBotonPrenda;

    [Header("Texto del botón comprar kit (set de grabación)")]
    [SerializeField] private TextMeshProUGUI textoBotonKit;

    [Header("Paneles de contenido")]
    [SerializeField] private GameObject panelObjetosPersonaje;
    [SerializeField] private GameObject panelObjetosSetDeGrabacion;

    [Header("Botones")]
    [SerializeField] private Button buttonPersonaje;
    [SerializeField] private Button buttonSetDeGrabacion;
    [SerializeField] private Button buttonCerrar;
    [SerializeField] private Button buttonComprarPrenda;
    [SerializeField] private Button buttonComprarKit;

    [Header("Animaciones de tabs")]
    [SerializeField] private BotonUIAnimado animacionButtonPersonaje;
    [SerializeField] private BotonUIAnimado animacionButtonSetDeGrabacion;

    [Header("Saldo del jugador")]
    [SerializeField] private TextMeshProUGUI textoSaldo;

    [Header("Aviso invitado")]
    [SerializeField] private GameObject avisoInvitadoPanel;
    [SerializeField] private TextMeshProUGUI avisoInvitadoTexto;
    [SerializeField] private Button avisoInvitadoEntendidoButton;
    [TextArea(2, 4)]
    [SerializeField] private string avisoInvitadoMensaje =
        "Estas navegando como invitado.\nTu progreso y tus compras no se guardaran.\nInicia sesion o crea una cuenta para usar Ditas y conservar tu progreso.";

    [Header("Categorías de tienda")]
    [SerializeField] private ShopCategoryUI[] categoriasTienda;

    [Header("Preview del avatar (prendas)")]
    [SerializeField] private GameObject avatarPreviewRoot;
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private CharacterPreviewRotator previewRotator;

    [Header("Preview de kit (set de grabación)")]
    [SerializeField] private KitPreviewPanel kitPreviewPanel;
    [SerializeField] private PhotoKitCatalog kitCatalog;

    [Header("Animación de preview (bounce al seleccionar prenda)")]
    [Tooltip("RectTransform del RawImage que muestra CharacterPreviewRT.")]
    [SerializeField] private RectTransform previewBounceTarget;
    [SerializeField] private float previewStartScale = 0.82f;
    [SerializeField] private float previewOvershootScale = 1.08f;
    [SerializeField] private float previewNormalScale = 1f;
    [SerializeField] private float previewInDuration = 0.12f;
    [SerializeField] private float previewBounceDuration = 0.16f;

    private Coroutine previewBounceRoutine;

    [Header("Controles táctiles")]
[SerializeField] private GameObject controlesTactiles;

private bool controlesTactilesEstabanActivos;

    [Header("Cursor")]
    [SerializeField] private CursorLockManager cursorLockManager;

    [Header("Configuración")]
    [SerializeField] private bool abrirSiempreEnPersonaje = true;
    [SerializeField] private bool permitirCerrarConEscape = true;

    public bool EstaAbierta { get; private set; }

    private PlayerCharacterCustomized character;
    private MovePlayerInput playerInput;

    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private Dictionary<CustomizationCatalog.BodyPartType, int> originalOutfit
        = new Dictionary<CustomizationCatalog.BodyPartType, int>();

    private ShopCategoryUI selectedCategory;
    private int selectedIndex = -1;

    // Prenda que se está probando en el preview (para dejar solo UNA a la vez).
    private bool prendaEnPreview;
    private CustomizationCatalog.BodyPartType tipoPrendaEnPreview;

    private enum TipoPanel { Personaje, SetDeGrabacion }
    private TipoPanel panelActual = TipoPanel.Personaje;

    private void Awake()
    {
        ObtenerAnimacionesAutomaticamente();

        if (buttonPersonaje != null)
            buttonPersonaje.onClick.AddListener(MostrarPanelPersonaje);

        if (buttonSetDeGrabacion != null)
            buttonSetDeGrabacion.onClick.AddListener(MostrarPanelSetDeGrabacion);

        if (buttonCerrar != null)
            buttonCerrar.onClick.AddListener(CerrarTienda);

        if (buttonComprarPrenda != null)
            buttonComprarPrenda.onClick.AddListener(ComprarPrendaSeleccionada);

        if (buttonComprarKit != null)
            buttonComprarKit.onClick.AddListener(ComprarKitSeleccionado);

        ConfigurarAvisoInvitado();

        if (categoriasTienda != null)
        {
            foreach (var cat in categoriasTienda)
                if (cat != null) cat.OnItemSelected += OnItemSelected;
        }
    }

    private void Start()
    {
        EstaAbierta = false;
        panelActual = TipoPanel.Personaje;

        PrepararPanelInicial();

        if (uiTaquilla != null) uiTaquilla.SetActive(false);
        if (previewCamera != null) previewCamera.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnCoinsChanged += ActualizarSaldo;
            PlayerInventory.Instance.OnInventoryChanged += HandleInventoryChanged;
            ActualizarSaldo(PlayerInventory.Instance.Coins);
        }
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnCoinsChanged -= ActualizarSaldo;
            PlayerInventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void Update()
    {
        if (!EstaAbierta) return;

        if (permitirCerrarConEscape && Input.GetKeyDown(KeyCode.Escape))
            CerrarTienda();
    }

    public void Bind(PlayerCharacterCustomized newCharacter, MovePlayerInput newInput)
    {
        character = newCharacter;
        playerInput = newInput;
    }

    private void OnItemSelected(ShopCategoryUI categoria, int index)
    {
        // Selección exclusiva: al elegir un objeto, se limpia la selección
        // de todas las demás categorías (solo uno seleccionado a la vez).
        if (categoriasTienda != null)
        {
            foreach (var cat in categoriasTienda)
                if (cat != null && cat != categoria)
                    cat.ClearSelection();
        }

        selectedCategory = categoria;
        selectedIndex = index;

        if (categoria.Tipo == ShopCategoryUI.TipoCategoria.Prenda)
        {
            // Solo se previsualiza una prenda a la vez: si la anterior era de
            // otra parte del cuerpo, se revierte antes de probar la nueva.
            if (prendaEnPreview && tipoPrendaEnPreview != categoria.BodyPartType)
                RevertirPrendaEnPreview();

            if (character != null)
                character.SetBodyPart(categoria.BodyPartType, index);

            prendaEnPreview = true;
            tipoPrendaEnPreview = categoria.BodyPartType;

            PlayPreviewBounce();
        }
        else if (categoria.Tipo == ShopCategoryUI.TipoCategoria.Kit)
        {
            // Al pasar a un kit, quita la prenda que se estaba probando.
            RevertirPrendaEnPreview();

            if (kitPreviewPanel != null && kitCatalog != null)
            {
                var kit = kitCatalog.GetKit(index);
                if (kit != null) kitPreviewPanel.ShowKit(kit);
            }
        }

        ActualizarBotonComprar();
    }

    // Regresa la parte del cuerpo que se estaba previsualizando a su valor original.
    private void RevertirPrendaEnPreview()
    {
        if (!prendaEnPreview) return;

        if (character != null &&
            originalOutfit.TryGetValue(tipoPrendaEnPreview, out int originalIndex))
        {
            character.SetBodyPart(tipoPrendaEnPreview, originalIndex);
        }

        prendaEnPreview = false;
    }

    // Reproduce el mismo "bounce" de escala que el preview del SelectorAvatar,
    // esta vez sobre el RawImage que muestra CharacterPreviewRT.
    private void PlayPreviewBounce()
    {
        if (previewBounceTarget == null) return;

        if (previewBounceRoutine != null)
            StopCoroutine(previewBounceRoutine);

        previewBounceRoutine = StartCoroutine(PreviewBounceRoutine());
    }

    private IEnumerator PreviewBounceRoutine()
    {
        previewBounceTarget.localScale = Vector3.one * previewStartScale;

        yield return ScalePreviewRoutine(previewOvershootScale, previewInDuration);
        yield return ScalePreviewRoutine(previewNormalScale, previewBounceDuration);

        previewBounceRoutine = null;
    }

    private IEnumerator ScalePreviewRoutine(float targetScale, float duration)
    {
        Vector3 startScale = previewBounceTarget.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            t = Mathf.SmoothStep(0f, 1f, t);

            previewBounceTarget.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        previewBounceTarget.localScale = endScale;
    }

    private void ActualizarBotonComprar()
    {
        // Por defecto, ambos botones desactivados
        if (buttonComprarPrenda != null) buttonComprarPrenda.interactable = false;
        if (buttonComprarKit != null) buttonComprarKit.interactable = false;

        if (selectedCategory == null || selectedIndex < 0) return;

        selectedCategory.GetItem(selectedIndex, out string id, out _, out int price, out bool gratuito);

        bool owned = gratuito ||
            (PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(id));

        bool canAfford = PlayerInventory.Instance != null &&
            PlayerInventory.Instance.CanAfford(price);

        bool hasServerState = SnefBridge.Instance != null &&
            SnefBridge.Instance.HasServerState;
        bool isGuest = hasServerState && SnefBridge.Instance.IsGuest;
        bool waitingForServerState = IsWaitingForServerState();
        bool purchasePending = SnefBridge.Instance != null &&
            SnefBridge.Instance.IsPurchasePending(id);
        bool comprable = !waitingForServerState &&
            !owned &&
            (canAfford || isGuest) &&
            !purchasePending;

        // Activa solo el botón que corresponde al tipo seleccionado
        if (selectedCategory.Tipo == ShopCategoryUI.TipoCategoria.Prenda)
        {
            // Cambia el texto del botón según si ya lo tiene
            if (textoBotonPrenda != null)
                textoBotonPrenda.text = owned
                    ? "Usar"
                    : waitingForServerState ? "Cargando"
                    : purchasePending ? "Procesando" : "Comprar";

            // Se puede presionar si: puede comprar (no lo tiene y le alcanza), O ya lo tiene (para usar)
            if (buttonComprarPrenda != null)
                buttonComprarPrenda.interactable = comprable || owned;
        }
        else if (selectedCategory.Tipo == ShopCategoryUI.TipoCategoria.Kit)
        {
            // Cambia el texto del botón según si ya lo tiene.
            if (textoBotonKit != null)
                textoBotonKit.text = owned
                    ? "Comprado"
                    : waitingForServerState ? "Cargando"
                    : purchasePending ? "Procesando" : "Comprar";

            if (buttonComprarKit != null)
                buttonComprarKit.interactable = comprable;
        }
    }

    private void ComprarPrendaSeleccionada()
    {
        if (selectedCategory == null || selectedIndex < 0) return;
        if (selectedCategory.Tipo != ShopCategoryUI.TipoCategoria.Prenda) return;

        selectedCategory.GetItem(selectedIndex, out string id, out _, out int price, out bool gratuito);

        bool owned = gratuito ||
            (PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(id));

        if (owned)
        {
            // Ya lo tiene: USAR (dejar la prenda puesta definitivamente)
            bool wasAlreadyEquipped =
                originalOutfit.TryGetValue(
                    selectedCategory.BodyPartType,
                    out int originalIndex
                ) &&
                originalIndex == selectedIndex;

            if (character != null)
            {
                character.SetBodyPart(selectedCategory.BodyPartType, selectedIndex);
                originalOutfit[selectedCategory.BodyPartType] = selectedIndex;
            }

            if (!wasAlreadyEquipped)
            {
                SendPrendaUseIfEquipped(
                    selectedCategory.BodyPartType,
                    selectedIndex,
                    id
                );
            }

            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlaySeleccion();
        }
        else
        {
            // No lo tiene: COMPRAR
            ComprarSeleccionado(ShopCategoryUI.TipoCategoria.Prenda);
        }
    }

    private void ComprarKitSeleccionado()
    {
        ComprarSeleccionado(ShopCategoryUI.TipoCategoria.Kit);
    }

    // Compra genérica, verifica que lo seleccionado sea del tipo esperado
    private void ComprarSeleccionado(ShopCategoryUI.TipoCategoria tipoEsperado)
    {
        if (selectedCategory == null || selectedIndex < 0) return;
        if (selectedCategory.Tipo != tipoEsperado) return; // el botón no coincide con lo seleccionado
        if (PlayerInventory.Instance == null) return;

        selectedCategory.GetItem(selectedIndex, out string id, out _, out int price, out _);

        if (IsWaitingForServerState())
        {
            Debug.LogWarning("[SNEF Compra] Compra bloqueada: esperando SnefEstado.");

            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompraErrada();

            ActualizarBotonComprar();
            return;
        }

        if (SnefBridge.Instance != null && SnefBridge.Instance.HasServerState)
        {
            ComprarSeleccionadoConServidor(tipoEsperado, id);
            return;
        }

        if (PlayerInventory.Instance.TryPurchase(id, price))
        {
            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompra();   // ← sonido de compra
            
            if (selectedCategory.Tipo == ShopCategoryUI.TipoCategoria.Prenda)
            {
                originalOutfit[selectedCategory.BodyPartType] = selectedIndex;
                SendPrendaUseIfEquipped(
                    selectedCategory.BodyPartType,
                    selectedIndex,
                    id
                );
            }

            RefrescarCategorias();
            ActualizarBotonComprar();
        }
        else
        {
            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompraErrada();
        }
    }

    private void ComprarSeleccionadoConServidor(
        ShopCategoryUI.TipoCategoria tipoEsperado,
        string id
    )
    {
        if (SnefBridge.Instance == null || string.IsNullOrWhiteSpace(id))
            return;

        if (SnefBridge.Instance.IsGuest)
        {
            MostrarAvisoInvitado();

            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompraErrada();

            return;
        }

        CustomizationCatalog.BodyPartType bodyPartType =
            selectedCategory.BodyPartType;
        int optionIndex = selectedIndex;

        bool requested = SnefBridge.Instance.RequestPurchase(
            id,
            result => HandleResultadoCompraServidor(
                result,
                tipoEsperado,
                bodyPartType,
                optionIndex,
                id
            )
        );

        if (!requested)
        {
            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompraErrada();
            return;
        }

        ActualizarBotonComprar();
    }

    private void HandleResultadoCompraServidor(
        SnefBridge.CompraResult result,
        ShopCategoryUI.TipoCategoria tipoEsperado,
        CustomizationCatalog.BodyPartType bodyPartType,
        int optionIndex,
        string expectedItemId
    )
    {
        if (result == null)
            return;

        if (!result.Ok)
        {
            Debug.LogWarning(
                $"[SNEF Compra] Compra rechazada para '{expectedItemId}': {result.Motivo}"
            );

            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompraErrada();

            RefrescarCategorias();
            ActualizarBotonComprar();
            return;
        }

        if (!string.IsNullOrEmpty(result.ItemId) &&
            result.ItemId != expectedItemId)
        {
            Debug.LogWarning(
                $"[SNEF Compra] Resultado de compra no coincide. Esperado='{expectedItemId}', recibido='{result.ItemId}'."
            );
            return;
        }

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayCompra();

        if (tipoEsperado == ShopCategoryUI.TipoCategoria.Prenda)
        {
            if (character != null)
            {
                character.SetBodyPart(bodyPartType, optionIndex);
                originalOutfit[bodyPartType] = optionIndex;
            }

            SendPrendaUseIfEquipped(
                bodyPartType,
                optionIndex,
                expectedItemId
            );
        }

        RefrescarCategorias();
        ActualizarBotonComprar();
    }

    private void SendPrendaUseIfEquipped(
        CustomizationCatalog.BodyPartType bodyPartType,
        int optionIndex,
        string optionId
    )
    {
        if (character == null ||
            character.GetCurrentIndex(bodyPartType) != optionIndex ||
            string.IsNullOrWhiteSpace(optionId))
        {
            return;
        }

        SnefMetrics.Send("prenda_use", optionId);
    }

    private void MostrarAvisoInvitado()
    {
        if (avisoInvitadoPanel != null)
        {
            avisoInvitadoPanel.SetActive(true);
            return;
        }

        Debug.LogWarning("[SNEF Invitado] " + avisoInvitadoMensaje);
    }

    public void CerrarAvisoInvitado()
    {
        if (avisoInvitadoPanel != null)
            avisoInvitadoPanel.SetActive(false);
    }

    private void ConfigurarAvisoInvitado()
    {
        CerrarAvisoInvitado();

        if (avisoInvitadoTexto != null &&
            string.IsNullOrWhiteSpace(avisoInvitadoTexto.text) &&
            !string.IsNullOrWhiteSpace(avisoInvitadoMensaje))
        {
            avisoInvitadoTexto.text = avisoInvitadoMensaje;
        }

        if (avisoInvitadoEntendidoButton == null)
            return;

        avisoInvitadoEntendidoButton.onClick.RemoveListener(CerrarAvisoInvitado);
        avisoInvitadoEntendidoButton.onClick.AddListener(CerrarAvisoInvitado);
    }

    private bool IsWaitingForServerState()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return SnefBridge.Instance == null || !SnefBridge.Instance.HasServerState;
#else
        return false;
#endif
    }

    public void AbrirTienda()
{
    if (EstaAbierta) return;
    if (uiTaquilla == null) return;

    EstaAbierta = true;

    // ==========================================
    // OCULTAR CONTROLES TÁCTILES
    // ==========================================
    if (controlesTactiles != null)
    {
        // Guardamos cómo estaban antes de abrir la interfaz.
        controlesTactilesEstabanActivos = controlesTactiles.activeSelf;

        controlesTactiles.SetActive(false);
    }

    uiTaquilla.SetActive(true);
    SnefMetrics.Send("taquilla_enter", "app");

    if (character != null)
    {
        CharacterMover mover = character.GetComponent<CharacterMover>();

        if (mover != null)
        {
            mover.ResetToIdle();
            mover.enabled = false;
        }
    }

    if (playerInput != null)
        playerInput.enabled = false;

    GuardarOutfitActual();

    if (character != null && previewAnchor != null)
    {
        savedPosition = character.transform.position;
        savedRotation = character.transform.rotation;

        CharacterController cc =
            character.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        character.transform.SetPositionAndRotation(
            previewAnchor.position,
            previewAnchor.rotation
        );

        if (cc != null)
            cc.enabled = true;
    }

    if (previewRotator != null && character != null)
        previewRotator.SetTarget(character.transform);

    if (previewCamera != null)
        previewCamera.gameObject.SetActive(true);

    if (PlayerInventory.Instance != null)
        ActualizarSaldo(PlayerInventory.Instance.Coins);

    RefrescarCategorias();
    LimpiarSeleccion();

    if (abrirSiempreEnPersonaje)
        MostrarPanelPersonaje();
    else
        MostrarPanelActual();

    MostrarCursorInterfaz(true);
}
   public void CerrarTienda()
{
    if (!EstaAbierta) return;

    EstaAbierta = false;

    RestaurarOutfitOriginal();

    if (uiTaquilla != null)
        uiTaquilla.SetActive(false);

    if (previewCamera != null)
        previewCamera.gameObject.SetActive(false);

    if (previewRotator != null)
        previewRotator.SetTarget(null);

    if (character != null && previewAnchor != null)
    {
        CharacterController cc =
            character.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        character.transform.SetPositionAndRotation(
            savedPosition,
            savedRotation
        );

        if (cc != null)
            cc.enabled = true;
    }

    if (character != null)
    {
        CharacterMover mover =
            character.GetComponent<CharacterMover>();

        if (mover != null)
            mover.enabled = true;
    }

    if (playerInput != null)
        playerInput.enabled = true;

    MostrarCursorInterfaz(false);

    // ==========================================
    // RESTAURAR CONTROLES TÁCTILES
    // ==========================================
    if (controlesTactiles != null)
    {
        controlesTactiles.SetActive(
            controlesTactilesEstabanActivos
        );
    }

    if (UISoundManager.Instance != null)
        UISoundManager.Instance.PlayCerrarMenu();
}

    private void GuardarOutfitActual()
    {
        if (character == null) return;

        originalOutfit.Clear();

        foreach (CustomizationCatalog.BodyPartType type in
                 System.Enum.GetValues(typeof(CustomizationCatalog.BodyPartType)))
        {
            originalOutfit[type] = character.GetCurrentIndex(type);
        }
    }

    private void RestaurarOutfitOriginal()
    {
        if (character == null) return;

        foreach (var kvp in originalOutfit)
            character.SetBodyPart(kvp.Key, kvp.Value);
    }

    // Muestra u oculta el cursor para la interfaz. Usa el CursorLockManager si está
    // asignado; si no, controla el cursor directamente (igual que el selector de kit
    // y las salas de cine), para que funcione aunque no haya CursorLockManager.
    private void MostrarCursorInterfaz(bool mostrar)
    {
        if (cursorLockManager != null)
        {
            cursorLockManager.SetInterfaceMode(mostrar);
            return;
        }

        Cursor.visible = mostrar;
        Cursor.lockState = mostrar ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void ActualizarSaldo(int monedas)
    {
        if (textoSaldo != null) textoSaldo.text = monedas.ToString();
    }

    private void HandleInventoryChanged()
    {
        RefrescarCategorias();
        ActualizarBotonComprar();
    }

    private void RefrescarCategorias()
    {
        if (categoriasTienda == null) return;

        foreach (var cat in categoriasTienda)
            if (cat != null) cat.Refresh();
    }

    private void LimpiarSeleccion()
    {
        // Quita la prenda que se estaba probando (deja el avatar como estaba).
        RevertirPrendaEnPreview();

        selectedCategory = null;
        selectedIndex = -1;

        if (categoriasTienda != null)
        {
            foreach (var cat in categoriasTienda)
                if (cat != null) cat.ClearSelection();
        }

        if (kitPreviewPanel != null) kitPreviewPanel.Clear();

        ActualizarBotonComprar();
    }

    private void ObtenerAnimacionesAutomaticamente()
    {
        if (animacionButtonPersonaje == null && buttonPersonaje != null)
            animacionButtonPersonaje = buttonPersonaje.GetComponent<BotonUIAnimado>();

        if (animacionButtonSetDeGrabacion == null && buttonSetDeGrabacion != null)
            animacionButtonSetDeGrabacion = buttonSetDeGrabacion.GetComponent<BotonUIAnimado>();
    }

    private void PrepararPanelInicial()
    {
        if (panelObjetosPersonaje != null) panelObjetosPersonaje.SetActive(true);
        if (panelObjetosSetDeGrabacion != null) panelObjetosSetDeGrabacion.SetActive(false);
        ActualizarEstadoVisualTabs();
    }

    public void MostrarPanelPersonaje()
    {
        panelActual = TipoPanel.Personaje;

        if (panelObjetosPersonaje != null) panelObjetosPersonaje.SetActive(true);
        if (panelObjetosSetDeGrabacion != null) panelObjetosSetDeGrabacion.SetActive(false);

        if (avatarPreviewRoot != null) avatarPreviewRoot.SetActive(true);
        if (kitPreviewPanel != null) kitPreviewPanel.gameObject.SetActive(false);
        
        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayAbrirMenu();
        LimpiarSeleccion();
        ActualizarEstadoVisualTabs();
    }

    public void MostrarPanelSetDeGrabacion()
    {
        panelActual = TipoPanel.SetDeGrabacion;

        if (panelObjetosPersonaje != null) panelObjetosPersonaje.SetActive(false);
        if (panelObjetosSetDeGrabacion != null) panelObjetosSetDeGrabacion.SetActive(true);

        if (avatarPreviewRoot != null) avatarPreviewRoot.SetActive(false);
        if (kitPreviewPanel != null)
        {
            kitPreviewPanel.gameObject.SetActive(true);
            kitPreviewPanel.Clear();
        }

        if (UISoundManager.Instance != null)
            UISoundManager.Instance.PlayAbrirMenu();

        LimpiarSeleccion();
        ActualizarEstadoVisualTabs();
    }

    private void ActualizarEstadoVisualTabs()
    {
        bool personajeActivo = panelActual == TipoPanel.Personaje;

        if (animacionButtonPersonaje != null)
            animacionButtonPersonaje.SetSeleccionado(personajeActivo);

        if (animacionButtonSetDeGrabacion != null)
            animacionButtonSetDeGrabacion.SetSeleccionado(!personajeActivo);
    }

    private void MostrarPanelActual()
    {
        switch (panelActual)
        {
            case TipoPanel.Personaje: MostrarPanelPersonaje(); break;
            case TipoPanel.SetDeGrabacion: MostrarPanelSetDeGrabacion(); break;
        }
    }

    private void OnDestroy()
    {
        if (buttonPersonaje != null) buttonPersonaje.onClick.RemoveListener(MostrarPanelPersonaje);
        if (buttonSetDeGrabacion != null) buttonSetDeGrabacion.onClick.RemoveListener(MostrarPanelSetDeGrabacion);
        if (buttonCerrar != null) buttonCerrar.onClick.RemoveListener(CerrarTienda);
        if (buttonComprarPrenda != null) buttonComprarPrenda.onClick.RemoveListener(ComprarPrendaSeleccionada);
        if (buttonComprarKit != null) buttonComprarKit.onClick.RemoveListener(ComprarKitSeleccionado);
        if (avisoInvitadoEntendidoButton != null) avisoInvitadoEntendidoButton.onClick.RemoveListener(CerrarAvisoInvitado);
    }
}
