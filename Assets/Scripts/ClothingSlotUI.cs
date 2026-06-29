using UnityEngine;
using UnityEngine.UI;

public class ClothingSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private Button button;

    private int optionIndex;
    private System.Action<int> onClick;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(() => onClick?.Invoke(optionIndex));
    }

    // Configura este recuadro para una opción concreta
    public void Setup(int index, Sprite sprite, System.Action<int> clickCallback)
    {
        optionIndex = index;
        onClick = clickCallback;

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
            iconImage.preserveAspect = true;
        }

        gameObject.SetActive(true);
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedBorder != null) selectedBorder.SetActive(selected);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}