using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ChangeAvatarTrigger : MonoBehaviour
{
    [Header("Escena de selección de avatar")]
    [SerializeField] private string avatarSceneName = "SeleccionAvatar"; // nombre exacto de tu escena 1

    [Header("UI")]
    [SerializeField] private GameObject promptPresionarE;

    [Header("Configuración")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<Collider> collidersJugador = new HashSet<Collider>();
    private bool JugadorDentro => collidersJugador.Count > 0;

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

    // Público para que también lo pueda llamar el botón táctil (punto 2)
    public void CambiarEscena()
    {
        SceneManager.LoadScene(avatarSceneName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other)) return;
        collidersJugador.Add(other);
        if (promptPresionarE != null) promptPresionarE.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other)) return;
        collidersJugador.Remove(other);
        if (!JugadorDentro && promptPresionarE != null)
            promptPresionarE.SetActive(false);
    }

    private bool EsJugador(Collider other)
    {
        if (other.CompareTag(playerTag)) return true;
        Transform raiz = other.transform.root;
        return raiz != null && raiz.CompareTag(playerTag);
    }
}