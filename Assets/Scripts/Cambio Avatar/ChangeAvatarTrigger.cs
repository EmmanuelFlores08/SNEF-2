using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChangeAvatarTrigger : MonoBehaviour
{
    [Header("Escena de selección de avatar")]
    [SerializeField] private string avatarSceneName = "SeleccionAvatar"; // nombre exacto de tu escena 1

    [Header("UI")]
    [SerializeField] private GameObject promptPresionarE;

    [Tooltip("Botón del mismo prompt. En móvil se toca para cambiar de avatar.")]
    [SerializeField] private Button botonPrompt;

    [Header("Configuración")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<Collider> collidersJugador = new HashSet<Collider>();
    private bool JugadorDentro => collidersJugador.Count > 0;

    private void Awake()
    {
        // Si no se asignó manualmente, busca el Button en el propio prompt.
        if (botonPrompt == null && promptPresionarE != null)
        {
            botonPrompt = promptPresionarE.GetComponent<Button>();

            if (botonPrompt == null)
                botonPrompt = promptPresionarE.GetComponentInChildren<Button>(true);
        }

        if (botonPrompt != null)
        {
            botonPrompt.onClick.RemoveListener(CambiarEscenaDesdePrompt);
            botonPrompt.onClick.AddListener(CambiarEscenaDesdePrompt);
        }
    }

    private void Start()
    {
        if (promptPresionarE != null) promptPresionarE.SetActive(false);
    }

    private void Update()
    {
        if (!JugadorDentro) return;

        if (Input.GetKeyDown(interactionKey))
            CambiarEscena();
    }

    /// <summary>
    /// Se ejecuta al tocar/presionar el prompt en celular o tablet.
    /// </summary>
    public void CambiarEscenaDesdePrompt()
    {
        if (!JugadorDentro)
            return;

        CambiarEscena();
    }

    private void CambiarEscena()
    {
        SceneManager.LoadScene(avatarSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other)) return;
        collidersJugador.Add(other);
        if (promptPresionarE != null) promptPresionarE.SetActive(true);
        if (botonPrompt != null) botonPrompt.interactable = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other)) return;
        collidersJugador.Remove(other);
        if (!JugadorDentro && promptPresionarE != null)
            promptPresionarE.SetActive(false);

        if (!JugadorDentro && botonPrompt != null)
            botonPrompt.interactable = false;
    }

    private bool EsJugador(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        Transform raiz = other.transform.root;
        return raiz != null && raiz.CompareTag(playerTag);
    }

    private void OnDisable()
    {
        collidersJugador.Clear();

        if (promptPresionarE != null)
            promptPresionarE.SetActive(false);

        if (botonPrompt != null)
            botonPrompt.interactable = false;
    }

    private void OnDestroy()
    {
        if (botonPrompt != null)
            botonPrompt.onClick.RemoveListener(CambiarEscenaDesdePrompt);
    }
}
