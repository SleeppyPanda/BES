using System.Collections.Generic;
using UnityEngine;

namespace BES.UI
{
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "BES/Weapon Database")]
    public class WeaponDatabase : ScriptableObject
    {
        public List<WeaponDefinition> weapons = new();

        public WeaponDefinition GetById(string id)
        {
            foreach (var w in weapons)
            {
                if (w != null && w.weaponId == id)
                    return w;
            }

            return weapons.Count > 0 ? weapons[0] : null;
        }
    }
}
