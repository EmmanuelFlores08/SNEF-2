using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AvatarSelectorController : MonoBehaviour
{
    [Header("Cards de avatar")]
    [SerializeField] private AvatarCardUI[] avatarCards;

    [Header("Preview principal")]
    [Tooltip("Preview 3D en vivo (como la taquilla). Si se asigna, muestra el modelo real del avatar.")]
    [SerializeField] private Avatar3DPreview preview3D;
    [Tooltip("Sprite estático opcional. Puede dejarse vacío si usas el preview 3D.")]
    [SerializeField] private Image avatarPreviewImage;
    [Tooltip("RectTransform que recibe la animación de bounce (el RawImage del RenderTexture, o la Image).")]
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

    private void OnEnable()
    {
        if (SnefBridge.Instance != null)
            SnefBridge.Instance.OnStateChanged += HandleBridgeStateChanged;
    }

    private void OnDisable()
    {
        if (SnefBridge.Instance != null)
            SnefBridge.Instance.OnStateChanged -= HandleBridgeStateChanged;
    }

    private void Start()
    {
        if (avatarPreviewTransform == null && avatarPreviewImage != null)
            avatarPreviewTransform = avatarPreviewImage.GetComponent<RectTransform>();

        InitCards();

        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedAvatar);

        if (!ApplyServerAvatarSelection() && avatarCards.Length > 0)
            SelectAvatar(avatarCards[0]);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        // Preview 3D en vivo (mismo enfoque que la taquilla de Cine).
        if (preview3D != null)
            preview3D.Show(avatarCard.AvatarId);

        // Sprite estático opcional (si todavía se usa una imagen).
        if (avatarPreviewImage != null)
        {
            avatarPreviewImage.sprite = avatarCard.PreviewSprite;
            avatarPreviewImage.preserveAspect = true;
        }

        PlayPreviewBounceAnimation();
    }

    private void PlayPreviewBounceAnimation()
    {
        if (avatarPreviewTransform == null)
        {
            Debug.LogWarning(
                "AvatarSelectorController: 'Avatar Preview Transform' está vacío, " +
                "por eso no hay animación. Asígnalo al RectTransform del RawImage " +
                "que muestra el RenderTexture del avatar."
            );
            return;
        }

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
        SendAvatarSelectMetric(avatarId);
        SendAvatarPersistence(avatarId);

        yield return null;

        // Usa el SceneLoader (pantalla de carga con barra) si existe
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene(nextSceneName);
        else
            SceneManager.LoadScene(nextSceneName); // respaldo si no hay SceneLoader
    }

    private void SendAvatarSelectMetric(string avatarId)
    {
        if (!IsValidAvatarId(avatarId))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                $"AvatarSelectorController: No se envio avatar_select; avatarId invalido: '{avatarId}'.",
                this
            );
#endif
            return;
        }

        SnefMetrics.Send("avatar_select", avatarId);
    }

    private void SendAvatarPersistence(string avatarId)
    {
        if (!IsValidAvatarId(avatarId))
            return;

        if (SnefBridge.Instance == null)
        {
            return;
        }

        SnefBridge.Instance.RequestAvatarSave(avatarId);
    }

    private bool ApplyServerAvatarSelection()
    {
        if (SnefBridge.Instance == null ||
            !SnefBridge.Instance.HasServerState ||
            string.IsNullOrWhiteSpace(SnefBridge.Instance.AvatarId))
        {
            return false;
        }

        AvatarCardUI card = FindAvatarCard(SnefBridge.Instance.AvatarId);
        if (card == null)
            return false;

        SelectAvatar(card);
        return true;
    }

    private void HandleBridgeStateChanged()
    {
        ApplyServerAvatarSelection();
    }

    private AvatarCardUI FindAvatarCard(string avatarId)
    {
        if (avatarCards == null)
            return null;

        foreach (AvatarCardUI card in avatarCards)
        {
            if (card != null && card.AvatarId == avatarId)
                return card;
        }

        return null;
    }

    private bool IsValidAvatarId(string avatarId)
    {
        switch (avatarId)
        {
            case "avatar_01":
            case "avatar_02":
            case "avatar_03":
            case "avatar_04":
            case "avatar_05":
            case "avatar_06":
            case "avatar_07":
            case "avatar_08":
            case "avatar_09":
            case "avatar_10":
            case "avatar_11":
            case "avatar_12":
                return true;
            default:
                return false;
        }
    }
}
