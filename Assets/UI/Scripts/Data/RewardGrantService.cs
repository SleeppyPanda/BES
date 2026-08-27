using BES.Core;
using BES.Gameplay;
using BES.UI.Menu;
using UnityEngine;

namespace BES.UI
{
    public static class RewardGrantService
    {
        public static bool Grant(string rewardId, int amount = 1, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || amount <= 0)
                return false;

            rewardId = rewardId.Trim();
            amount = Mathf.Max(1, amount);

            if (IsCoins(rewardId))
            {
                PlayerWallet.Instance?.AddCoins(amount);
                GameManager.Instance?.SaveGame();
                return PlayerWallet.Instance != null;
            }

            if (IsGems(rewardId))
            {
                PlayerWallet.Instance?.AddGems(amount);
                GameManager.Instance?.SaveGame();
                return PlayerWallet.Instance != null;
            }

            if (TryAddMenuCurrency(rewardId, amount))
            {
                GameManager.Instance?.SaveGame();
                return true;
            }

            if (IsCharacterReward(rewardId))
            {
                var characterId = CharacterIdentity.Canonical(rewardId);
                var wasOwned = CharacterOwnership.Owns(characterId);
                CharacterOwnership.Grant(characterId, displayName);
                if (wasOwned)
                {
                    var shards = CharacterDatabaseLoader.Load()?.Get(characterId)?.duplicateShardReward ?? 1;
                    CharacterProgressionState.AddDuplicateShards(characterId, Mathf.Max(1, shards) * amount);
                }
                GameManager.Instance?.SaveGame();
                return true;
            }

            if (TryGrantWeapon(rewardId, amount))
            {
                GameManager.Instance?.SaveGame();
                return true;
            }

            var inventory = GameManager.Instance?.Inventory;
            if (inventory == null)
                return false;

            var granted = inventory.AddItem(rewardId, amount);
            GameManager.Instance?.SaveGame();
            return granted;
        }

        public static bool IsCurrency(string rewardId) =>
            IsCoins(rewardId) || IsGems(rewardId) || IsMenuCurrency(rewardId);

        static bool IsCoins(string rewardId) =>
            rewardId.Equals("coins", System.StringComparison.OrdinalIgnoreCase) ||
            rewardId.Equals("coin", System.StringComparison.OrdinalIgnoreCase) ||
            rewardId.Equals("gold", System.StringComparison.OrdinalIgnoreCase) ||
            rewardId.Equals("vang", System.StringComparison.OrdinalIgnoreCase) ||
            rewardId.Equals("vàng", System.StringComparison.OrdinalIgnoreCase);

        static bool IsGems(string rewardId) =>
            rewardId.Equals("gems", System.StringComparison.OrdinalIgnoreCase) ||
            rewardId.Equals("gem", System.StringComparison.OrdinalIgnoreCase) ||
            rewardId.Equals("crystal", System.StringComparison.OrdinalIgnoreCase) ||
            rewardId.Equals("ruby", System.StringComparison.OrdinalIgnoreCase);

        static bool IsMenuCurrency(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                return false;
            var database = LoadMenuContentDatabase();
            return database?.currencies != null &&
                   database.currencies.Exists(x => x != null && x.id.Equals(rewardId, System.StringComparison.OrdinalIgnoreCase));
        }

        static bool TryAddMenuCurrency(string rewardId, int amount)
        {
            var database = LoadMenuContentDatabase();
            var entry = database?.currencies?.Find(x => x != null && x.id.Equals(rewardId, System.StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                return false;

            entry.amount = Mathf.Max(0, entry.amount + amount);
            return true;
        }

        static bool IsCharacterReward(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                return false;
            var canonical = CharacterIdentity.Canonical(rewardId);
            return !string.IsNullOrEmpty(canonical) && CharacterDatabaseLoader.Load()?.Get(canonical) != null;
        }

        static bool TryGrantWeapon(string rewardId, int amount)
        {
            var weaponDb = Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
#if UNITY_EDITOR
            if (weaponDb == null)
                weaponDb = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponDatabase>("Assets/_Project/Resources/Data/WeaponDatabase.asset");
#endif
            if (weaponDb == null || weaponDb.FindExact(rewardId) == null || EquippedWeaponState.Instance == null)
                return false;

            for (var i = 0; i < amount; i++)
                EquippedWeaponState.Instance.AddWeaponInstance(rewardId);
            return true;
        }

        static MenuContentDatabase LoadMenuContentDatabase()
        {
            var database = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
            if (database == null)
                database = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
            return database;
        }
    }
}
