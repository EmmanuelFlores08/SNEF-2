using UnityEngine;
using Controller;

public class RoomCameraManager : MonoBehaviour
{
    [SerializeField] private PlayerCamera followCamera;
    [SerializeField] private MovePlayerInput playerInput;

    private PlayerCamera activeCamera;
    private Transform player;

    // La cámara fija de la zona en la que está parado el jugador (la registra el trigger)
    private PlayerCamera currentZoneCamera;

    public void Init(Transform playerTransform, MovePlayerInput input)
    {
        player = playerTransform;
        playerInput = input;

        activeCamera = followCamera;

        if (followCamera != null)
        {
            followCamera.gameObject.SetActive(true);
            if (player != null) followCamera.BindPlayer(player);
            if (playerInput != null) playerInput.BindCamera(followCamera);
        }
    }

    // Llamado por el trigger al ENTRAR a una zona: solo registra cuál es su cámara
    public void SetZoneCamera(PlayerCamera zoneCamera)
    {
        currentZoneCamera = zoneCamera;
    }

    // Llamado por el trigger al SALIR de una zona
    public void ClearZoneCamera(PlayerCamera zoneCamera)
    {
        if (currentZoneCamera == zoneCamera)
            currentZoneCamera = null;
    }

    // Llamado por el menú de películas al elegir una: activa la cámara fija de la zona actual
    public void ActivateZoneCamera()
    {
        if (currentZoneCamera != null)
            SwitchTo(currentZoneCamera);
    }

    // Vuelve a la cámara de seguimiento (botón "dejar de ver")
    public void ActivateFollowCamera()
    {
        SwitchTo(followCamera);
    }

    private void SwitchTo(PlayerCamera target)
    {
        if (target == null || target == activeCamera) return;

        if (activeCamera != null) activeCamera.gameObject.SetActive(false);
        target.gameObject.SetActive(true);

        activeCamera = target;

        if (player != null) activeCamera.BindPlayer(player);
        if (playerInput != null) playerInput.BindCamera(activeCamera);
    }
}