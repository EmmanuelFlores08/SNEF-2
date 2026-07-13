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

    public System.Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    public int Coins => coins;

    public bool IsOwned(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return ownedItems.Contains(itemId);
    }

    public bool CanAfford(int precio) => coins >= precio;

    public bool TryPurchase(string itemId, int precio)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (IsOwned(itemId)) return false;
        if (!CanAfford(precio)) return false;

        coins -= precio;
        ownedItems.Add(itemId);

        Save();
        OnCoinsChanged?.Invoke(coins);

        return true;
    }

    public void AddCoins(int amount)
    {
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

    private void Save()
    {
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

        OnCoinsChanged?.Invoke(coins);

        Debug.Log("Progreso reseteado: monedas y compras borradas.");
    }
}