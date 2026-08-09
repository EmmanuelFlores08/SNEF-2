using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
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
    [SerializeField] private Button takePhotoButton;

    [Tooltip("Objeto raíz fondoFotografia.")]
    [SerializeField] private GameObject photoPanel;

    [Tooltip("RawImage donde se muestra la fotografía.")]
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

    [Header("Texto para compartir")]
    [SerializeField]
    private string shareTitle =
        "Mi fotografía SNEF 2026";

    [TextArea(2, 4)]
    [SerializeField]
    private string shareText =
        "¡Así viví la experiencia de la SNEF 2026!";

    private Texture2D capturedTexture;
    private byte[] capturedPngBytes;

    private bool isCapturing;


    // =========================================================
    // JAVASCRIPT WEBGL
    // =========================================================

#if UNITY_WEBGL && !UNITY_EDITOR

    [DllImport("__Internal")]
    private static extern void SNEF_DownloadPNG(
        byte[] data,
        int dataLength,
        string fileName
    );

    [DllImport("__Internal")]
    private static extern void SNEF_SharePNG(
        byte[] data,
        int dataLength,
        string fileName,
        string title,
        string text,
        string network
    );

#endif


    public Texture2D CapturedTexture => capturedTexture;

    public byte[] CapturedPngBytes => capturedPngBytes;

    public bool HasCapturedPhoto =>
        capturedPngBytes != null &&
        capturedPngBytes.Length > 0;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ConfigureButtons();

        if (photoPanel != null)
            photoPanel.SetActive(false);

        SetPhotoActionButtonsInteractable(false);
    }


    // =========================================================
    // BOTONES
    // =========================================================

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


    // =========================================================
    // TOMAR FOTOGRAFÍA
    // =========================================================

    public void TakePhoto()
    {
        if (isCapturing)
            return;

        if (photoCamera == null)
        {
            Debug.LogError(
                "PhotoCaptureController: No hay cámara asignada."
            );

            return;
        }

        if (photoPanel == null)
        {
            Debug.LogError(
                "PhotoCaptureController: No hay Photo Panel asignado."
            );

            return;
        }

        if (photoPreview == null)
        {
            Debug.LogError(
                "PhotoCaptureController: No hay RawImage de previsualización."
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


        // ------------------------------------------------------
        // OCULTAR INTERFAZ
        // ------------------------------------------------------

        if (photoPanel != null)
            photoPanel.SetActive(false);

        if (kitSelectorController != null)
            kitSelectorController.SetSetButtonsVisible(false);

        Canvas.ForceUpdateCanvases();

        yield return new WaitForEndOfFrame();


        // ------------------------------------------------------
        // CREAR RENDER TEXTURE
        // ------------------------------------------------------

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

            renderTexture.filterMode =
                FilterMode.Bilinear;


            photoCamera.targetTexture =
                renderTexture;

            RenderTexture.active =
                renderTexture;


            // --------------------------------------------------
            // RENDERIZAR FOTO
            // --------------------------------------------------

            photoCamera.Render();


            Texture2D newCapturedTexture =
                new Texture2D(
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


            newCapturedTexture.Apply(
                false,
                false
            );


            // --------------------------------------------------
            // BORRAR FOTO ANTERIOR
            // --------------------------------------------------

            if (capturedTexture != null)
                Destroy(capturedTexture);


            capturedTexture =
                newCapturedTexture;


            // --------------------------------------------------
            // CONVERTIR A PNG
            // --------------------------------------------------

            capturedPngBytes =
                capturedTexture.EncodeToPNG();


            captureSucceeded =
                capturedPngBytes != null &&
                capturedPngBytes.Length > 0;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Error capturando fotografía:\n" +
                exception
            );
        }
        finally
        {
            photoCamera.targetTexture =
                previousCameraTarget;

            RenderTexture.active =
                previousActiveRenderTexture;

            if (renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(
                    renderTexture
                );
            }
        }


        // ------------------------------------------------------
        // MOSTRAR RESULTADO
        // ------------------------------------------------------

        if (captureSucceeded)
        {
            ShowCapturedPhoto();
        }
        else
        {
            if (kitSelectorController != null)
                kitSelectorController.SetSetButtonsVisible(true);

            SetPhotoActionButtonsInteractable(false);
        }


        if (takePhotoButton != null)
            takePhotoButton.interactable = true;

        isCapturing = false;
    }


    // =========================================================
    // PREVISUALIZACIÓN
    // =========================================================

    private void ShowCapturedPhoto()
    {
        if (photoPreview != null)
        {
            photoPreview.texture =
                capturedTexture;

            photoPreview.uvRect =
                new Rect(
                    0f,
                    0f,
                    1f,
                    1f
                );
        }


        if (photoPanel != null)
            photoPanel.SetActive(true);


        SetPhotoActionButtonsInteractable(true);


        if (kitSelectorController != null)
            kitSelectorController.SetSetButtonsVisible(false);
    }


    // =========================================================
    // CERRAR PREVISUALIZACIÓN
    // =========================================================

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


    // =========================================================
    // DESCARGAR
    // =========================================================

    public void DownloadPhoto()
    {
        if (!HasCapturedPhoto)
        {
            Debug.LogWarning(
                "No existe una fotografía para descargar."
            );

            return;
        }


        string fileName =
            GenerateFileName();


#if UNITY_WEBGL && !UNITY_EDITOR

        // ------------------------------------------------------
        // WEBGL
        // ------------------------------------------------------

        SNEF_DownloadPNG(
            capturedPngBytes,
            capturedPngBytes.Length,
            fileName
        );

#else

        // ------------------------------------------------------
        // EDITOR / PC
        // ------------------------------------------------------

        try
        {
            string folderPath =
                Path.Combine(
                    Application.persistentDataPath,
                    "Fotografias"
                );


            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);


            string completePath =
                Path.Combine(
                    folderPath,
                    fileName
                );


            File.WriteAllBytes(
                completePath,
                capturedPngBytes
            );


            Debug.Log(
                "Fotografía guardada en:\n" +
                completePath
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "No se pudo guardar la fotografía:\n" +
                exception
            );
        }

#endif
    }


    // =========================================================
    // INSTAGRAM
    // =========================================================

    public void ShareOnInstagram()
    {
        SharePhoto(
            "Instagram"
        );
    }


    // =========================================================
    // X
    // =========================================================

    public void ShareOnX()
    {
        SharePhoto(
            "X"
        );
    }


    // =========================================================
    // FACEBOOK
    // =========================================================

    public void ShareOnFacebook()
    {
        SharePhoto(
            "Facebook"
        );
    }


    // =========================================================
    // LINKEDIN
    // =========================================================

    public void ShareOnLinkedIn()
    {
        SharePhoto(
            "LinkedIn"
        );
    }


    // =========================================================
    // COMPARTIR
    // =========================================================

    private void SharePhoto(
        string network
    )
    {
        if (!HasCapturedPhoto)
        {
            Debug.LogWarning(
                "No existe una fotografía para compartir."
            );

            return;
        }


#if UNITY_WEBGL && !UNITY_EDITOR

        string fileName =
            GenerateFileName();


        SNEF_SharePNG(
            capturedPngBytes,
            capturedPngBytes.Length,
            fileName,
            shareTitle,
            shareText,
            network
        );

#else

        Debug.Log(
            "La función de compartir se ejecuta " +
            "desde el build WebGL."
        );

#endif
    }


    // =========================================================
    // NOMBRE DEL ARCHIVO
    // =========================================================

    private string GenerateFileName()
    {
        string safePrefix =
            string.IsNullOrWhiteSpace(fileNamePrefix)
                ? "SNEF2026_Fotografia"
                : fileNamePrefix.Trim();


        string date =
            DateTime.Now.ToString(
                "yyyy-MM-dd_HH-mm-ss"
            );


        return
            $"{safePrefix}_{date}.png";
    }


    // =========================================================
    // BOTONES DISPONIBLES
    // =========================================================

    private void SetPhotoActionButtonsInteractable(
        bool interactable
    )
    {
        if (downloadPhotoButton != null)
            downloadPhotoButton.interactable =
                interactable;

        if (instagramButton != null)
            instagramButton.interactable =
                interactable;

        if (xButton != null)
            xButton.interactable =
                interactable;

        if (facebookButton != null)
            facebookButton.interactable =
                interactable;

        if (linkedInButton != null)
            linkedInButton.interactable =
                interactable;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (takePhotoButton != null)
            takePhotoButton.onClick.RemoveListener(
                TakePhoto
            );

        if (closePhotoButton != null)
            closePhotoButton.onClick.RemoveListener(
                ClosePhotoPanel
            );

        if (downloadPhotoButton != null)
            downloadPhotoButton.onClick.RemoveListener(
                DownloadPhoto
            );

        if (instagramButton != null)
            instagramButton.onClick.RemoveListener(
                ShareOnInstagram
            );

        if (xButton != null)
            xButton.onClick.RemoveListener(
                ShareOnX
            );

        if (facebookButton != null)
            facebookButton.onClick.RemoveListener(
                ShareOnFacebook
            );

        if (linkedInButton != null)
            linkedInButton.onClick.RemoveListener(
                ShareOnLinkedIn
            );


        if (capturedTexture != null)
        {
            Destroy(capturedTexture);
            capturedTexture = null;
        }


        capturedPngBytes = null;
    }
}