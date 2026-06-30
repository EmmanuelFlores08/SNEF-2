using UnityEngine;
using UnityEngine.Video;

public class MovieScreenPlayer : MonoBehaviour
{
    [Header("Video Player de la pantalla")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Render Texture de la pantalla")]
    [SerializeField] private RenderTexture screenRenderTexture;

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

        if (movie.VideoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = movie.VideoClip;

            Debug.Log($"MovieScreenPlayer: VideoClip asignado para {movie.MovieId}");
        }
        else if (!string.IsNullOrEmpty(movie.VideoUrl))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = movie.VideoUrl;

            Debug.Log($"MovieScreenPlayer: VideoUrl asignada para {movie.MovieId}: {movie.VideoUrl}");
        }
        else
        {
            Debug.LogWarning($"MovieScreenPlayer: La película {movie.MovieId} no tiene VideoClip ni VideoUrl.");
            return;
        }

        if (screenRenderTexture != null)
            videoPlayer.targetTexture = screenRenderTexture;

        Debug.Log("MovieScreenPlayer: Preparando video...");
        videoPlayer.Prepare();
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