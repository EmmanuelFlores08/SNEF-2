using UnityEngine;

[CreateAssetMenu(fileName = "CustomizationCatalog", menuName = "Customization/Catalog")]
public class CustomizationCatalog : ScriptableObject
{
    public enum BodyPartType { Hat, Pants, Shoes, Accesories }
    public enum HairOverrideMode { None, Hide, Replace }

    [System.Serializable]
    public class BodyPartOption
    {
        public string optionId;       // ID único, ej: "hat_01"
        public Sprite previewSprite;
        public Mesh mesh;
        public Material[] materials;

        [Header("Tienda")]
        public int precio = 0;
        public bool gratuito = false; // true = desbloqueado desde el inicio

        [Header("Solo para sombreros")]
        public HairOverrideMode hairOverride = HairOverrideMode.None;
    }

    [System.Serializable]
    public class BodyPartCatalog
    {
        public BodyPartType bodyPartType;
        public BodyPartOption[] optionArray;
    }

    public BodyPartCatalog[] bodyPartCatalogArray;

    public BodyPartCatalog GetCatalog(BodyPartType type)
    {
        foreach (var c in bodyPartCatalogArray)
            if (c.bodyPartType == type) return c;
        return null;
    }
}