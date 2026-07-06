using UnityEngine;
using System.Collections.Generic;

public class PhotoSetManager : MonoBehaviour
{
    [Header("Fondo")]
    [SerializeField] private Renderer backgroundQuad; // el Quad con material Unlit

    [Header("Punto donde se colocan los objetos del kit")]
    [SerializeField] private Transform objectsAnchor; // los objetos se instancian relativos a esto

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    public void ApplyKit(PhotoKitCatalog.PhotoKit kit)
    {
        if (kit == null) return;

        ClearCurrentKit();

        // Fondo
        if (backgroundQuad != null && kit.backgroundSprite != null)
        {
            // Usa la textura del sprite en el material del Quad
            backgroundQuad.material.mainTexture = kit.backgroundSprite.texture;
        }

        // Objetos del kit, cada uno en su posición definida
        if (kit.objects != null)
        {
            foreach (var obj in kit.objects)
            {
                if (obj.prefab == null) continue;

                GameObject instance = Instantiate(obj.prefab, objectsAnchor);
                instance.transform.localPosition = obj.localPosition;
                instance.transform.localEulerAngles = obj.localEulerAngles;

                spawnedObjects.Add(instance);
            }
        }
    }

    public void ClearCurrentKit()
    {
        foreach (var go in spawnedObjects)
        {
            if (go != null) Destroy(go);
        }
        spawnedObjects.Clear();
    }
}