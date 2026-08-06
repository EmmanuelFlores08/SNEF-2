using UnityEngine;

public class PointOfInterestLevitation : MonoBehaviour
{
    [Header("Animación de levitación")]

    [Tooltip("Distancia máxima que subirá y bajará.")]
    [SerializeField, Min(0f)]
    private float altura = 0.2f;

    [Tooltip("Velocidad de la animación.")]
    [SerializeField, Min(0f)]
    private float velocidad = 2f;

    [Tooltip("Permite que varios indicadores no se muevan exactamente al mismo tiempo.")]
    [SerializeField]
    private float desfase = 0f;

    private Vector3 posicionInicial;

    private void OnEnable()
    {
        posicionInicial = transform.localPosition;
    }

    private void Update()
    {
        float desplazamientoY =
            Mathf.Sin((Time.time * velocidad) + desfase) * altura;

        transform.localPosition =
            posicionInicial + Vector3.up * desplazamientoY;
    }

    private void OnDisable()
    {
        transform.localPosition = posicionInicial;
    }
}