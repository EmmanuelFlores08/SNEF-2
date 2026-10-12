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

    public event System.Action OnServerOutfitApplied;
    private bool restoreServerOutfit;
    private bool defaultsApplied;
    private int appliedOutfitStateVersion = -1;

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

        if (index < 0)
        {
            rend.currentIndex = -1;
            SetRenderer(rend.skinnedMeshRenderer, null, null);
            if (bodyPartType == CustomizationCatalog.BodyPartType.Hat)
                SetRenderer(hairRenderer, defaultHairMesh, hairMaterials);
            return;
        }

        index = Mathf.Clamp(index, 0, cat.optionArray.Length - 1);
        rend.currentIndex = index;
        ApplyOption(rend, cat, index);
    }

    public int GetCurrentIndex(CustomizationCatalog.BodyPartType bodyPartType)
    {
        BodyPartRenderer rend = GetRenderer(bodyPartType);
        return rend != null ? rend.currentIndex : 0;
    }

    public void EnableServerOutfitRestoration()
    {
        restoreServerOutfit = true;
    }

    public SnefBridge.EquipadosPayload GetCurrentOutfit()
    {
        if (!TryGetEquippedId(CustomizationCatalog.BodyPartType.Hat, out string hat) ||
            !TryGetEquippedId(CustomizationCatalog.BodyPartType.Pants, out string pants) ||
            !TryGetEquippedId(CustomizationCatalog.BodyPartType.Shoes, out string shoes) ||
            !TryGetEquippedId(CustomizationCatalog.BodyPartType.Accesories, out string acce))
            return null;

        return new SnefBridge.EquipadosPayload
        {
            hat = hat, pants = pants, shoes = shoes, acce = acce
        };
    }

    private bool TryGetEquippedId(CustomizationCatalog.BodyPartType type, out string id)
    {
        id = null;
        BodyPartRenderer rend = GetRenderer(type);
        var cat = catalog != null ? catalog.GetCatalog(type) : null;
        if (rend == null || rend.skinnedMeshRenderer == null || cat?.optionArray == null)
            return false;
        if (rend.currentIndex == -1)
            return true;
        if (rend.currentIndex < 0 || rend.currentIndex >= cat.optionArray.Length)
            return false;

        var option = cat.optionArray[rend.currentIndex];
        if (option == null)
            return false;
        // Una opcion sin malla representa "ninguno"; _0 con malla sigue siendo una prenda.
        if (option.mesh == null)
            return true;
        if (string.IsNullOrWhiteSpace(option.optionId))
        {
            Debug.LogWarning($"[SNEF Outfit] Falta optionId en {type}; no se guardara el outfit.", this);
            return false;
        }
        id = option.optionId;
        return true;
    }

    private void Update()
    {
        SnefBridge bridge = SnefBridge.Instance;
        if (!restoreServerOutfit || !defaultsApplied || catalog == null || bridge == null ||
            !bridge.HasServerState || appliedOutfitStateVersion == bridge.OutfitStateVersion ||
            PlayerInventory.Instance == null || !PlayerInventory.Instance.HasServerState)
            return;

        appliedOutfitStateVersion = bridge.OutfitStateVersion;
        SnefBridge.EquipadosPayload outfit = bridge.ConfirmedOutfit;
        if (outfit == null || bridge.IsGuest)
            return;

        ApplyServerBodyPart(CustomizationCatalog.BodyPartType.Hat, outfit.hat);
        ApplyServerBodyPart(CustomizationCatalog.BodyPartType.Pants, outfit.pants);
        ApplyServerBodyPart(CustomizationCatalog.BodyPartType.Shoes, outfit.shoes);
        ApplyServerBodyPart(CustomizationCatalog.BodyPartType.Accesories, outfit.acce);
        OnServerOutfitApplied?.Invoke();
    }

    private void ApplyServerBodyPart(CustomizationCatalog.BodyPartType type, string id)
    {
        var cat = catalog.GetCatalog(type);
        BodyPartRenderer rend = GetRenderer(type);
        if (cat?.optionArray == null || rend == null || rend.skinnedMeshRenderer == null)
        {
            Debug.LogWarning($"[SNEF Outfit] No hay catalogo/renderer para {type}.", this);
            return;
        }

        for (int i = 0; i < cat.optionArray.Length; i++)
        {
            var option = cat.optionArray[i];
            if (option != null && (id == null ? option.mesh == null : option.optionId == id))
            {
                SetBodyPart(type, i);
                return;
            }
        }

        if (id == null)
            SetBodyPart(type, -1);
        else
            Debug.LogWarning($"[SNEF Outfit] '{id}' no existe en el catalogo de {type}; se conserva esa categoria.", this);
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
        if (renderer == null) return;
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
        defaultsApplied = true;
    }
}
