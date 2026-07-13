using UnityEngine;
using UnityEngine.UI;

public class ClothingCategoryUI : MonoBehaviour
{
    [SerializeField] private CustomizationCatalog.BodyPartType bodyPartType;
    [SerializeField] private CustomizationCatalog catalog;

    [Header("Recuadros (5 fijos)")]
    [SerializeField] private ClothingSlotUI[] slots;

    [Header("Paginación")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;

    private PlayerCharacterCustomized character;
    private int pageStart = 0;
    private int selectedIndex = 0;

    private int PageSize => slots.Length;

    private void Awake()
    {
        if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
        if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
    }

    public void SetCharacter(PlayerCharacterCustomized newCharacter)
    {
        character = newCharacter;
        if (character != null)
            selectedIndex = character.GetCurrentIndex(bodyPartType);

        pageStart = (selectedIndex / PageSize) * PageSize;
        Refresh();
    }

    private void NextPage()
    {
        var cat = catalog.GetCatalog(bodyPartType);
        if (cat == null) return;

        if (pageStart + PageSize < cat.optionArray.Length)
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

    private void OnSlotClicked(int optionIndex)
    {
        if (character == null) return;

        selectedIndex = optionIndex;
        character.SetBodyPart(bodyPartType, optionIndex);
        UpdateSelectionHighlight();
    }

    public void Refresh()
    {
        var cat = catalog.GetCatalog(bodyPartType);
        if (cat == null) return;

        int total = cat.optionArray.Length;

        for (int i = 0; i < slots.Length; i++)
        {
            int optionIndex = pageStart + i;

            if (optionIndex < total)
            {
                var option = cat.optionArray[optionIndex];

                bool locked = !option.gratuito
                    && PlayerInventory.Instance != null
                    && !PlayerInventory.Instance.IsOwned(option.optionId);

                slots[i].Setup(optionIndex, option.previewSprite, locked, OnSlotClicked);
            }
            else
            {
                slots[i].Hide();
            }
        }

        UpdateSelectionHighlight();
        UpdatePageButtons(total);
    }

    private void UpdateSelectionHighlight()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int optionIndex = pageStart + i;
            slots[i].SetSelected(optionIndex == selectedIndex);
        }
    }

    private void UpdatePageButtons(int total)
    {
        if (prevPageButton != null) prevPageButton.interactable = (pageStart > 0);
        if (nextPageButton != null) nextPageButton.interactable = (pageStart + PageSize < total);
    }
}