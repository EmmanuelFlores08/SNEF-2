using UnityEngine;

[CreateAssetMenu(fileName = "PhotoKitCatalog", menuName = "Photo/Kit Catalog")]
public class PhotoKitCatalog : ScriptableObject
{
    [System.Serializable]
    public class KitObject
    {
        public GameObject prefab;        // objeto 3D del kit
        public Vector3 localPosition;    // dónde va, relativo al set
        public Vector3 localEulerAngles; // rotación
    }

    [System.Serializable]
    public class PhotoKit
    {
        public string kitId;
        public Sprite previewSprite;     // imagen del recuadro en el menú
        public Sprite backgroundSprite;  // el fondo 2D del set
        public KitObject[] objects;      // los 2 objetos (o los que quieras)
    }

    public PhotoKit[] kits;

    public PhotoKit GetKit(int index)
    {
        if (index < 0 || index >= kits.Length) return null;
        return kits[index];
    }
}