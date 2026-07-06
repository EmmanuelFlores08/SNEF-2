using UnityEngine;
using UnityEngine.UI;

public class PhotoKitCardUI : MonoBehaviour
{
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

    public void SetSelected(bool selected)
    {
        if (selectedBorder != null) selectedBorder.SetActive(selected);
    }
}