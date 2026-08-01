using UnityEngine;
using Controller;

[RequireComponent(typeof(CharacterMover))]
public class FootstepAudio : MonoBehaviour
{
    [Header("Fuente de audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sonidos de paso (varios para alternar)")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Ritmo de pasos")]
    [Tooltip("Segundos entre pasos al caminar.")]
    [SerializeField] private float walkStepInterval = 0.5f;

    [Tooltip("Segundos entre pasos al correr (más rápido).")]
    [SerializeField] private float runStepInterval = 0.3f;

    [Header("Volumen")]
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 1f;

    [Header("Variación de tono (para que no suene idéntico)")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    private CharacterMover mover;
    private float stepTimer;
    private int lastClipIndex = -1;

    private void Awake()
    {
        mover = GetComponent<CharacterMover>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // ¿Se está moviendo? Usamos el eje de animación del mover
        bool isMoving = mover.Axis.sqrMagnitude > 0.01f;

        if (!isMoving)
        {
            // Reinicia el timer para que el primer paso suene apenas empiece a andar
            stepTimer = 0f;
            return;
        }

        bool isRunning = mover.IsRun;
        float interval = isRunning ? runStepInterval : walkStepInterval;

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            PlayFootstep(isRunning);
            stepTimer = interval;
        }
    }

    private void PlayFootstep(bool isRunning)
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        // Elige un clip al azar, evitando repetir el anterior
        int index = Random.Range(0, footstepClips.Length);
        if (footstepClips.Length > 1 && index == lastClipIndex)
            index = (index + 1) % footstepClips.Length;

        lastClipIndex = index;

        AudioClip clip = footstepClips[index];
        if (clip == null) return;

        // Tono aleatorio para variar cada paso
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        float volume = isRunning ? runVolume : walkVolume;
        audioSource.PlayOneShot(clip, volume);
    }
}