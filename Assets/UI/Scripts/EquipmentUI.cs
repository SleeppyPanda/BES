using UnityEngine;
using TMPro;

namespace BES.UI
{
    public class EquipmentUI : MonoBehaviour
    {
        [SerializeField] TMP_Text weaponNameText;
        [SerializeField] TMP_Text weaponAtkText;
        [SerializeField] WeaponDatabase database;

        void Awake()
        {
            database ??= Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
        }

        public void Refresh()
        {
            var equipped = EquippedWeaponState.Instance;
            var weapon = equipped?.EquippedWeapon ?? database?.GetById("weapon_iron_sword");

            if (weaponNameText != null)
                weaponNameText.text = weapon != null ? weapon.displayName : "Iron Sword";

            if (weaponAtkText != null)
                weaponAtkText.text = $"ATK: {(equipped != null ? equipped.GetDisplayAtk() : 15):0}";
        }

        void OnEnable() => Refresh();
    }
}
