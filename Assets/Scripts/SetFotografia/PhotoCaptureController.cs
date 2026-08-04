using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PhotoCaptureController : MonoBehaviour
{
    [Header("Controlador principal del set")]
    [SerializeField] private PhotoKitSelectorController kitSelectorController;

    [Header("Cámara de fotografía")]
    [Tooltip("Debe ser la misma cámara fija utilizada para visualizar el set.")]
    [SerializeField] private Camera photoCamera;

    [Header("Resolución de la fotografía")]
    [Min(1)]
    [SerializeField] private int captureWidth = 1920;

    [Min(1)]
    [SerializeField] private int captureHeight = 1080;

    [Header("Interfaz principal")]
    [Tooltip("Botón Tomar fotografía que aparece dentro del set.")]
    [SerializeField] private Button takePhotoButton;

    [Tooltip("Objeto raíz del menú de previsualización: fondoFotografia.")]
    [SerializeField] private GameObject photoPanel;

    [Tooltip("RawImage donde se mostrará la fotografía capturada.")]
    [SerializeField] private RawImage photoPreview;

    [Header("Botones del menú de fotografía")]
    [SerializeField] private Button closePhotoButton;
    [SerializeField] private Button downloadPhotoButton;

    [Header("Botones de redes sociales")]
    [SerializeField] private Button instagramButton;
    [SerializeField] private Button xButton;
    [SerializeField] private Button facebookButton;
    [SerializeField] private Button linkedInButton;

    [Header("Configuración del archivo")]
    [SerializeField] private string fileNamePrefix = "SNEF2026_Fotografia";

    private Texture2D capturedTexture;
    private byte[] capturedPngBytes;

    private bool isCapturing;

    public Texture2D CapturedTexture => capturedTexture;
    public byte[] CapturedPngBytes => capturedPngBytes;
    public bool HasCapturedPhoto => capturedPngBytes != null &&
                                    capturedPngBytes.Length > 0;

    private void Start()
    {
        ConfigureButtons();

        if (photoPanel != null)
            photoPanel.SetActive(false);

        SetPhotoActionButtonsInteractable(false);
    }

    private void ConfigureButtons()
    {
        if (takePhotoButton != null)
            takePhotoButton.onClick.AddListener(TakePhoto);

        if (closePhotoButton != null)
            closePhotoButton.onClick.AddListener(ClosePhotoPanel);

        if (downloadPhotoButton != null)
            downloadPhotoButton.onClick.AddListener(DownloadPhoto);

        if (instagramButton != null)
            instagramButton.onClick.AddListener(ShareOnInstagram);

        if (xButton != null)
            xButton.onClick.AddListener(ShareOnX);

        if (facebookButton != null)
            facebookButton.onClick.AddListener(ShareOnFacebook);

        if (linkedInButton != null)
            linkedInButton.onClick.AddListener(ShareOnLinkedIn);
    }

    /// <summary>
    /// Método ejecutado por el botón Tomar fotografía.
    /// </summary>
    public void TakePhoto()
    {
        if (isCapturing)
            return;

        if (photoCamera == null)
        {
            Debug.LogError(
                "PhotoCaptureController: No se ha asignado la cámara de fotografía."
            );

            return;
        }

        if (photoPanel == null)
        {
            Debug.LogError(
                "PhotoCaptureController: No se ha asignado el panel fondoFotografia."
            );

            return;
        }

        if (photoPreview == null)
        {
            Debug.LogError(
                "PhotoCaptureController: No se ha asignado PrevisualizacionFoto."
            );

            return;
        }

        if (captureWidth <= 0 || captureHeight <= 0)
        {
            Debug.LogError(
                "PhotoCaptureController: La resolución de captura no es válida."
            );

            return;
        }

        StartCoroutine(CapturePhotoRoutine());
    }

    private IEnumerator CapturePhotoRoutine()
    {
        isCapturing = true;

        if (takePhotoButton != null)
            takePhotoButton.interactable = false;

        // Evita que el menú anterior aparezca en la captura.
        if (photoPanel != null)
            photoPanel.SetActive(false);

        // Oculta Cambiar Kit, Salir y Tomar fotografía.
        if (kitSelectorController != null)
            kitSelectorController.SetSetButtonsVisible(false);

        /*
         * Esperamos a que Unity actualice la interfaz después de ocultar
         * los botones. Esto evita que aparezcan durante la captura.
         */
        Canvas.ForceUpdateCanvases();

        yield return new WaitForEndOfFrame();

        RenderTexture renderTexture = null;

        RenderTexture previousActiveRenderTexture =
            RenderTexture.active;

        RenderTexture previousCameraTarget =
            photoCamera.targetTexture;

        bool captureSucceeded = false;

        try
        {
            renderTexture = RenderTexture.GetTemporary(
                captureWidth,
                captureHeight,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default
            );

            renderTexture.filterMode = FilterMode.Bilinear;

            photoCamera.targetTexture = renderTexture;

            RenderTexture.active = renderTexture;

            /*
             * Renderiza manualmente la cámara fija del set.
             * De esta manera capturamos exactamente su encuadre.
             */
            photoCamera.Render();

            Texture2D newCapturedTexture = new Texture2D(
                captureWidth,
                captureHeight,
                TextureFormat.RGB24,
                false
            );

            newCapturedTexture.ReadPixels(
                new Rect(
                    0,
                    0,
                    captureWidth,
                    captureHeight
                ),
                0,
                0,
                false
            );

            newCapturedTexture.Apply(false, false);

            // Elimina la captura anterior para evitar fugas de memoria.
            if (capturedTexture != null)
                Destroy(capturedTexture);

            capturedTexture = newCapturedTexture;
            capturedPngBytes = capturedTexture.EncodeToPNG();

            captureSucceeded =
                capturedPngBytes != null &&
                capturedPngBytes.Length > 0;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "PhotoCaptureController: Ocurrió un error al capturar " +
                "la fotografía.\n" +
                exception
            );
        }
        finally
        {
            photoCamera.targetTexture = previousCameraTarget;
            RenderTexture.active = previousActiveRenderTexture;

            if (renderTexture != null)
                RenderTexture.ReleaseTemporary(renderTexture);
        }

        if (captureSucceeded)
        {
            ShowCapturedPhoto();
        }
        else
        {
            /*
             * Si ocurrió un error, devolvemos los botones para que
             * el usuario pueda volver a intentarlo.
             */
            if (kitSelectorController != null)
                kitSelectorController.SetSetButtonsVisible(true);

            SetPhotoActionButtonsInteractable(false);
        }

        if (takePhotoButton != null)
            takePhotoButton.interactable = true;

        isCapturing = false;
    }

    private void ShowCapturedPhoto()
    {
        if (photoPreview != null)
        {
            photoPreview.texture = capturedTexture;

            // Configuración normal de orientación.
            photoPreview.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        if (photoPanel != null)
            photoPanel.SetActive(true);

        SetPhotoActionButtonsInteractable(true);

        // Los botones del set permanecen ocultos detrás del menú.
        if (kitSelectorController != null)
            kitSelectorController.SetSetButtonsVisible(false);
    }

    /// <summary>
    /// Cierra el menú de fotografía y vuelve a mostrar los botones del set.
    /// </summary>
    public void ClosePhotoPanel()
    {
        if (kitSelectorController != null)
        {
            kitSelectorController.ClosePhotoPanel();
            return;
        }

        if (photoPanel != null)
            photoPanel.SetActive(false);
    }

    /// <summary>
    /// Guarda la fotografía en Editor, Windows, macOS o Linux.
    ///
    /// En WebGL se requerirá posteriormente un complemento .jslib,
    /// porque el navegador no permite utilizar File.WriteAllBytes.
    /// </summary>
    public void DownloadPhoto()
    {
        if (!HasCapturedPhoto)
        {
            Debug.LogWarning(
                "PhotoCaptureController: Todavía no existe una fotografía para descargar."
            );

            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR

        Debug.LogWarning(
            "PhotoCaptureController: La descarga dentro de WebGL " +
            "requiere el puente JavaScript que agregaremos en el siguiente paso."
        );

#else

        try
        {
            string folderPath = Path.Combine(
                Application.persistentDataPath,
                "Fotografias"
            );

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = GenerateFileName();

            string completePath = Path.Combine(
                folderPath,
                fileName
            );

            File.WriteAllBytes(
                completePath,
                capturedPngBytes
            );

            Debug.Log(
                "Fotografía guardada correctamente en:\n" +
                completePath
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "PhotoCaptureController: No fue posible guardar " +
                "la fotografía.\n" +
                exception
            );
        }

#endif
    }

    public void ShareOnInstagram()
    {
        PrepareSocialShare("Instagram");
    }

    public void ShareOnX()
    {
        PrepareSocialShare("X");
    }

    public void ShareOnFacebook()
    {
        PrepareSocialShare("Facebook");
    }

    public void ShareOnLinkedIn()
    {
        PrepareSocialShare("LinkedIn");
    }

    private void PrepareSocialShare(string socialNetwork)
    {
        if (!HasCapturedPhoto)
        {
            Debug.LogWarning(
                "PhotoCaptureController: No existe una fotografía para compartir."
            );

            return;
        }

        /*
         * Estos métodos quedan preparados para conectar posteriormente
         * el puente entre Unity WebGL y JavaScript.
         *
         * Un navegador no permite adjuntar directamente una imagen local
         * a Instagram, X, Facebook o LinkedIn solamente mediante una URL.
         */
        Debug.Log(
            "Fotografía preparada para compartir en " +
            socialNetwork +
            ". Falta conectar el puente WebGL con JavaScript."
        );
    }

    private string GenerateFileName()
    {
        string safePrefix = string.IsNullOrWhiteSpace(fileNamePrefix)
            ? "SNEF2026_Fotografia"
            : fileNamePrefix.Trim();

        string date = DateTime.Now.ToString(
            "yyyy-MM-dd_HH-mm-ss"
        );

        return $"{safePrefix}_{date}.png";
    }

    /// <summary>
    /// Devuelve la fotografía como texto Base64.
    /// Este método será utilizado por la integración WebGL.
    /// </summary>
    public string GetCapturedPhotoBase64()
    {
        if (!HasCapturedPhoto)
            return string.Empty;

        return Convert.ToBase64String(capturedPngBytes);
    }

    private void SetPhotoActionButtonsInteractable(bool interactable)
    {
        if (downloadPhotoButton != null)
            downloadPhotoButton.interactable = interactable;

        if (instagramButton != null)
            instagramButton.interactable = interactable;

        if (xButton != null)
            xButton.interactable = interactable;

        if (facebookButton != null)
            facebookButton.interactable = interactable;

        if (linkedInButton != null)
            linkedInButton.interactable = interactable;
    }

    private void OnDestroy()
    {
        if (takePhotoButton != null)
            takePhotoButton.onClick.RemoveListener(TakePhoto);

        if (closePhotoButton != null)
            closePhotoButton.onClick.RemoveListener(ClosePhotoPanel);

        if (downloadPhotoButton != null)
            downloadPhotoButton.onClick.RemoveListener(DownloadPhoto);

        if (instagramButton != null)
            instagramButton.onClick.RemoveListener(ShareOnInstagram);

        if (xButton != null)
            xButton.onClick.RemoveListener(ShareOnX);

        if (facebookButton != null)
            facebookButton.onClick.RemoveListener(ShareOnFacebook);

        if (linkedInButton != null)
            linkedInButton.onClick.RemoveListener(ShareOnLinkedIn);

        if (capturedTexture != null)
        {
            Destroy(capturedTexture);
            capturedTexture = null;
        }

        capturedPngBytes = null;
    }
}