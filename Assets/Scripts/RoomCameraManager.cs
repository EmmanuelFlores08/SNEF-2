using UnityEngine;
using Controller;

public class RoomCameraManager : MonoBehaviour
{
    [SerializeField] private PlayerCamera followCamera;
    [SerializeField] private MovePlayerInput playerInput;

    private PlayerCamera activeCamera;
    private Transform player;

    public void Init(Transform playerTransform, MovePlayerInput input)
    {
        player = playerTransform;
        playerInput = input;

        // Aseguramos que la cámara de seguimiento sea la activa y quede conectada
        activeCamera = followCamera;

        if (followCamera != null)
        {
            followCamera.gameObject.SetActive(true);
            if (player != null) followCamera.BindPlayer(player);
            if (playerInput != null) playerInput.BindCamera(followCamera);
        }
    }

    public void ActivateFixedCamera(PlayerCamera fixedCamera)
    {
        if (fixedCamera == null) return;
        SwitchTo(fixedCamera);
    }

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

        Debug.Log($"SwitchTo: cámara={target.name} | player={(player != null ? player.name : "NULL")}");
    }
}