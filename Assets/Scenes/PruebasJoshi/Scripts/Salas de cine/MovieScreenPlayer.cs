using UnityEngine;
using UnityEngine.Video;

public class MovieScreenPlayer : MonoBehaviour
{
    [Header("Video Player de la pantalla")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Render Texture de la pantalla")]
    [SerializeField] private RenderTexture screenRenderTexture;

    [Header("Forzar VideoClip (override / respaldo)")]
    [Tooltip("Si está activo, SIEMPRE reproduce el VideoClip de abajo e ignora la URL. Déjalo DESACTIVADO para usar los URL de cada card.")]
    [SerializeField] private bool forzarVideoClip = false;

    [Tooltip("Clip que se usa si 'Forzar Video Clip' está activo, o como respaldo cuando una card no tiene URL ni VideoClip. Puedes asignar CondusefVideo03.")]
    [SerializeField] private VideoClip videoClipForzado;

    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("MovieScreenPlayer: No se encontró VideoPlayer.");
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        if (screenRenderTexture != null)
            videoPlayer.targetTexture = screenRenderTexture;

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.errorReceived -= OnVideoError;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.Stop();

        Debug.Log("MovieScreenPlayer inicializado correctamente.");
    }

    public void PlayMovie(MovieCardUI movie)
    {
        Debug.Log("MovieScreenPlayer: PlayMovie fue llamado.");

        if (movie == null)
        {
            Debug.LogWarning("MovieScreenPlayer: No hay película seleccionada.");
            return;
        }

        if (videoPlayer == null)
        {
            Debug.LogError("MovieScreenPlayer: Falta asignar VideoPlayer.");
            return;
        }

        if (screenRenderTexture == null)
        {
            Debug.LogWarning("MovieScreenPlayer: No se asignó RenderTexture. Se usará la del VideoPlayer si existe.");
        }

        videoPlayer.Stop();

        // Override global opcional: reproducir siempre el clip forzado.
        if (forzarVideoClip)
        {
            if (videoClipForzado == null)
            {
                Debug.LogError(
                    "MovieScreenPlayer: 'Forzar VideoClip' está activo pero no se " +
                    "asignó 'Video Clip Forzado'. Desactívalo para usar los URL, " +
                    "o arrastra un clip (ej. CondusefVideo03)."
                );
                return;
            }

            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.url = string.Empty;
            videoPlayer.clip = videoClipForzado;

            Debug.Log($"MovieScreenPlayer: VideoClip forzado: {videoClipForzado.name}");
        }
        // Prioridad normal: el URL de la card.
        else if (!string.IsNullOrEmpty(movie.VideoUrl))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.clip = null;                  // evita que un clip viejo tenga prioridad
            videoPlayer.url = movie.VideoUrl;

            Debug.Log($"MovieScreenPlayer: Reproduciendo URL de {movie.MovieId}: {movie.VideoUrl}");
        }
        // Si no hay URL, usa el VideoClip de la card.
        else if (movie.VideoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.url = string.Empty;
            videoPlayer.clip = movie.VideoClip;

            Debug.Log($"MovieScreenPlayer: VideoClip de la card {movie.MovieId}");
        }
        // Último respaldo: el clip forzado (si se asignó).
        else if (videoClipForzado != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.url = string.Empty;
            videoPlayer.clip = videoClipForzado;

            Debug.Log($"MovieScreenPlayer: Sin URL ni clip en la card; se usa respaldo {videoClipForzado.name}");
        }
        else
        {
            Debug.LogWarning($"MovieScreenPlayer: La película {movie.MovieId} no tiene URL ni VideoClip.");
            return;
        }

        if (screenRenderTexture != null)
            videoPlayer.targetTexture = screenRenderTexture;

        Debug.Log("MovieScreenPlayer: Preparando video...");
        videoPlayer.Prepare();
    }

    public void Pause()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.isPlaying)
            videoPlayer.Pause();
    }

    public void Resume()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.isPrepared && !videoPlayer.isPlaying)
            videoPlayer.Play();
    }

    public void Stop()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();
    }

    public void ClearScreen()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (screenRenderTexture != null)
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = screenRenderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = active;
        }
    }

    private void OnVideoPrepared(VideoPlayer preparedVideoPlayer)
    {
        Debug.Log("MovieScreenPlayer: Video preparado. Reproduciendo...");
        preparedVideoPlayer.Play();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"MovieScreenPlayer: Error al reproducir video: {message}");
    }
}