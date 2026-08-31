using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Cuando el juego corre en móvil/tablet, apaga los videos que se reproducen
/// mediante RenderTexture (las TVs promocionales: TV16_9, TV9_16, TVCenefa, etc.)
/// para ahorrar rendimiento. En escritorio no hace nada.
/// </summary>
public class VideosOffEnMovil : MonoBehaviour
{
    [Header("Videos a apagar en móvil")]
    [Tooltip("VideoPlayers de las TVs promocionales. En móvil se detienen y se desactivan.")]
    [SerializeField] private VideoPlayer[] videoPlayers;

    [Header("Objetos a desactivar en móvil (opcional)")]
    [Tooltip("Objetos completos a desactivar (ej. las TVs enteras) si además quieres ocultarlas.")]
    [SerializeField] private GameObject[] objetosADesactivar;

    [Header("Limpiar pantalla")]
    [Tooltip("Pinta de negro el RenderTexture al apagar el video, para no dejar el último frame congelado.")]
    [SerializeField] private bool limpiarRenderTextures = true;

    [Header("Pruebas")]
    [Tooltip("Simula móvil dentro del Editor para probar.")]
    [SerializeField] private bool forzarMovilEnEditor = false;

    private void Start()
    {
        if (!DispositivoUtil.EsMovilOTablet(forzarMovilEnEditor))
            return; // En escritorio no tocamos nada.

        ApagarVideoPlayers();

        if (objetosADesactivar != null)
        {
            foreach (GameObject go in objetosADesactivar)
                if (go != null) go.SetActive(false);
        }
    }

    private void ApagarVideoPlayers()
    {
        if (videoPlayers == null) return;

        foreach (VideoPlayer vp in videoPlayers)
        {
            if (vp == null) continue;

            RenderTexture rt = vp.targetTexture;

            vp.Stop();
            vp.enabled = false;

            if (limpiarRenderTextures && rt != null)
                LimpiarRenderTexture(rt);
        }
    }

    private void LimpiarRenderTexture(RenderTexture rt)
    {
        RenderTexture anterior = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = anterior;
    }
}
