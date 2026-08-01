using UnityEngine;
using UnityEngine.UI;

public class ClothingSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private GameObject lockedOverlay;  // candado
    [SerializeField] private Button button;

    private int optionIndex;
    private System.Action<int> onClick;
    private bool isLocked;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            button.onClick.AddListener(() =>
            {
                if (isLocked)
                {
                    if (UISoundManager.Instance != null)
                        UISoundManager.Instance.PlayCompraErrada();
                    return;
                }
                onClick?.Invoke(optionIndex);
            }); 
                });
    }

    public void Setup(int index, Sprite sprite, bool locked, System.Action<int> clickCallback)
    {
        optionIndex = index;
        onClick = clickCallback;
        isLocked = locked;

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null);
            iconImage.preserveAspect = true;
            iconImage.color = locked ? new Color(0.4f, 0.4f, 0.4f, 1f) : Color.white;
        }

        if (lockedOverlay != null)
            lockedOverlay.SetActive(locked);

        gameObject.SetActive(true);
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedBorder != null) selectedBorder.SetActive(selected);
    }

    public void Hide() => gameObject.SetActive(false);

}