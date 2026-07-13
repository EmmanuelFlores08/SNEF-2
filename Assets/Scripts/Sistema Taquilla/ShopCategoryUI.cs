using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopCategoryUI : MonoBehaviour
{
    public enum TipoCategoria { Prenda, Kit }

    [Header("Tipo y fuente de datos")]
    [SerializeField] private TipoCategoria tipo = TipoCategoria.Prenda;
    [SerializeField] private CustomizationCatalog.BodyPartType bodyPartType;
    [SerializeField] private CustomizationCatalog clothingCatalog;
    [SerializeField] private PhotoKitCatalog kitCatalog;

    [Header("Recuadros (5 fijos)")]
    [SerializeField] private ShopSlotUI[] slots;

    [Header("Paginación")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;

    private int pageStart = 0;
    private int selectedIndex = -1;
    private int PageSize => slots.Length;

    // Índices del catálogo que AÚN NO están comprados (los únicos que se muestran)
    private readonly List<int> availableIndices = new List<int>();

    public System.Action<ShopCategoryUI, int> OnItemSelected;

    public TipoCategoria Tipo => tipo;
    public CustomizationCatalog.BodyPartType BodyPartType => bodyPartType;

    private void Awake()
    {
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
    }

    private int GetCatalogTotal()
    {
        if (tipo == TipoCategoria.Kit)
            return kitCatalog != null ? kitCatalog.kits.Length : 0;

        var cat = clothingCatalog != null ? clothingCatalog.GetCatalog(bodyPartType) : null;
        return cat != null ? cat.optionArray.Length : 0;
    }

    public void GetItem(int index, out string id, out Sprite sprite, out int price, out bool gratuito)
    {
        id = null; sprite = null; price = 0; gratuito = false;

        if (tipo == TipoCategoria.Kit)
        {
            var kit = kitCatalog.GetKit(index);
            if (kit == null) return;
            id = kit.kitId; sprite = kit.previewSprite;
            price = kit.precio; gratuito = kit.gratuito;
        }
        else
        {
            var cat = clothingCatalog.GetCatalog(bodyPartType);
            if (cat == null || index >= cat.optionArray.Length) return;
            var opt = cat.optionArray[index];
            id = opt.optionId; sprite = opt.previewSprite;
            price = opt.precio; gratuito = opt.gratuito;
        }
    }

    // Construye la lista de los que aún se pueden comprar
    private void BuildAvailableList()
    {
        availableIndices.Clear();

        int total = GetCatalogTotal();

        for (int i = 0; i < total; i++)
        {
            GetItem(i, out string id, out _, out _, out bool gratuito);

            bool owned = gratuito ||
                (PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(id));

            if (!owned)
                availableIndices.Add(i);
        }
    }

    public void Refresh()
    {
        BuildAvailableList();

        // Si la página actual quedó fuera de rango (porque compraste cosas), retrocede
        if (pageStart >= availableIndices.Count && pageStart > 0)
        {
            pageStart = Mathf.Max(0, ((availableIndices.Count - 1) / PageSize) * PageSize);
        }

        for (int i = 0; i < slots.Length; i++)
        {
            int listPos = pageStart + i;

            if (listPos < availableIndices.Count)
            {
                int catalogIndex = availableIndices[listPos];

                GetItem(catalogIndex, out string id, out Sprite sprite, out int price, out _);

                // owned siempre es false aquí, porque solo listamos los no comprados
                slots[i].Setup(catalogIndex, sprite, price, false, OnSlotClicked);
                slots[i].SetSelected(catalogIndex == selectedIndex);
            }
            else
            {
                slots[i].Hide();
            }
        }

        UpdatePageButtons();
    }

    private void OnSlotClicked(int catalogIndex)
    {
        selectedIndex = catalogIndex;
        UpdateSelectionHighlight();
        OnItemSelected?.Invoke(this, catalogIndex);
    }

    public void ClearSelection()
    {
        selectedIndex = -1;
        UpdateSelectionHighlight();
    }

    private void UpdateSelectionHighlight()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int listPos = pageStart + i;

            if (listPos < availableIndices.Count)
            {
                int catalogIndex = availableIndices[listPos];
                slots[i].SetSelected(catalogIndex == selectedIndex);
            }
        }
    }

    private void NextPage()
    {
        if (pageStart + PageSize < availableIndices.Count)
        {
            pageStart += PageSize;
            Refresh();
        }
    }

    private void PrevPage()
    {
        if (pageStart - PageSize >= 0)
        {
            pageStart -= PageSize;
            Refresh();
        }
    }

    private void UpdatePageButtons()
    {
        if (prevPageButton != null)
            prevPageButton.interactable = (pageStart > 0);

        if (nextPageButton != null)
            nextPageButton.interactable = (pageStart + PageSize < availableIndices.Count);
    }
}