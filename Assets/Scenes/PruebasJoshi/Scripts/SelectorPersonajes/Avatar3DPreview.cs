using System.Collections.Generic;
using UnityEngine;
using Controller;

/// <summary>
/// Muestra el modelo 3D del avatar seleccionado en un "escenario" de preview
/// (frente a una cámara que renderiza a un RenderTexture, como la taquilla de Cine).
/// Reutiliza los prefabs de la CharacterDatabase e instancia cada avatar una sola vez.
/// </summary>
public class Avatar3DPreview : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private CharacterDatabase database;

    [Header("Escenario de preview")]
    [Tooltip("Punto donde se colocan los modelos de preview, frente a la cámara de preview.")]
    [SerializeField] private Transform previewAnchor;

    [Header("Rotación (opcional)")]
    [Tooltip("Si se asigna, el modelo actual se puede rotar arrastrando (igual que la taquilla).")]
    [SerializeField] private CharacterPreviewRotator previewRotator;

    [Header("Opciones")]
    [Tooltip("Desactiva movimiento/control/CharacterController en el modelo de preview.")]
    [SerializeField] private bool disableGameplayComponents = true;

    [Tooltip("Opcional: capa a la que se mueven los modelos, para que SOLO los vea la cámara de preview. Déjalo vacío si no usas una capa dedicada.")]
    [SerializeField] private string previewLayerName = "";

    // Un modelo instanciado por avatarId, para reutilizar y no re-instanciar en cada clic.
    private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();
    private GameObject currentInstance;

    /// <summary>
    /// Muestra el modelo 3D del avatar indicado, ocultando el anterior.
    /// </summary>
    public void Show(string avatarId)
    {
        if (database == null || previewAnchor == null || string.IsNullOrEmpty(avatarId))
            return;

        // Oculta el modelo que estaba visible.
        if (currentInstance != null)
            currentInstance.SetActive(false);

        // ¿Ya lo instanciamos antes? Reutilízalo.
        if (instances.TryGetValue(avatarId, out GameObject existing) && existing != null)
        {
            existing.SetActive(true);
            currentInstance = existing;
            BindRotator();
            return;
        }

        // Instancia el prefab 3D del avatar por primera vez.
        PlayerCharacterCustomized prefab = database.GetPrefab(avatarId);
        if (prefab == null)
        {
            Debug.LogWarning($"Avatar3DPreview: no existe prefab para el avatarId '{avatarId}'.");
            return;
        }

        PlayerCharacterCustomized model = Instantiate(prefab, previewAnchor);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = prefab.transform.localScale;

        if (disableGameplayComponents)
            DisableGameplay(model.gameObject);

        ApplyPreviewLayer(model.gameObject);

        instances[avatarId] = model.gameObject;
        currentInstance = model.gameObject;

        BindRotator();
    }

    private void BindRotator()
    {
        if (previewRotator != null && currentInstance != null)
            previewRotator.SetTarget(currentInstance.transform);
    }

    // Evita que el modelo de preview se mueva, capture input o bloquee el cursor;
    // el Animator sigue activo (idle). Se desactivan ANTES de que corra su Start().
    private void DisableGameplay(GameObject go)
    {
        CharacterController cc = go.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        CharacterMover mover = go.GetComponent<CharacterMover>();
        if (mover != null) mover.enabled = false;

        MovePlayerInput input = go.GetComponent<MovePlayerInput>();
        if (input != null) input.enabled = false;

        // El prefab trae un CursorLockManager que, al iniciar, oculta/bloquea el cursor.
        // En el selector el cursor debe estar SIEMPRE visible, así que lo apagamos.
        CursorLockManager cursorLock = go.GetComponent<CursorLockManager>();
        if (cursorLock != null) cursorLock.enabled = false;

        // Evita un segundo AudioListener en la escena (warning de Unity).
        AudioListener listener = go.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = false;
    }

    private void ApplyPreviewLayer(GameObject go)
    {
        if (string.IsNullOrEmpty(previewLayerName))
            return;

        int layer = LayerMask.NameToLayer(previewLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"Avatar3DPreview: la capa '{previewLayerName}' no existe. Créala en Tags & Layers.");
            return;
        }

        SetLayerRecursive(go.transform, layer);
    }

    private void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform child in t)
            SetLayerRecursive(child, layer);
    }
}
