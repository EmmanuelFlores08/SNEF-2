using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Partes del joystick")]
    [SerializeField] private RectTransform background; // el círculo base
    [SerializeField] private RectTransform handle;     // el círculo que mueves

    [Header("Configuración")]
    [SerializeField] private float handleRange = 80f;  // qué tan lejos llega el handle

    private Vector2 inputVector = Vector2.zero;
    private Canvas canvas;

    public Vector2 Input => inputVector; // dirección actual (-1 a 1)

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();

        // Empieza oculto; aparece al tocar
        if (background != null) background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Coloca el joystick donde tocaste
        if (background != null)
        {
            background.gameObject.SetActive(true);

            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, eventData.position, eventData.pressEventCamera, out pos);

            background.anchoredPosition = pos;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out pos);

        // Limita el handle al rango
        Vector2 clamped = Vector2.ClampMagnitude(pos, handleRange);
        if (handle != null) handle.anchoredPosition = clamped;

        // Normaliza a -1..1
        inputVector = clamped / handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
    }
}