using System.Collections.Generic;
using UnityEngine;

public class TiendaObjetosTrigger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TiendaObjetosUI tiendaObjetosUI;

    [Tooltip("La imagen completa que muestra E + Comprar objetos.")]
    [SerializeField] private GameObject promptPresionarE;

    [Header("Configuración")]
    [SerializeField] private string tagJugador = "Player";

    [Tooltip("Cierra la interfaz si el jugador sale del área.")]
    [SerializeField] private bool cerrarAlSalirDeLaZona = true;

    private readonly HashSet<Collider> collidersJugador =
        new HashSet<Collider>();

    private bool JugadorDentro => collidersJugador.Count > 0;

    private void Start()
    {
        ActualizarPrompt();
    }

    private void Update()
    {
        ActualizarPrompt();

        if (!JugadorDentro)
            return;

        if (tiendaObjetosUI == null)
            return;

        if (tiendaObjetosUI.EstaAbierta)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            tiendaObjetosUI.AbrirTienda();
            ActualizarPrompt();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!EsJugador(other))
            return;

        collidersJugador.Add(other);
        ActualizarPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!EsJugador(other))
            return;

        collidersJugador.Remove(other);

        if (!JugadorDentro)
        {
            if (promptPresionarE != null)
                promptPresionarE.SetActive(false);

            if (cerrarAlSalirDeLaZona &&
                tiendaObjetosUI != null &&
                tiendaObjetosUI.EstaAbierta)
            {
                tiendaObjetosUI.CerrarTienda();
            }
        }
    }

    private bool EsJugador(Collider other)
    {
        if (other.CompareTag(tagJugador))
            return true;

        Transform raiz = other.transform.root;

        return raiz != null && raiz.CompareTag(tagJugador);
    }

    private void ActualizarPrompt()
    {
        if (promptPresionarE == null)
            return;

        bool debeMostrarse =
            JugadorDentro &&
            tiendaObjetosUI != null &&
            !tiendaObjetosUI.EstaAbierta;

        if (promptPresionarE.activeSelf != debeMostrarse)
            promptPresionarE.SetActive(debeMostrarse);
    }

    private void OnDisable()
    {
        collidersJugador.Clear();

        if (promptPresionarE != null)
            promptPresionarE.SetActive(false);
    }
}