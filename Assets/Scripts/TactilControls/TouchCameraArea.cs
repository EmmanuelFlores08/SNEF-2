using UnityEngine;
using UnityEngine.EventSystems;

public class TouchCameraArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Sensibilidad")]
    [SerializeField] private float sensitivity = 0.1f;

    private Vector2 delta;
    private Vector2 lastPosition;
    private bool dragging;

    public Vector2 Delta => delta;

    public void OnPointerDown(PointerEventData eventData)
    {
        lastPosition = eventData.position;
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 current = eventData.position;
        delta = (current - lastPosition) * sensitivity;
        lastPosition = current;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        delta = Vector2.zero;
    }

    // Se llama cada frame desde el input para consumir el delta
    public Vector2 ConsumeDelta()
    {
        Vector2 d = delta;
        delta = Vector2.zero; // se consume, para que no se acumule
        return d;
    }
}