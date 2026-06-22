using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterPreviewRotator : MonoBehaviour, IDragHandler
{
    [SerializeField] private CharacterSelector characterSelector;
    [SerializeField] private float rotationSpeed = 0.5f;

    public void OnDrag(PointerEventData eventData){
        if (characterSelector == null) return;

        PlayerCharacterCustomized active = characterSelector.GetActiveCharacter();
        if (active == null) return;

        float deltaX = eventData.delta.x;
        active.transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
    }
}