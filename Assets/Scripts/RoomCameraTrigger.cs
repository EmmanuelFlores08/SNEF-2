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
        if (!other.CompareTag(playerTag)) return;
        if (cameraManager != null) cameraManager.SetZoneCamera(fixedCamera);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (cameraManager != null) cameraManager.ClearZoneCamera(fixedCamera);
    }
}