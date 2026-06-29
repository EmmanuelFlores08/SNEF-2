using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AvatarSelectorController : MonoBehaviour
{
    [Header("Cards de avatar")]
    [SerializeField] private AvatarCardUI[] avatarCards;

    [Header("Preview principal")]
    [SerializeField] private Image avatarPreviewImage;
    [SerializeField] private RectTransform avatarPreviewTransform;

    [Header("Animación preview")]
    [SerializeField] private float previewStartScale = 0.82f;
    [SerializeField] private float previewOvershootScale = 1.08f;
    [SerializeField] private float previewNormalScale = 1f;
    [SerializeField] private float previewInDuration = 0.12f;
    [SerializeField] private float previewBounceDuration = 0.16f;

    [Header("Botón usar")]
    [SerializeField] private Button useButton;

    [Header("Configuración")]
    [SerializeField] private string nextSceneName = "CinePrincipal";

    private AvatarCardUI selectedAvatar;
    private Coroutine previewAnimationRoutine;

    private void Start()
    {
        if (avatarPreviewTransform == null && avatarPreviewImage != null)
            avatarPreviewTransform = avatarPreviewImage.GetComponent<RectTransform>();

        InitCards();

        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedAvatar);

        if (avatarCards.Length > 0)
            SelectAvatar(avatarCards[0]);
    }

    private void InitCards()
    {
        foreach (AvatarCardUI card in avatarCards)
        {
            card.Init(this);
            card.SetSelected(false);
        }
    }

    public void SelectAvatar(AvatarCardUI avatarCard)
    {
        if (selectedAvatar != null)
            selectedAvatar.SetSelected(false);

        selectedAvatar = avatarCard;
        selectedAvatar.SetSelected(true);
        selectedAvatar.PlaySelectionAnimation();

        UpdatePreview(selectedAvatar);
    }

    private void UpdatePreview(AvatarCardUI avatarCard)
    {
        if (avatarPreviewImage == null)
            return;

        avatarPreviewImage.sprite = avatarCard.PreviewSprite;
        avatarPreviewImage.preserveAspect = true;

        PlayPreviewBounceAnimation();
    }

    private void PlayPreviewBounceAnimation()
    {
        if (avatarPreviewTransform == null)
            return;

        if (previewAnimationRoutine != null)
            StopCoroutine(previewAnimationRoutine);

        previewAnimationRoutine = StartCoroutine(PreviewBounceRoutine());
    }

    private IEnumerator PreviewBounceRoutine()
    {
        avatarPreviewTransform.localScale = Vector3.one * previewStartScale;

        yield return ScalePreviewRoutine(previewOvershootScale, previewInDuration);
        yield return ScalePreviewRoutine(previewNormalScale, previewBounceDuration);

        previewAnimationRoutine = null;
    }

    private IEnumerator ScalePreviewRoutine(float targetScale, float duration)
    {
        Vector3 startScale = avatarPreviewTransform.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            t = Mathf.SmoothStep(0f, 1f, t);

            avatarPreviewTransform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        avatarPreviewTransform.localScale = endScale;
    }

    private void UseSelectedAvatar()
    {
        if (selectedAvatar == null)
            return;

        StartCoroutine(SaveSelectedAvatarRoutine(selectedAvatar.AvatarId));
    }

    private IEnumerator SaveSelectedAvatarRoutine(string avatarId)
    {
        PlayerPrefs.SetString("selectedAvatarId", avatarId);
        PlayerPrefs.Save();

        Debug.Log($"Avatar seleccionado: {avatarId}");

        yield return null;

        SceneManager.LoadScene(nextSceneName);
    }
}