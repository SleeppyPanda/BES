using UnityEngine;

namespace BES.Gameplay
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [SerializeField] int totalKills;
        [SerializeField] int totalExperience;

        public int TotalKills => totalKills;
        public int TotalExperience => totalExperience;

        void Awake()
        {
            Instance = this;
        }

        public void RegisterKill(string enemyId, int experience)
        {
            totalKills++;
            totalExperience += experience;
        }
    }
}
