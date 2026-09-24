using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class SnefBridge : MonoBehaviour
{
    public const string BridgeObjectName = "SNEFBridge";

    public static SnefBridge Instance { get; private set; }

    public bool HasServerState { get; private set; }
    public bool InitialServerStateReceived { get; private set; }
    public bool InitialServerAvatarWasNull { get; private set; }
    public bool IsGuest { get; private set; } = true;
    public int Ditas { get; private set; }
    public string AvatarId { get; private set; }

    public event Action OnStateChanged;
    public event Action<CompraResult> OnCompraResult;
    public event Action<AvatarResult> OnAvatarResult;

    private readonly HashSet<string> comprados = new HashSet<string>();
    private readonly Dictionary<string, Action<CompraResult>> pendingPurchasesByTxId =
        new Dictionary<string, Action<CompraResult>>();
    private readonly Dictionary<string, string> pendingItemsByTxId =
        new Dictionary<string, string>();
    private readonly HashSet<string> pendingPurchaseItems = new HashSet<string>();
    private string pendingAvatarId;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SnefComprar(string itemId, string txId);

    [DllImport("__Internal")]
    private static extern void SnefAvatar(string avatarId);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBridge()
    {
        if (Instance != null)
            return;

        GameObject bridgeObject = new GameObject(BridgeObjectName);
        bridgeObject.AddComponent<SnefBridge>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        gameObject.name = BridgeObjectName;
        DontDestroyOnLoad(gameObject);
    }

    public IReadOnlyCollection<string> Comprados => comprados;

    public bool IsPurchasePending(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) &&
               pendingPurchaseItems.Contains(itemId);
    }

    public bool RequestPurchase(string itemId, Action<CompraResult> callback)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (!HasServerState)
        {
            Debug.LogWarning("[SNEF Bridge] Compra bloqueada: esperando SnefEstado.");
            return false;
        }

        if (IsGuest)
        {
            Debug.LogWarning("[SNEF Bridge] Compra bloqueada: usuario invitado.");
            return false;
        }

        if (pendingPurchaseItems.Contains(itemId))
        {
            Debug.LogWarning($"[SNEF Bridge] Compra ya pendiente para '{itemId}'.");
            return false;
        }

        string txId = Guid.NewGuid().ToString();
        pendingPurchaseItems.Add(itemId);
        pendingPurchasesByTxId[txId] = callback;
        pendingItemsByTxId[txId] = itemId;

        try
        {
#if UNITY_EDITOR
            Debug.Log($"[SNEF Bridge] Editor envio simulado de compra. itemId='{itemId}', txId='{txId}'");
#elif UNITY_WEBGL
            SnefComprar(itemId, txId);
#endif
            return true;
        }
        catch (Exception exception)
        {
            pendingPurchaseItems.Remove(itemId);
            pendingPurchasesByTxId.Remove(txId);
            pendingItemsByTxId.Remove(txId);
            Debug.LogWarning($"[SNEF Bridge] No se pudo solicitar compra '{itemId}'. {exception.Message}");
            return false;
        }
    }

    public void RequestAvatarSave(string avatarId)
    {
        if (string.IsNullOrWhiteSpace(avatarId))
            return;

        if (!HasServerState)
        {
            pendingAvatarId = avatarId;
            Debug.Log($"[SNEF Bridge] Avatar '{avatarId}' pendiente hasta recibir SnefEstado.");
            return;
        }

        if (IsGuest)
        {
            Debug.Log("[SNEF Bridge] Invitado: avatar local, sin persistencia en backend.");
            return;
        }

        try
        {
#if UNITY_EDITOR
            Debug.Log($"[SNEF Bridge] Editor envio simulado de avatar. avatarId='{avatarId}'");
#elif UNITY_WEBGL
            SnefAvatar(avatarId);
#endif
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SNEF Bridge] No se pudo persistir avatar '{avatarId}'. {exception.Message}");
        }
    }

    public void SnefEstado(string json)
    {
        try
        {
            EstadoPayload payload = JsonUtility.FromJson<EstadoPayload>(json);
            if (payload == null)
            {
                Debug.LogWarning("[SNEF Bridge] SnefEstado vacio o invalido.");
                return;
            }

            if (!InitialServerStateReceived)
            {
                InitialServerStateReceived = true;
                InitialServerAvatarWasNull =
                    string.IsNullOrWhiteSpace(payload.avatar);
            }

            HasServerState = true;
            IsGuest = payload.invitado;
            Ditas = Mathf.Max(0, payload.ditas);

            comprados.Clear();
            if (payload.comprados != null)
            {
                foreach (string itemId in payload.comprados)
                {
                    if (!string.IsNullOrWhiteSpace(itemId))
                        comprados.Add(itemId);
                }
            }

            AvatarId = payload.avatar;
            if (!string.IsNullOrWhiteSpace(AvatarId))
            {
                PlayerPrefs.SetString("selectedAvatarId", AvatarId);
                PlayerPrefs.Save();
            }

            ApplyInventoryState();
            FlushPendingAvatar();
            OnStateChanged?.Invoke();

            Debug.Log(
                $"[SNEF Bridge] Estado aplicado. invitado={IsGuest}, ditas={Ditas}, " +
                $"avatar='{AvatarId}', comprados={comprados.Count}"
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SNEF Bridge] Error procesando SnefEstado: {exception.Message}");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("SNEF/Test SnefEstado invitado")]
    private void TestSnefEstadoInvitado()
    {
        const string json =
            "{\"invitado\":true,\"ditas\":250,\"avatar\":null,\"comprados\":[]}";

        SnefEstado(json);
    }

    [ContextMenu("SNEF/Test SnefEstado registrado nuevo")]
    private void TestSnefEstadoRegistradoNuevo()
    {
        const string json =
            "{\"invitado\":false,\"ditas\":250,\"avatar\":null,\"comprados\":[]}";

        SnefEstado(json);
    }

    [ContextMenu("SNEF/Test SnefEstado registrado recurrente")]
    private void TestSnefEstadoRegistradoRecurrente()
    {
        const string json =
            "{\"invitado\":false,\"ditas\":250,\"avatar\":\"avatar_03\",\"comprados\":[]}";

        SnefEstado(json);
    }

    [ContextMenu("SNEF/Test ResultadoCompra OK")]
    private void TestResultadoCompraOk()
    {
        if (pendingPurchasesByTxId.Count == 0)
        {
            Debug.LogWarning("[SNEF Bridge] No hay compras pendientes para completar.");
            return;
        }

        if (pendingPurchasesByTxId.Count > 1)
        {
            Debug.LogWarning(
                $"[SNEF Bridge] Hay {pendingPurchasesByTxId.Count} compras pendientes; el tester requiere exactamente una."
            );
            return;
        }

        string txId = null;
        foreach (string pendingTxId in pendingPurchasesByTxId.Keys)
        {
            txId = pendingTxId;
            break;
        }

        if (string.IsNullOrEmpty(txId) ||
            !pendingItemsByTxId.TryGetValue(txId, out string itemId) ||
            string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogWarning("[SNEF Bridge] No se pudo resolver txId/itemId de la compra pendiente.");
            return;
        }

        if (!TryFindEditorItemPrice(itemId, out int price))
        {
            Debug.LogWarning($"[SNEF Bridge] No se encontro precio de Editor para '{itemId}'.");
            return;
        }

        List<string> updatedComprados = new List<string>(comprados);
        if (!updatedComprados.Contains(itemId))
            updatedComprados.Add(itemId);

        CompraPayload payload = new CompraPayload
        {
            txId = txId,
            ok = true,
            itemId = itemId,
            ditas = Mathf.Max(0, Ditas - price),
            comprados = updatedComprados.ToArray(),
            motivo = null
        };

        ResultadoCompra(JsonUtility.ToJson(payload));
    }

    private bool TryFindEditorItemPrice(string itemId, out int price)
    {
        return TryFindCustomizationItemPrice(itemId, out price) ||
               TryFindPhotoKitPrice(itemId, out price);
    }

    private bool TryFindCustomizationItemPrice(string itemId, out int price)
    {
        price = 0;

        string[] guids = AssetDatabase.FindAssets("t:CustomizationCatalog");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CustomizationCatalog catalog =
                AssetDatabase.LoadAssetAtPath<CustomizationCatalog>(path);

            if (catalog == null || catalog.bodyPartCatalogArray == null)
                continue;

            foreach (CustomizationCatalog.BodyPartCatalog bodyPart in catalog.bodyPartCatalogArray)
            {
                if (bodyPart == null || bodyPart.optionArray == null)
                    continue;

                foreach (CustomizationCatalog.BodyPartOption option in bodyPart.optionArray)
                {
                    if (option != null && option.optionId == itemId)
                    {
                        price = option.precio;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryFindPhotoKitPrice(string itemId, out int price)
    {
        price = 0;

        string[] guids = AssetDatabase.FindAssets("t:PhotoKitCatalog");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PhotoKitCatalog catalog =
                AssetDatabase.LoadAssetAtPath<PhotoKitCatalog>(path);

            if (catalog == null || catalog.kits == null)
                continue;

            foreach (PhotoKitCatalog.PhotoKit kit in catalog.kits)
            {
                if (kit != null && kit.kitId == itemId)
                {
                    price = kit.precio;
                    return true;
                }
            }
        }

        return false;
    }
#endif

    public void ResultadoCompra(string json)
    {
        CompraPayload payload = null;

        try
        {
            payload = JsonUtility.FromJson<CompraPayload>(json);
            if (payload == null)
            {
                Debug.LogWarning("[SNEF Bridge] ResultadoCompra vacio o invalido.");
                return;
            }

            CompraResult result = new CompraResult(
                payload.txId,
                payload.ok,
                payload.itemId,
                payload.ditas,
                payload.comprados,
                payload.motivo
            );

            if (!IsKnownPurchaseTransaction(result.TxId))
            {
                Debug.LogWarning(
                    $"[SNEF Bridge] ResultadoCompra ignorado: txId desconocido o ya procesado '{result.TxId}'."
                );
                return;
            }

            if (payload.ok)
            {
                Ditas = Mathf.Max(0, payload.ditas);
                ReplaceComprados(payload.comprados);
                ApplyInventoryState();
                OnStateChanged?.Invoke();
            }

            InvokePurchaseCallback(result);
            OnCompraResult?.Invoke(result);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SNEF Bridge] Error procesando ResultadoCompra: {exception.Message}");

            if (payload != null && IsKnownPurchaseTransaction(payload.txId))
            {
                CompraResult result = new CompraResult(
                    payload.txId,
                    payload.ok,
                    payload.itemId,
                    payload.ditas,
                    payload.comprados,
                    payload.motivo
                );
                InvokePurchaseCallback(result);
                OnCompraResult?.Invoke(result);
            }
        }
    }

    public void ResultadoAvatar(string json)
    {
        try
        {
            AvatarPayload payload = JsonUtility.FromJson<AvatarPayload>(json);
            if (payload == null)
            {
                Debug.LogWarning("[SNEF Bridge] ResultadoAvatar vacio o invalido.");
                return;
            }

            if (payload.ok && !string.IsNullOrWhiteSpace(payload.avatar))
            {
                AvatarId = payload.avatar;
                PlayerPrefs.SetString("selectedAvatarId", AvatarId);
                PlayerPrefs.Save();
                OnStateChanged?.Invoke();
            }

            OnAvatarResult?.Invoke(new AvatarResult(payload.ok, payload.avatar));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SNEF Bridge] Error procesando ResultadoAvatar: {exception.Message}");
        }
    }

    private void ReplaceComprados(string[] itemIds)
    {
        comprados.Clear();

        if (itemIds == null)
            return;

        foreach (string itemId in itemIds)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
                comprados.Add(itemId);
        }
    }

    private void ApplyInventoryState()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.ApplyServerState(Ditas, comprados);
    }

    private void FlushPendingAvatar()
    {
        if (string.IsNullOrWhiteSpace(pendingAvatarId))
            return;

        string avatarId = pendingAvatarId;
        pendingAvatarId = null;

        if (IsGuest)
        {
            Debug.Log("[SNEF Bridge] Invitado: avatar pendiente queda solo local.");
            return;
        }

        AvatarId = avatarId;
        PlayerPrefs.SetString("selectedAvatarId", AvatarId);
        PlayerPrefs.Save();
        RequestAvatarSave(avatarId);
    }

    private bool IsKnownPurchaseTransaction(string txId)
    {
        return !string.IsNullOrEmpty(txId) &&
               pendingPurchasesByTxId.ContainsKey(txId);
    }

    private void InvokePurchaseCallback(CompraResult result)
    {
        if (result == null)
            return;

        if (!string.IsNullOrEmpty(result.ItemId))
            pendingPurchaseItems.Remove(result.ItemId);

        if (string.IsNullOrEmpty(result.TxId))
            return;

        if (pendingItemsByTxId.TryGetValue(result.TxId, out string pendingItemId))
        {
            pendingPurchaseItems.Remove(pendingItemId);
            pendingItemsByTxId.Remove(result.TxId);
        }

        if (pendingPurchasesByTxId.TryGetValue(result.TxId, out Action<CompraResult> callback))
        {
            pendingPurchasesByTxId.Remove(result.TxId);
            callback?.Invoke(result);
        }
    }

    [Serializable]
    private class EstadoPayload
    {
        public bool invitado;
        public int ditas;
        public string avatar;
        public string[] comprados;
    }

    [Serializable]
    private class CompraPayload
    {
        public string txId;
        public bool ok;
        public string itemId;
        public int ditas;
        public string[] comprados;
        public string motivo;
    }

    [Serializable]
    private class AvatarPayload
    {
        public bool ok;
        public string avatar;
    }

    public class CompraResult
    {
        public string TxId { get; }
        public bool Ok { get; }
        public string ItemId { get; }
        public int Ditas { get; }
        public string Motivo { get; }
        public string[] Comprados { get; }

        public CompraResult(
            string txId,
            bool ok,
            string itemId,
            int ditas,
            string[] comprados,
            string motivo
        )
        {
            TxId = txId;
            Ok = ok;
            ItemId = itemId;
            Ditas = ditas;
            Comprados = comprados;
            Motivo = motivo;
        }
    }

    public class AvatarResult
    {
        public bool Ok { get; }
        public string Avatar { get; }

        public AvatarResult(bool ok, string avatar)
        {
            Ok = ok;
            Avatar = avatar;
        }
    }
}
