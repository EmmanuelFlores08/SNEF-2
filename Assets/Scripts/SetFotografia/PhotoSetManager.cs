using UnityEngine;
using System.Collections.Generic;

public class PhotoSetManager : MonoBehaviour
{
    [Header("Fondo")]
    [SerializeField] private Renderer backgroundQuad; // el Quad con material Unlit

    [Header("Logo del kit")]
    [SerializeField] private Renderer logoQuad; // Quad/Renderer donde se muestra el logo del kit

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

        // Logo del kit
        if (logoQuad != null && kit.logoSprite != null)
        {
            logoQuad.material.mainTexture = kit.logoSprite.texture;
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

                // Aplica el material del kit a todos los objetos (si se asignó).
                if (kit.objectsMaterial != null)
                    AplicarMaterial(instance, kit.objectsMaterial);

                spawnedObjects.Add(instance);
            }
        }
    }

    // Asigna 'material' a todos los renderers del objeto (incluidos submeshes e hijos).
    private void AplicarMaterial(GameObject go, Material material)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = material;
            r.sharedMaterials = mats;
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