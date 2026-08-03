using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private GameObject ownedMark;      // marca/palomita de "comprado"
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private Button button;

    private int optionIndex;
    private System.Action<int> onSelect;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(() => onSelect?.Invoke(optionIndex));
    }

    public void Setup(int index, Sprite sprite, int price, bool owned, System.Action<int> selectCallback)
    {
        optionIndex = index;
        onSelect = selectCallback;

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
            iconImage.preserveAspect = true;
        }

        // Precio y marca de comprado ocupan la MISMA zona (abajo), se alternan
        if (priceText != null)
            priceText.gameObject.SetActive(!owned);   // oculta el precio si está comprado

        if (ownedMark != null)
            ownedMark.SetActive(owned);                // muestra "COMPRADO" en la banda inferior

        gameObject.SetActive(true);
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedBorder != null) selectedBorder.SetActive(selected);
    }

    public void Hide() => gameObject.SetActive(false);
}