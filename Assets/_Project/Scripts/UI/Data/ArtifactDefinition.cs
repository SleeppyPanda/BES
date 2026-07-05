using UnityEngine;

namespace BES.UI
{
    [CreateAssetMenu(fileName = "ArtifactDefinition", menuName = "BES/Artifact Definition")]
    public class ArtifactDefinition : ScriptableObject
    {
        public string artifactId;
        public string displayName;
        [TextArea] public string description;
        public ItemRarity rarity = ItemRarity.FourStar;
        public int atkBonus;
        public int hpBonus;
        public string setId;
        public int setBonusAtk;
    }
}
