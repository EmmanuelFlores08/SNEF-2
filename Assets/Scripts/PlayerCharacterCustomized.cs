using UnityEngine;

public class PlayerCharacterCustomized : MonoBehaviour
{
    [SerializeField] private CustomizationCatalog catalog; // mismo asset para todos

    [Header("Renderers de este personaje, uno por parte")]
    [SerializeField] private BodyPartRenderer[] bodyPartRenderers;

    [Header("Cabello propio de este personaje")]
    [SerializeField] private SkinnedMeshRenderer hairRenderer;
    [SerializeField] private Mesh defaultHairMesh;
    [SerializeField] private Mesh hatHairMesh;
    [SerializeField] private Material[] hairMaterials;

    [System.Serializable]
    public class BodyPartRenderer{
        public CustomizationCatalog.BodyPartType bodyPartType;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        [HideInInspector] public int currentIndex;
    }

    public void ChangeBodyPart(CustomizationCatalog.BodyPartType bodyPartType){
        BodyPartRenderer rend = GetRenderer(bodyPartType);
        var cat = catalog.GetCatalog(bodyPartType);
        if (rend == null || cat == null || cat.optionArray.Length == 0) return;

        rend.currentIndex = (rend.currentIndex + 1) % cat.optionArray.Length;
        ApplyOption(rend, cat, rend.currentIndex);
    }

    // Añade estos métodos públicos a PlayerCharacterCustomized

    public void SetBodyPart(CustomizationCatalog.BodyPartType bodyPartType, int index)
    {
        BodyPartRenderer rend = GetRenderer(bodyPartType);
        var cat = catalog.GetCatalog(bodyPartType);
        if (rend == null || cat == null || cat.optionArray.Length == 0) return;

        index = Mathf.Clamp(index, 0, cat.optionArray.Length - 1);
        rend.currentIndex = index;
        ApplyOption(rend, cat, index);
    }

    public int GetCurrentIndex(CustomizationCatalog.BodyPartType bodyPartType)
    {
        BodyPartRenderer rend = GetRenderer(bodyPartType);
        return rend != null ? rend.currentIndex : 0;
    }
    private void ApplyOption(BodyPartRenderer rend, CustomizationCatalog.BodyPartCatalog cat, int index){
        var option = cat.optionArray[index];
        SetRenderer(rend.skinnedMeshRenderer, option.mesh, option.materials);

        if (rend.bodyPartType == CustomizationCatalog.BodyPartType.Hat){
            ApplyHairOverride(option);
        }
    }

    private void ApplyHairOverride(CustomizationCatalog.BodyPartOption hatOption){
        if (hairRenderer == null) return;

        switch (hatOption.hairOverride){
            case CustomizationCatalog.HairOverrideMode.Hide:
                SetRenderer(hairRenderer, null, null);
                break;
            case CustomizationCatalog.HairOverrideMode.Replace:
                SetRenderer(hairRenderer, hatHairMesh, hairMaterials);
                break;
            default:
                SetRenderer(hairRenderer, defaultHairMesh, hairMaterials);
                break;
        }
    }

    private void SetRenderer(SkinnedMeshRenderer renderer, Mesh mesh, Material[] materials){
        renderer.sharedMesh = mesh;
        renderer.sharedMaterials = (mesh == null || materials == null) ? new Material[0] : materials;
    }

    private BodyPartRenderer GetRenderer(CustomizationCatalog.BodyPartType type){
        foreach (var r in bodyPartRenderers){
            if (r.bodyPartType == type) return r;
        }
        return null;
    }

    private void Start(){
        foreach (var rend in bodyPartRenderers){
            var cat = catalog.GetCatalog(rend.bodyPartType);
            if (cat != null && cat.optionArray.Length > 0){
                ApplyOption(rend, cat, rend.currentIndex);
            }
        }
    }
}