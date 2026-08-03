using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Controller;
using System.Collections.Generic;

public class TiendaObjetosUI : MonoBehaviour
{
    [Header("Interfaz completa")]
    [SerializeField] private GameObject uiTaquilla;

    [Header("Texto del botón comprar/usar prenda")]
    [SerializeField] private TextMeshProUGUI textoBotonPrenda;  

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
            ActualizarSaldo(PlayerInventory.Instance.Coins);
        }
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnCoinsChanged -= ActualizarSaldo;
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
        selectedCategory = categoria;
        selectedIndex = index;

        if (categoria.Tipo == ShopCategoryUI.TipoCategoria.Prenda && character != null)
        {
            character.SetBodyPart(categoria.BodyPartType, index);
        }
        else if (categoria.Tipo == ShopCategoryUI.TipoCategoria.Kit)
        {
            if (kitPreviewPanel != null && kitCatalog != null)
            {
                var kit = kitCatalog.GetKit(index);
                if (kit != null) kitPreviewPanel.ShowKit(kit);
            }
        }

        ActualizarBotonComprar();
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

        bool comprable = !owned && canAfford;

        // Activa solo el botón que corresponde al tipo seleccionado
        if (selectedCategory.Tipo == ShopCategoryUI.TipoCategoria.Prenda)
        {
            // Cambia el texto del botón según si ya lo tiene
            if (textoBotonPrenda != null)
                textoBotonPrenda.text = owned ? "Usar" : "Comprar";

            // Se puede presionar si: puede comprar (no lo tiene y le alcanza), O ya lo tiene (para usar)
            if (buttonComprarPrenda != null)
                buttonComprarPrenda.interactable = comprable || owned;
        }
        else if (selectedCategory.Tipo == ShopCategoryUI.TipoCategoria.Kit)
        {
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
            if (character != null)
            {
                character.SetBodyPart(selectedCategory.BodyPartType, selectedIndex);
                originalOutfit[selectedCategory.BodyPartType] = selectedIndex;
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

        if (PlayerInventory.Instance.TryPurchase(id, price))
        {
            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompra();   // ← sonido de compra
            
            if (selectedCategory.Tipo == ShopCategoryUI.TipoCategoria.Prenda)
                originalOutfit[selectedCategory.BodyPartType] = selectedIndex;

            RefrescarCategorias();
            ActualizarBotonComprar();
        }
        else
        {
            if (UISoundManager.Instance != null)
                UISoundManager.Instance.PlayCompraErrada();
        }
    }

    public void AbrirTienda()
    {
        if (EstaAbierta) return;
        if (uiTaquilla == null) return;

        EstaAbierta = true;
        uiTaquilla.SetActive(true);

        if (character != null)
        {
            CharacterMover mover = character.GetComponent<CharacterMover>();
            if (mover != null)
            {
                mover.ResetToIdle();
                mover.enabled = false;
            }
        }
        if (playerInput != null) playerInput.enabled = false;

        GuardarOutfitActual();

        if (character != null && previewAnchor != null)
        {
            savedPosition = character.transform.position;
            savedRotation = character.transform.rotation;

            CharacterController cc = character.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            character.transform.SetPositionAndRotation(previewAnchor.position, previewAnchor.rotation);

            if (cc != null) cc.enabled = true;
        }

        if (previewRotator != null && character != null)
            previewRotator.SetTarget(character.transform);

        if (previewCamera != null) previewCamera.gameObject.SetActive(true);

        if (PlayerInventory.Instance != null)
            ActualizarSaldo(PlayerInventory.Instance.Coins);

        RefrescarCategorias();
        LimpiarSeleccion();

        if (abrirSiempreEnPersonaje) MostrarPanelPersonaje();
        else MostrarPanelActual();

        if (cursorLockManager != null) cursorLockManager.SetInterfaceMode(true);
    }

    public void CerrarTienda()
    {
        if (!EstaAbierta) return;
        
        EstaAbierta = false;

        RestaurarOutfitOriginal();

        if (uiTaquilla != null) uiTaquilla.SetActive(false);
        if (previewCamera != null) previewCamera.gameObject.SetActive(false);
        if (previewRotator != null) previewRotator.SetTarget(null);

        if (character != null && previewAnchor != null)
        {
            CharacterController cc = character.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            character.transform.SetPositionAndRotation(savedPosition, savedRotation);

            if (cc != null) cc.enabled = true;
        }

        if (character != null)
        {
            CharacterMover mover = character.GetComponent<CharacterMover>();
            if (mover != null) mover.enabled = true;
        }
        if (playerInput != null) playerInput.enabled = true;

        if (cursorLockManager != null) cursorLockManager.SetInterfaceMode(false);
        
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

    private void ActualizarSaldo(int monedas)
    {
        if (textoSaldo != null) textoSaldo.text = monedas.ToString();
    }

    private void RefrescarCategorias()
    {
        if (categoriasTienda == null) return;

        foreach (var cat in categoriasTienda)
            if (cat != null) cat.Refresh();
    }

    private void LimpiarSeleccion()
    {
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
    }
}