using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AvatarCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos del avatar")]
    [SerializeField] private string avatarId;
    [SerializeField] private Sprite previewSprite;

    [Header("Referencias visuales")]
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private RectTransform animatedTarget;

    [Header("Animación Hover")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.07f;
    [SerializeField] private float hoverDuration = 0.12f;

    [Header("Animación Selección")]
    [SerializeField] private float pressedScale = 0.90f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.12f;

    private Button button;
    private AvatarSelectorController controller;
    private Coroutine scaleRoutine;

    private bool isSelected;
    private bool isSelectionAnimating;

    public string AvatarId => avatarId;
    public Sprite PreviewSprite => previewSprite;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (animatedTarget == null)
            animatedTarget = GetComponent<RectTransform>();

        if (selectedBorder != null)
            selectedBorder.SetActive(false);

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    public void Init(AvatarSelectorController selectorController)
    {
        controller = selectorController;
    }

    private void HandleClick()
    {
        if (controller == null)
            return;

        controller.SelectAvatar(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedBorder != null)
            selectedBorder.SetActive(selected);

        // Si la card queda seleccionada, cancelamos hover y la dejamos en escala normal.
        if (isSelected && !isSelectionAnimating)
            AnimateScale(normalScale, hoverDuration);
    }

    public void PlaySelectionAnimation()
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(SelectionAnimationRoutine());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Si ya está seleccionada, no debe crecer con hover.
        if (isSelected || isSelectionAnimating)
            return;

        AnimateScale(hoverScale, hoverDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Si ya está seleccionada, no hacemos nada.
        if (isSelected || isSelectionAnimating)
            return;

        AnimateScale(normalScale, hoverDuration);
    }

    private void AnimateScale(float targetScale, float duration)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale, duration));
    }

    private IEnumerator SelectionAnimationRoutine()
    {
        isSelectionAnimating = true;

        yield return ScaleRoutine(pressedScale, pressDuration);
        yield return ScaleRoutine(normalScale, releaseDuration);

        isSelectionAnimating = false;
        scaleRoutine = null;
    }

    private IEnumerator ScaleRoutine(float targetScale, float duration)
    {
        Vector3 startScale = animatedTarget.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            animatedTarget.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        animatedTarget.localScale = endScale;
    }
}