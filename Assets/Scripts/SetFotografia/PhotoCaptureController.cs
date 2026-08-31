using System;

using System.Collections;

using System.IO;

using System.Runtime.InteropServices;

using UnityEngine;

using UnityEngine.UI;



public class PhotoCaptureController : MonoBehaviour

{

    // =========================================================

    // REFERENCIAS PRINCIPALES

    // =========================================================



    [Header("Controlador principal del set")]

    [SerializeField]

    private PhotoKitSelectorController kitSelectorController;





    [Header("Cámara de fotografía")]

    [SerializeField]

    private Camera photoCamera;





    // =========================================================

    // RESOLUCIÓN

    // =========================================================



    [Header("Resolución")]

    [Min(1)]

    [SerializeField]

    private int captureWidth = 1920;



    [Min(1)]

    [SerializeField]

    private int captureHeight = 1080;





    // =========================================================

    // INTERFAZ

    // =========================================================



    [Header("Interfaz")]

    [SerializeField]

    private Button takePhotoButton;



    [SerializeField]

    private GameObject photoPanel;



    [SerializeField]

    private RawImage photoPreview;





    // =========================================================

    // BOTONES

    // =========================================================



    [Header("Botones")]

    [SerializeField]

    private Button closePhotoButton;



    [SerializeField]

    private Button downloadPhotoButton;



    [Tooltip("Botón único que abre el menú nativo de compartir del dispositivo.")]

    [SerializeField]

    private Button sharePhotoButton;





    // =========================================================

    // CONFIGURACIÓN DEL ARCHIVO

    // =========================================================



    [Header("Archivo")]

    [SerializeField]

    private string fileNamePrefix =

        "SNEF2026_Fotografia";





    // =========================================================

    // CONFIGURACIÓN DE COMPARTIR

    // =========================================================



    [Header("Compartir")]

    [SerializeField]

    private string shareTitle =

        "Mi fotografía SNEF 2026";



    [TextArea(2, 4)]

    [SerializeField]

    private string shareText =

        "¡Así viví la experiencia SNEF 2026!";





    // =========================================================

    // VARIABLES INTERNAS

    // =========================================================



    private Texture2D capturedTexture;



    private byte[] capturedPngBytes;



    private bool isCapturing;





    // =========================================================

    // JAVASCRIPT WEBGL

    // =========================================================



#if UNITY_WEBGL && !UNITY_EDITOR



    /*

     * Download.jslib

     *

     * Se ejecuta ÚNICAMENTE al presionar

     * "Descargar fotografía".

     */

    [DllImport("__Internal")]

    private static extern void DownloadBase64File(

        string base64Data,

        string filename,

        string mimeType

    );





    /*

     * SharePlugin.jslib

     *

     * Envía el PNG al menú nativo de compartir.

     *

     * NO debe descargar la fotografía.

     */

    [DllImport("__Internal")]

    private static extern void ShareImageBase64(

        string base64,

        string filename,

        string title,

        string text

    );



#endif





    // =========================================================

    // PROPIEDADES

    // =========================================================



    public Texture2D CapturedTexture =>

        capturedTexture;





    public byte[] CapturedPngBytes =>

        capturedPngBytes;





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

        {

            photoPanel.SetActive(false);

        }





        SetPhotoActionButtonsInteractable(false);

    }





    // =========================================================

    // CONFIGURAR BOTONES

    // =========================================================



    private void ConfigureButtons()

    {

        if (takePhotoButton != null)

        {

            takePhotoButton.onClick.AddListener(

                TakePhoto

            );

        }





        if (closePhotoButton != null)

        {

            closePhotoButton.onClick.AddListener(

                ClosePhotoPanel

            );

        }





        if (downloadPhotoButton != null)

        {

            downloadPhotoButton.onClick.AddListener(

                DownloadPhoto

            );

        }





        if (sharePhotoButton != null)

        {

            sharePhotoButton.onClick.AddListener(

                SharePhoto

            );

        }

    }





    // =========================================================

    // TOMAR FOTOGRAFÍA

    // =========================================================



    public void TakePhoto()

    {

        if (isCapturing)

        {

            return;

        }





        if (photoCamera == null)

        {

            Debug.LogError(

                "[Photo] No se ha asignado Photo Camera."

            );



            return;

        }





        if (photoPreview == null)

        {

            Debug.LogError(

                "[Photo] No se ha asignado Photo Preview."

            );



            return;

        }





        if (photoPanel == null)

        {

            Debug.LogError(

                "[Photo] No se ha asignado Photo Panel."

            );



            return;

        }





        StartCoroutine(

            CapturePhotoRoutine()

        );

    }





    // =========================================================

    // RUTINA DE CAPTURA

    // =========================================================



    private IEnumerator CapturePhotoRoutine()

    {

        isCapturing = true;





        if (takePhotoButton != null)

        {

            takePhotoButton.interactable = false;

        }





        // =====================================================

        // OCULTAR INTERFAZ ANTES DE TOMAR LA FOTO

        // =====================================================



        if (photoPanel != null)

        {

            photoPanel.SetActive(false);

        }





        if (kitSelectorController != null)

        {

            kitSelectorController

                .SetSetButtonsVisible(false);

        }





        Canvas.ForceUpdateCanvases();





        /*

         * Esperamos hasta el final del frame para asegurarnos

         * de que Unity ya haya ocultado los botones.

         */

        yield return new WaitForEndOfFrame();





        // =====================================================

        // PREPARAR RENDER TEXTURE

        // =====================================================



        RenderTexture rt = null;





        RenderTexture previousActive =

            RenderTexture.active;





        RenderTexture previousTarget =

            photoCamera.targetTexture;





        bool captureSuccessful = false;





        try

        {

            rt = RenderTexture.GetTemporary(

                captureWidth,

                captureHeight,

                24,

                RenderTextureFormat.ARGB32

            );





            rt.filterMode =

                FilterMode.Bilinear;





            photoCamera.targetTexture =

                rt;





            RenderTexture.active =

                rt;





            // =================================================

            // RENDERIZAR CÁMARA

            // =================================================



            photoCamera.Render();





            // =================================================

            // CREAR TEXTURA 2D

            // =================================================



            Texture2D newTexture =

                new Texture2D(

                    captureWidth,

                    captureHeight,

                    TextureFormat.RGBA32,

                    false

                );





            newTexture.ReadPixels(

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





            newTexture.Apply(

                false,

                false

            );





            // =================================================

            // ELIMINAR FOTO ANTERIOR

            // =================================================



            if (capturedTexture != null)

            {

                Destroy(

                    capturedTexture

                );

            }





            capturedTexture =

                newTexture;





            // =================================================

            // GENERAR PNG

            // =================================================



            capturedPngBytes =

                capturedTexture.EncodeToPNG();





            if (

                capturedPngBytes == null ||

                capturedPngBytes.Length == 0

            )

            {

                throw new Exception(

                    "EncodeToPNG devolvió una imagen vacía."

                );

            }





            captureSuccessful = true;

        }

        catch (Exception exception)

        {

            Debug.LogError(

                "[Photo] Error capturando fotografía:\n" +

                exception

            );

        }

        finally

        {

            // =================================================

            // RESTAURAR CÁMARA

            // =================================================



            photoCamera.targetTexture =

                previousTarget;





            RenderTexture.active =

                previousActive;





            if (rt != null)

            {

                RenderTexture.ReleaseTemporary(

                    rt

                );

            }

        }





        // =====================================================

        // RESULTADO

        // =====================================================



        if (captureSuccessful)

        {

            ShowCapturedPhoto();

        }

        else

        {

            if (kitSelectorController != null)

            {

                kitSelectorController

                    .SetSetButtonsVisible(true);

            }





            SetPhotoActionButtonsInteractable(

                false

            );

        }





        if (takePhotoButton != null)

        {

            takePhotoButton.interactable = true;

        }





        isCapturing = false;

    }





    // =========================================================

    // MOSTRAR FOTOGRAFÍA

    // =========================================================



    private void ShowCapturedPhoto()

    {

        if (photoPreview != null)

        {

            /*

             * Esta misma Texture2D es la que posteriormente

             * se codifica y se comparte.

             */

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

        {

            photoPanel.SetActive(true);

        }





        SetPhotoActionButtonsInteractable(

            true

        );





        /*

         * Los botones de Cambiar Kit, Salir y Tomar fotografía

         * permanecen ocultos detrás de la previsualización.

         */

        if (kitSelectorController != null)

        {

            kitSelectorController

                .SetSetButtonsVisible(false);

        }

    }





    // =========================================================

    // CERRAR PANEL

    // =========================================================



    public void ClosePhotoPanel()

    {

        if (kitSelectorController != null)

        {

            kitSelectorController

                .ClosePhotoPanel();



            return;

        }





        if (photoPanel != null)

        {

            photoPanel.SetActive(false);

        }

    }





    // =========================================================

    // DESCARGAR FOTOGRAFÍA

    // =========================================================



    public void DownloadPhoto()

    {

        if (!HasCapturedPhoto)

        {

            Debug.LogWarning(

                "[Photo] No existe una fotografía para descargar."

            );



            return;

        }





        string filename =

            GenerateFileName();





#if UNITY_WEBGL && !UNITY_EDITOR



        // =====================================================

        // WEBGL

        // =====================================================



        string base64 =

            Convert.ToBase64String(

                capturedPngBytes

            );





        DownloadBase64File(

            base64,

            filename,

            "image/png"

        );



#else



        // =====================================================

        // EDITOR / PC

        // =====================================================



        try

        {

            string path =

                Path.Combine(

                    Application.persistentDataPath,

                    filename

                );





            File.WriteAllBytes(

                path,

                capturedPngBytes

            );





            Debug.Log(

                "[Photo] Fotografía guardada en:\n" +

                path

            );

        }

        catch (Exception exception)

        {

            Debug.LogError(

                "[Photo] No se pudo guardar la fotografía:\n" +

                exception

            );

        }



#endif

    }





    // =========================================================

    // COMPARTIR FOTOGRAFÍA

    // =========================================================



    public void SharePhoto()

    {

        if (!HasCapturedPhoto)

        {

            Debug.LogWarning(

                "[Share] No existe una fotografía para compartir."

            );



            return;

        }





        string filename =

            GenerateFileName();





#if UNITY_WEBGL && !UNITY_EDITOR



        /*

         * IMPORTANTE:

         *

         * No volvemos a renderizar.

         * No generamos otra fotografía.

         *

         * capturedPngBytes contiene EXACTAMENTE

         * la imagen actualmente visible en Photo Preview.

         */

        string base64 =

            Convert.ToBase64String(

                capturedPngBytes

            );





        ShareImageBase64(

            base64,

            filename,

            shareTitle,

            shareText

        );



#else



        Debug.Log(

            "[Share] El menú nativo de compartir " +

            "solo se ejecutará dentro del build WebGL."

        );



#endif

    }





    // =========================================================

    // GENERAR NOMBRE DE ARCHIVO

    // =========================================================



    private string GenerateFileName()

    {

        string prefix =

            string.IsNullOrWhiteSpace(

                fileNamePrefix

            )

            ? "SNEF2026_Fotografia"

            : fileNamePrefix.Trim();





        string date =

            DateTime.Now.ToString(

                "yyyyMMdd_HHmmss"

            );





        return MakeSafe(

            $"{prefix}_{date}.png"

        );

    }





    // =========================================================

    // SANITIZAR NOMBRE

    // =========================================================



    private string MakeSafe(

        string value

    )

    {

        foreach (

            char character in

            Path.GetInvalidFileNameChars()

        )

        {

            value =

                value.Replace(

                    character,

                    '_'

                );

        }





        return value.Replace(

            ' ',

            '_'

        );

    }





    // =========================================================

    // ACTIVAR / DESACTIVAR BOTONES

    // =========================================================



    private void SetPhotoActionButtonsInteractable(

        bool value

    )

    {

        if (downloadPhotoButton != null)

        {

            downloadPhotoButton.interactable =

                value;

        }





        if (sharePhotoButton != null)

        {

            sharePhotoButton.interactable =

                value;

        }

    }





    // =========================================================

    // LIMPIEZA

    // =========================================================



    private void OnDestroy()

    {

        if (takePhotoButton != null)

        {

            takePhotoButton.onClick.RemoveListener(

                TakePhoto

            );

        }





        if (closePhotoButton != null)

        {

            closePhotoButton.onClick.RemoveListener(

                ClosePhotoPanel

            );

        }





        if (downloadPhotoButton != null)

        {

            downloadPhotoButton.onClick.RemoveListener(

                DownloadPhoto

            );

        }





        if (sharePhotoButton != null)

        {

            sharePhotoButton.onClick.RemoveListener(

                SharePhoto

            );

        }





        if (capturedTexture != null)

        {

            Destroy(

                capturedTexture

            );



            capturedTexture = null;

        }





        capturedPngBytes = null;

    }

}
