using UnityEngine;
using Controller;

[RequireComponent(typeof(Collider))]
public class RoomCameraTrigger : MonoBehaviour
{
    [SerializeField] private RoomCameraManager cameraManager;
    [SerializeField] private PlayerCamera fixedCamera;   // la cámara fija de ESTA zona
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Algo entró al trigger: {other.name} | tag: {other.tag}");
        if (!other.CompareTag(playerTag)) return;
        Debug.Log("Es el player, cambiando a cámara fija");
        if (cameraManager != null) cameraManager.ActivateFixedCamera(fixedCamera);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Algo salió del trigger: {other.name}");
        if (!other.CompareTag(playerTag)) return;
        if (cameraManager != null) cameraManager.ActivateFollowCamera();
    }
}