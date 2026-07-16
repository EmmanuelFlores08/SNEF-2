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

    public System.Action<ShopCategoryUI, int> OnItemSelected;

    public TipoCategoria Tipo => tipo;
    public CustomizationCatalog.BodyPartType BodyPartType => bodyPartType;

    private void Awake()
    {
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
    }

    private int GetTotal()
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

    public void Refresh()
    {
        int total = GetTotal();

        for (int i = 0; i < slots.Length; i++)
        {
            int index = pageStart + i;

            if (index < total)
            {
                GetItem(index, out string id, out Sprite sprite, out int price, out bool gratuito);

                bool owned = gratuito ||
                    (PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(id));

                slots[i].Setup(index, sprite, price, owned, OnSlotClicked);
                slots[i].SetSelected(index == selectedIndex);
            }
            else
            {
                slots[i].Hide();
            }
        }

        UpdatePageButtons(total);
    }

    private void OnSlotClicked(int index)
    {
        Debug.Log($"[ShopCategoryUI] Slot clickeado. tipo={tipo} index={index}");
        selectedIndex = index;
        UpdateSelectionHighlight();
        OnItemSelected?.Invoke(this, index);
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
            int index = pageStart + i;
            slots[i].SetSelected(index == selectedIndex);
        }
    }

    private void NextPage()
    {
        int total = GetTotal();
        if (pageStart + PageSize < total) { pageStart += PageSize; Refresh(); }
    }

    private void PrevPage()
    {
        if (pageStart - PageSize >= 0) { pageStart -= PageSize; Refresh(); }
    }

    private void UpdatePageButtons(int total)
    {
        if (prevPageButton != null) prevPageButton.interactable = (pageStart > 0);
        if (nextPageButton != null) nextPageButton.interactable = (pageStart + PageSize < total);
    }
}