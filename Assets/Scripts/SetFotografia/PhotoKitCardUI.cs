using UnityEngine;
using UnityEngine.UI;

public class PhotoKitCardUI : MonoBehaviour
{
    private static readonly Vector2 PreviewPadding = new Vector2(18f, 14f);

    [SerializeField] private Image previewImage;
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private Button button;

    private PhotoKitSelectorController controller;
    private int kitIndex;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (selectedBorder != null) selectedBorder.SetActive(false);
        button.onClick.AddListener(() => controller?.SelectKit(kitIndex));
    }

    public void Init(PhotoKitSelectorController selectorController, int index)
    {
        controller = selectorController;
        kitIndex = index;
    }

    // Configura la card para un kit concreto (con su imagen)
    public void Setup(int index, Sprite sprite)
    {
        kitIndex = index;

        if (previewImage != null)
        {
            previewImage.sprite = sprite;
            previewImage.enabled = (sprite != null);
            previewImage.preserveAspect = true;
            FitPreviewImageToCard();
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

    private void FitPreviewImageToCard()
    {
        RectTransform previewRect = previewImage.rectTransform;

        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.offsetMin = PreviewPadding;
        previewRect.offsetMax = -PreviewPadding;
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = Vector2.zero;
        previewRect.localScale = Vector3.one;
    }
}
