using UnityEngine;
using Controller;

public class CustomizationSceneSetup : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private Transform anchor;

    [Header("Juego")]
    [SerializeField] private PlayerCamera playerCamera;

    [Header("Panel (escena de Cine)")]
    [SerializeField] private CustomizationPanelController panelController;

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
        character.transform.localRotation = Quaternion.Euler(0, 180, 0);

        // Cámara
        MovePlayerInput input = character.GetComponent<MovePlayerInput>();
        if (input != null && playerCamera != null) input.BindCamera(playerCamera);
        if (playerCamera != null) playerCamera.BindPlayer(character.transform);

        // Panel de personalización: le pasamos el personaje instanciado
        if (panelController != null)
        {
            panelController.Bind(character, input);
        }
    }
}