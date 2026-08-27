using System;
using BES.Core;
using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance { get; private set; }

        [SerializeField] int coins = 99999;
        [SerializeField] int gems = 1600;

        public int Coins => coins;
        public int Gems => gems;

        public event Action WalletChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool TrySpendGems(int amount)
        {
            if (gems < amount)
                return false;

            gems -= amount;
            SyncMenuCurrency("gems", gems);
            WalletChanged?.Invoke();
            GameManager.Instance?.SaveGame();
            return true;
        }

        public bool TrySpendCoins(int amount)
        {
            if (coins < amount)
                return false;

            coins -= amount;
            SyncMenuCurrency("coins", coins);
            WalletChanged?.Invoke();
            GameManager.Instance?.SaveGame();
            return true;
        }

        public void AddGems(int amount)
        {
            gems += amount;
            SyncMenuCurrency("gems", gems);
            WalletChanged?.Invoke();
            GameManager.Instance?.SaveGame();
        }

        public void AddCoins(int amount)
        {
            coins += amount;
            SyncMenuCurrency("coins", coins);
            WalletChanged?.Invoke();
            GameManager.Instance?.SaveGame();
        }

        public void LoadDefaults()
        {
            coins = 99999;
            gems = 1600;
            SyncMenuCurrency("coins", coins);
            SyncMenuCurrency("gems", gems);
            WalletChanged?.Invoke();
        }

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;
            data.coins = coins;
            data.gems = gems;
        }

        public void ImportFromSave(SaveData data)
        {
            if (data == null)
                return;
            coins = data.coins > 0 ? data.coins : 99999;
            gems = data.gems > 0 ? data.gems : 1600;
            SyncMenuCurrency("coins", coins);
            SyncMenuCurrency("gems", gems);
            WalletChanged?.Invoke();
        }

        static void SyncMenuCurrency(string currencyId, int value)
        {
            var database = Resources.Load<Menu.MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
            if (database == null)
                database = UnityEditor.AssetDatabase.LoadAssetAtPath<Menu.MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            var entry = database?.currencies?.Find(x => x != null && x.id == currencyId);
            if (entry != null)
                entry.amount = Mathf.Max(0, value);
        }
    }
}
