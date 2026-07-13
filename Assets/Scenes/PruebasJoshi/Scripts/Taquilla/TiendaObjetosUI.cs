using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Controller;
using System.Collections.Generic;

public class TiendaObjetosUI : MonoBehaviour
{
    [Header("Interfaz completa")]
    [SerializeField] private GameObject uiTaquilla;

    [Header("Paneles de contenido")]
    [SerializeField] private GameObject panelObjetosPersonaje;
    [SerializeField] private GameObject panelObjetosSetDeGrabacion;

    [Header("Botones")]
    [SerializeField] private Button buttonPersonaje;
    [SerializeField] private Button buttonSetDeGrabacion;
    [SerializeField] private Button buttonCerrar;
    [SerializeField] private Button buttonComprar;

    [Header("Animaciones de tabs")]
    [SerializeField] private BotonUIAnimado animacionButtonPersonaje;
    [SerializeField] private BotonUIAnimado animacionButtonSetDeGrabacion;

    [Header("Saldo del jugador")]
    [SerializeField] private TextMeshProUGUI textoSaldo;

    [Header("Categorías de tienda")]
    [SerializeField] private ShopCategoryUI[] categoriasTienda;

    [Header("Preview del avatar")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private Camera previewCamera;
    [SerializeField] private CharacterPreviewRotator previewRotator;

    [Header("Cursor")]
    [SerializeField] private CursorLockManager cursorLockManager;

    [Header("Configuración")]
    [SerializeField] private bool abrirSiempreEnPersonaje = true;
    [SerializeField] private bool permitirCerrarConEscape = true;

    public bool EstaAbierta { get; private set; }

    // Referencias del personaje (asignadas en runtime)
    private PlayerCharacterCustomized character;
    private MovePlayerInput playerInput;

    // Posición previa del avatar
    private Vector3 savedPosition;
    private Quaternion savedRotation;

    // Lo que llevaba puesto antes de previsualizar (para restaurar si no compra)
    private Dictionary<CustomizationCatalog.BodyPartType, int> originalOutfit
        = new Dictionary<CustomizationCatalog.BodyPartType, int>();

    // Selección actual
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

        if (buttonComprar != null)
            buttonComprar.onClick.AddListener(ComprarSeleccionado);

        // Escucha la selección de cada categoría
        if (categoriasTienda != null)
        {
            foreach (var cat in categoriasTienda)
            {
                if (cat != null) cat.OnItemSelected += OnItemSelected;
            }
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

    // Llamado por CustomizationSceneSetup tras instanciar el personaje
    public void Bind(PlayerCharacterCustomized newCharacter, MovePlayerInput newInput)
    {
        character = newCharacter;
        playerInput = newInput;
    }

    // ---- Selección y preview ----

    private void OnItemSelected(ShopCategoryUI categoria, int index)
    {
        selectedCategory = categoria;
        selectedIndex = index;

        // Solo previsualizamos prendas (los kits no se ponen en el avatar)
        if (categoria.Tipo == ShopCategoryUI.TipoCategoria.Prenda && character != null)
        {
            character.SetBodyPart(categoria.BodyPartType, index);
        }

        ActualizarBotonComprar();
    }

    private void ActualizarBotonComprar()
    {
        if (buttonComprar == null) return;

        if (selectedCategory == null || selectedIndex < 0)
        {
            buttonComprar.interactable = false;
            return;
        }

        selectedCategory.GetItem(selectedIndex, out string id, out _, out int price, out bool gratuito);

        bool owned = gratuito ||
            (PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(id));

        bool canAfford = PlayerInventory.Instance != null &&
            PlayerInventory.Instance.CanAfford(price);

        buttonComprar.interactable = !owned && canAfford;
    }

    private void ComprarSeleccionado()
    {
        Debug.Log("ComprarSeleccionado fue llamado");

        if (selectedCategory == null || selectedIndex < 0)
        {
            Debug.LogWarning($"Sin selección válida: cat={(selectedCategory != null ? "OK" : "NULL")}, index={selectedIndex}");
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory.Instance es NULL");
            return;
        }

        selectedCategory.GetItem(selectedIndex, out string id, out _, out int price, out _);
        Debug.Log($"Intentando comprar: id='{id}' precio={price} | monedas={PlayerInventory.Instance.Coins}");

        bool ok = PlayerInventory.Instance.TryPurchase(id, price);
        Debug.Log($"TryPurchase devolvió: {ok}");

        if (ok)
        {
            if (selectedCategory.Tipo == ShopCategoryUI.TipoCategoria.Prenda)
                originalOutfit[selectedCategory.BodyPartType] = selectedIndex;

            RefrescarCategorias();
            ActualizarBotonComprar();
        }
    }

    // ---- Abrir / Cerrar ----

    public void AbrirTienda()
    {
        if (EstaAbierta) return;
        if (uiTaquilla == null) return;

        EstaAbierta = true;
        uiTaquilla.SetActive(true);

        // Congela al jugador
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

        // Guarda el outfit actual para restaurarlo si no compra
        GuardarOutfitActual();

        // Teletransporta al avatar al preview
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

        // Restaura el outfit: quita lo que solo estaba previsualizando
        RestaurarOutfitOriginal();

        if (uiTaquilla != null) uiTaquilla.SetActive(false);
        if (previewCamera != null) previewCamera.gameObject.SetActive(false);
        if (previewRotator != null) previewRotator.SetTarget(null);

        // Devuelve al avatar a su sitio
        if (character != null && previewAnchor != null)
        {
            CharacterController cc = character.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            character.transform.SetPositionAndRotation(savedPosition, savedRotation);

            if (cc != null) cc.enabled = true;
        }

        // Reactiva el control
        if (character != null)
        {
            CharacterMover mover = character.GetComponent<CharacterMover>();
            if (mover != null) mover.enabled = true;
        }
        if (playerInput != null) playerInput.enabled = true;

        if (cursorLockManager != null) cursorLockManager.SetInterfaceMode(false);
    }

    // ---- Outfit ----

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
        {
            character.SetBodyPart(kvp.Key, kvp.Value);
        }
    }

    // ---- Saldo y categorías ----

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

        ActualizarBotonComprar();
    }

    // ---- Pestañas (igual que antes) ----

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
        ActualizarEstadoVisualTabs();
    }

    public void MostrarPanelSetDeGrabacion()
    {
        panelActual = TipoPanel.SetDeGrabacion;
        if (panelObjetosPersonaje != null) panelObjetosPersonaje.SetActive(false);
        if (panelObjetosSetDeGrabacion != null) panelObjetosSetDeGrabacion.SetActive(true);
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
        if (buttonComprar != null) buttonComprar.onClick.RemoveListener(ComprarSeleccionado);
    }
}