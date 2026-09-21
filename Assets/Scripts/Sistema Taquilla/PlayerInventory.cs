using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private const string KEY_COINS = "player_coins";
    private const string KEY_OWNED = "player_owned_items";

    [SerializeField] private int monedasIniciales = 500;

    private HashSet<string> ownedItems = new HashSet<string>();
    private int coins;
    private bool hasServerState;

    public System.Action<int> OnCoinsChanged;
    public System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (ShouldWaitForServerState())
        {
            coins = 0;
            ownedItems.Clear();
        }
        else
        {
            Load();
        }

        if (SnefBridge.Instance != null && SnefBridge.Instance.HasServerState)
            ApplyServerState(SnefBridge.Instance.Ditas, SnefBridge.Instance.Comprados);
    }

    public int Coins => coins;
    public bool HasServerState => hasServerState;
    public bool IsWaitingForServerState => ShouldWaitForServerState();

    public bool IsOwned(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return ownedItems.Contains(itemId);
    }

    public bool CanAfford(int precio) => !ShouldWaitForServerState() && coins >= precio;

    public bool TryPurchase(string itemId, int precio)
    {
        if (hasServerState || ShouldWaitForServerState())
        {
            Debug.LogWarning(
                "PlayerInventory.TryPurchase bloqueado: el saldo servidor es la fuente de verdad."
            );
            return false;
        }

        if (string.IsNullOrEmpty(itemId)) return false;
        if (IsOwned(itemId)) return false;
        if (!CanAfford(precio)) return false;

        coins -= precio;
        ownedItems.Add(itemId);

        Save();
        OnCoinsChanged?.Invoke(coins);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public void AddCoins(int amount)
    {
        if (hasServerState || ShouldWaitForServerState())
        {
            Debug.LogWarning(
                "PlayerInventory.AddCoins bloqueado: el saldo servidor es la fuente de verdad."
            );
            return;
        }

        coins += amount;
        Save();
        OnCoinsChanged?.Invoke(coins);
    }

    // ---- Persistencia: solo estos dos métodos tocan el almacenamiento.
    // ---- Cuando migres a base de datos, cambias SOLO esto.

    private void Load()
    {
        coins = PlayerPrefs.GetInt(KEY_COINS, monedasIniciales);

        string owned = PlayerPrefs.GetString(KEY_OWNED, "");
        ownedItems.Clear();

        if (!string.IsNullOrEmpty(owned))
        {
            foreach (string id in owned.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    ownedItems.Add(id);
            }
        }
    }

    public void ApplyServerState(int ditas, IEnumerable<string> comprados)
    {
        hasServerState = true;
        coins = Mathf.Max(0, ditas);

        ownedItems.Clear();
        if (comprados != null)
        {
            foreach (string itemId in comprados)
            {
                if (!string.IsNullOrEmpty(itemId))
                    ownedItems.Add(itemId);
            }
        }

        OnCoinsChanged?.Invoke(coins);
        OnInventoryChanged?.Invoke();
    }

    private void Save()
    {
        if (hasServerState || ShouldWaitForServerState())
            return;

        PlayerPrefs.SetInt(KEY_COINS, coins);
        PlayerPrefs.SetString(KEY_OWNED, string.Join(",", ownedItems));
        PlayerPrefs.Save();
    }

    [ContextMenu("Resetear progreso")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.DeleteKey(KEY_OWNED);
        PlayerPrefs.Save();

        ownedItems.Clear();
        coins = monedasIniciales;
        hasServerState = false;

        OnCoinsChanged?.Invoke(coins);
        OnInventoryChanged?.Invoke();

        Debug.Log("Progreso reseteado: monedas y compras borradas.");
    }

    private bool ShouldWaitForServerState()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return SnefBridge.Instance == null || !SnefBridge.Instance.HasServerState;
#else
        return false;
#endif
    }
}
