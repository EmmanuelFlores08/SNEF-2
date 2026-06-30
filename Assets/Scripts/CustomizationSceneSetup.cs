using UnityEngine;
using Controller;

public class CustomizationSceneSetup : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private Transform anchor;

    [Header("Juego")]
    [SerializeField] private PlayerCamera playerCamera;

    [Header("Panel")]
    [SerializeField] private CustomizationPanelController panelController;

    [Header("Cámaras de sala")]
    [SerializeField] private RoomCameraManager roomCameraManager;

    [Header("Menú de películas")]
    [SerializeField] private MovieSelectorController movieSelectorController;

    private void Start()
    {
        string avatarId = PlayerPrefs.GetString("selectedAvatarId", "");
        if (string.IsNullOrEmpty(avatarId))
        {
            Debug.LogError("No hay avatar seleccionado en PlayerPrefs.");
            return;
        }

        PlayerCharacterCustomized prefab = characterDatabase.GetPrefab(avatarId);
        if (prefab == null)
        {
            Debug.LogError($"No existe prefab para el avatarId '{avatarId}' en la CharacterDatabase.");
            return;
        }

        PlayerCharacterCustomized character = Instantiate(prefab, anchor);
        character.transform.localPosition = Vector3.zero;
        character.transform.localRotation = Quaternion.identity;

        MovePlayerInput input = character.GetComponent<MovePlayerInput>();

        // Cámara de seguimiento / sistema de cámaras de sala
        if (roomCameraManager != null)
        {
            roomCameraManager.Init(character.transform, input);
        }
        else
        {
            if (input != null && playerCamera != null) input.BindCamera(playerCamera);
            if (playerCamera != null) playerCamera.BindPlayer(character.transform);
        }

        // Panel de personalización
        if (panelController != null)
            panelController.Bind(character, input);

        // Menú de películas: le pasamos el input para que pueda congelar al jugador
        if (movieSelectorController != null && input != null)
            movieSelectorController.BindPlayerInput(input);
    }
}