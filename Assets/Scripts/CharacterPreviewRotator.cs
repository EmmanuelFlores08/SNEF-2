using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterPreviewRotator : MonoBehaviour, IDragHandler
{
    [SerializeField] private float rotationSpeed = 0.5f;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null) return;
        float deltaX = eventData.delta.x;
        target.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
    }
}