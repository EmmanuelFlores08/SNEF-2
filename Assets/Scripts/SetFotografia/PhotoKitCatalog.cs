using UnityEngine;

[CreateAssetMenu(fileName = "PhotoKitCatalog", menuName = "Photo/Kit Catalog")]
public class PhotoKitCatalog : ScriptableObject
{
    [System.Serializable]
    public class KitObject
    {
        public GameObject prefab;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;

        [Header("Tienda")]
        public Sprite previewSprite; // imagen del objeto para mostrar en la tienda
    }

    [System.Serializable]
    public class PhotoKit
    {
        public string kitId;
        public Sprite previewSprite;
        public Sprite backgroundSprite;
        public KitObject[] objects;

        [Header("Personalización del set")]
        [Tooltip("Material que se aplica a TODOS los objetos del kit al colocarlos en el set.")]
        public Material objectsMaterial;

        [Tooltip("Logo del kit (se muestra al seleccionar/usar el kit).")]
        public Sprite logoSprite;

        [Header("Tienda")]
        public int precio = 0;
        public bool gratuito = false;
    }

    public PhotoKit[] kits;

    public PhotoKit GetKit(int index)
    {
        if (index < 0 || index >= kits.Length) return null;
        return kits[index];
    }
}