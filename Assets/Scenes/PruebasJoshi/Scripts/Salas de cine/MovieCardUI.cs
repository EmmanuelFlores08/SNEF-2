using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class MovieCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Datos de la película")]
    [SerializeField] private string movieId;
    [SerializeField] private string movieTitle;

    [Header("Video temporal / mock")]
    [SerializeField] private VideoClip videoClip;

    [Header("Video futuro backend / CDN")]
    [SerializeField] private string videoUrl;

    [Header("Referencias visuales")]
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private RectTransform animatedTarget;

    [Header("Animación Hover")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.12f;

    [Header("Animación Selección")]
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.12f;

    private Button button;
    private MovieSelectorController controller;
    private Coroutine scaleRoutine;

    private bool isSelected;
    private bool isSelectionAnimating;

    public string MovieId => movieId;
    public string MovieTitle => movieTitle;
    public VideoClip VideoClip => videoClip;
    public string VideoUrl => videoUrl;

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

    public void Init(MovieSelectorController selectorController)
    {
        controller = selectorController;
    }

    private void HandleClick()
    {
        if (controller == null)
            return;

        controller.SelectMovie(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedBorder != null)
            selectedBorder.SetActive(selected);

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
        if (isSelected || isSelectionAnimating)
            return;

        AnimateScale(hoverScale, hoverDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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
            t = Mathf.SmoothStep(0f, 1f, t);

            animatedTarget.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        animatedTarget.localScale = endScale;
    }
}