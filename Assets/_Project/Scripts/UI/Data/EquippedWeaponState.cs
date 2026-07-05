using System.Collections.Generic;
using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class EquippedWeaponState : MonoBehaviour
    {
        public static EquippedWeaponState Instance { get; private set; }

        [SerializeField] WeaponDatabase database;
        [SerializeField] string equippedWeaponId = "weapon_iron_sword";
        [SerializeField] int level = 1;
        [SerializeField] int refinement = 1;

        readonly HashSet<string> ownedWeaponIds = new();

        public string EquippedWeaponId => equippedWeaponId;
        public int Level => level;
        public int Refinement => refinement;
        public IReadOnlyCollection<string> OwnedWeaponIds => ownedWeaponIds;

        public WeaponDefinition EquippedWeapon =>
            database != null ? database.GetById(equippedWeaponId) : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            database ??= Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
            if (ownedWeaponIds.Count == 0)
                ownedWeaponIds.Add(equippedWeaponId);
        }

        public bool OwnsWeapon(string weaponId) =>
            !string.IsNullOrEmpty(weaponId) && ownedWeaponIds.Contains(weaponId);

        public void UnlockWeapon(string weaponId)
        {
            if (!string.IsNullOrEmpty(weaponId))
                ownedWeaponIds.Add(weaponId);
        }

        public void Equip(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId) || !OwnsWeapon(weaponId))
                return;
            equippedWeaponId = weaponId;
            RefreshPlayerBuild();
        }

        public void Unequip()
        {
            equippedWeaponId = string.Empty;
        }

        public void SetLevel(int newLevel) => level = Mathf.Max(1, newLevel);
        public void SetRefinement(int newRefine) => refinement = Mathf.Max(1, newRefine);
        public void EnhanceLevel(int delta = 1)
        {
            level = Mathf.Min(100, level + delta);
            RefreshPlayerBuild();
        }
        public void EnhanceRefinement(int delta = 1)
        {
            refinement = Mathf.Min(5, refinement + delta);
            RefreshPlayerBuild();
        }

        static void RefreshPlayerBuild()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent<PlayerBuildStats>(out var build))
                build.Refresh();
        }

        public int GetDisplayAtk()
        {
            var w = EquippedWeapon;
            if (w == null)
                return 15;
            return w.baseAtk + (level - 1) * 8 + refinement * 12;
        }

        public void ResetToDefaults()
        {
            equippedWeaponId = "weapon_iron_sword";
            level = 1;
            refinement = 1;
            ownedWeaponIds.Clear();
            ownedWeaponIds.Add(equippedWeaponId);
        }

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;

            data.equippedWeaponId = equippedWeaponId;
            data.weaponLevel = level;
            data.weaponRefinement = refinement;
            data.ownedWeaponIds = new List<string>(ownedWeaponIds);
        }

        public void ImportFromSave(SaveData data)
        {
            if (data == null)
                return;

            ownedWeaponIds.Clear();
            if (data.ownedWeaponIds != null && data.ownedWeaponIds.Count > 0)
            {
                foreach (var id in data.ownedWeaponIds)
                    ownedWeaponIds.Add(id);
            }
            else
            {
                ownedWeaponIds.Add("weapon_iron_sword");
            }

            equippedWeaponId = string.IsNullOrEmpty(data.equippedWeaponId)
                ? "weapon_iron_sword"
                : data.equippedWeaponId;
            level = Mathf.Max(1, data.weaponLevel);
            refinement = Mathf.Max(1, data.weaponRefinement);
        }
    }
}
