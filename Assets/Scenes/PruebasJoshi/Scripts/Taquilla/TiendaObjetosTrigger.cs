using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TiendaObjetosTrigger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TiendaObjetosUI tiendaObjetosUI;

    [Tooltip("La imagen completa que muestra E + Comprar objetos.")]
    [SerializeField] private GameObject promptPresionarE;

    [Tooltip("Botón del mismo prompt. En móvil servirá para abrir la tienda tocándolo.")]
    [SerializeField] private Button botonPrompt;

    [Header("Configuración")]
    [SerializeField] private string tagJugador = "Player";

    [Tooltip("Cierra la interfaz si el jugador sale del área.")]
    [SerializeField] private bool cerrarAlSalirDeLaZona = true;

    private readonly HashSet<Collider> collidersJugador =
        new HashSet<Collider>();

    private bool JugadorDentro => collidersJugador.Count > 0;

    private void Awake()
    {
        // Si no se asignó manualmente, intentamos encontrar
        // el Button automáticamente dentro del prompt.
        if (botonPrompt == null && promptPresionarE != null)
        {
            botonPrompt = promptPresionarE.GetComponent<Button>();

            if (botonPrompt == null)
                botonPrompt = promptPresionarE.GetComponentInChildren<Button>(true);
        }

        if (botonPrompt != null)
            botonPrompt.onClick.AddListener(AbrirDesdePrompt);
    }

    private void Start()
    {
        ActualizarPrompt();
    }

    private void Update()
    {
        ActualizarPrompt();

        if (!PuedeAbrir())
            return;

        // PC / teclado
        if (Input.GetKeyDown(KeyCode.E))
        {
            AbrirInterfaz();
        }
    }

    /// <summary>
    /// Se ejecuta al tocar/presionar el prompt.
    /// Ideal para celular y tablet.
    /// </summary>
    public void AbrirDesdePrompt()
    {
        if (!PuedeAbrir())
            return;

        AbrirInterfaz();
    }

    /// <summary>
    /// Tanto E como el botón terminan llegando aquí.
    /// </summary>
    private void AbrirInterfaz()
    {
        if (tiendaObjetosUI == null)
            return;

        tiendaObjetosUI.AbrirTienda();

        ActualizarPrompt();
    }

    private bool PuedeAbrir()
    {
        if (!JugadorDentro)
            return false;

        if (tiendaObjetosUI == null)
            return false;

        if (tiendaObjetosUI.EstaAbierta)
            return false;

        return true;
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

        if (botonPrompt != null)
            botonPrompt.interactable = debeMostrarse;
    }

    private void OnDisable()
    {
        collidersJugador.Clear();

        if (promptPresionarE != null)
            promptPresionarE.SetActive(false);
    }

    private void OnDestroy()
    {
        if (botonPrompt != null)
            botonPrompt.onClick.RemoveListener(AbrirDesdePrompt);
    }
}