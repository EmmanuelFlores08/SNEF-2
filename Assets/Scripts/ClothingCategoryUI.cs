using UnityEngine;
using UnityEngine.UI;

public class ClothingCategoryUI : MonoBehaviour
{
    [SerializeField] private CustomizationCatalog.BodyPartType bodyPartType;
    [SerializeField] private CustomizationCatalog catalog;

    [Header("Recuadros (5 fijos en el Canvas)")]
    [SerializeField] private ClothingSlotUI[] slots; // exactamente 5

    [Header("Paginación")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;

    private PlayerCharacterCustomized character;
    private int pageStart = 0;       // índice de la primera opción visible
    private int selectedIndex = 0;

    private int PageSize => slots.Length; // 5

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

        // Arranca en la página donde está la opción seleccionada
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

    private void Refresh()
    {
        var cat = catalog.GetCatalog(bodyPartType);
        if (cat == null) return;

        int total = cat.optionArray.Length;

        for (int i = 0; i < slots.Length; i++)
        {
            int optionIndex = pageStart + i;

            if (optionIndex < total)
            {
                Sprite sprite = cat.optionArray[optionIndex].previewSprite;
                slots[i].Setup(optionIndex, sprite, OnSlotClicked);
            }
            else
            {
                slots[i].Hide(); // sobran recuadros en la última página
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
        if (prevPageButton != null)
            prevPageButton.interactable = (pageStart > 0);
        if (nextPageButton != null)
            nextPageButton.interactable = (pageStart + PageSize < total);
    }
}