using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance { get; private set; }

    [Header("Fuente de audio para UI")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sonidos")]
    [SerializeField] private AudioClip cerrarMenuSound;
    [SerializeField] private AudioClip abrirMenuSound;
    [SerializeField] private AudioClip compraErradaSound;
    [SerializeField] private AudioClip botonSound;       // hover del cursor
    [SerializeField] private AudioClip seleccionSound;   // clic de selección
    [SerializeField] private AudioClip compra;   // clic de selección
    [SerializeField] private AudioClip usarKitSound;
    [Header("Música de fondo")]
    [SerializeField] private AudioSource musicSource; // AudioSource SEPARADO, en loop
    [SerializeField] private AudioClip musicaGeneral; // música normal de la escena
    [SerializeField] private AudioClip musicaSet;     // música del set de fotos
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

    private AudioClip musicaActual;

    [Header("Volumen")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Arranca la música de fondo general
        PlayMusicaGeneral();
    }

    public void PlayCerrarMenu() => Play(cerrarMenuSound);
    public void PlayAbrirMenu() => Play(abrirMenuSound);
    public void PlayCompraErrada() => Play(compraErradaSound);
    public void PlayBoton() => Play(botonSound);
    public void PlaySeleccion() => Play(seleccionSound);
    public void PlayCompra() => Play(compra);
    public void PlayUsarKit() => Play(usarKitSound);

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
    public void PlayMusicaGeneral()
    {
        CambiarMusica(musicaGeneral);
    }

    public void PlayMusicaSet()
    {
        CambiarMusica(musicaSet);
    }

    public void PausarMusica()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ReanudarMusica()
    {
        if (musicSource != null && !musicSource.isPlaying)
            musicSource.UnPause();
    }

    private void CambiarMusica(AudioClip nuevaMusica)
    {
        if (musicSource == null || nuevaMusica == null) return;

        // Si ya está sonando esa misma música, no la reinicies
        if (musicaActual == nuevaMusica && musicSource.isPlaying)
            return;

        musicaActual = nuevaMusica;
        musicSource.clip = nuevaMusica;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }
}