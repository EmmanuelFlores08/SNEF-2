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
    }

    [System.Serializable]
    public class PhotoKit
    {
        public string kitId;
        public Sprite previewSprite;
        public Sprite backgroundSprite;
        public KitObject[] objects;

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