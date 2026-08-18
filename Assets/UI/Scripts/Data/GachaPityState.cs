using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class GachaPityState : MonoBehaviour
    {
        public const int CharacterPity = 80;
        public const int WeaponPity = 60;
        public const int DuplicateShardReward = 15;

        public static GachaPityState Instance { get; private set; }

        [SerializeField] int pullsSinceLastFiveStar;
        [SerializeField] int pullsSinceLastFiveStarWeapon;
        [SerializeField] int consecutiveOffRates;
        [SerializeField] int stardust;

        public int PullsSinceLastFiveStar => pullsSinceLastFiveStar;
        public int PullsSinceLastFiveStarWeapon => pullsSinceLastFiveStarWeapon;
        public int ConsecutiveOffRates => consecutiveOffRates;
        public int Stardust => stardust;

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

        public void RegisterPull(int rarity, bool isWeapon = false)
        {
            if (isWeapon)
            {
                pullsSinceLastFiveStarWeapon++;
                if (rarity >= 5)
                    pullsSinceLastFiveStarWeapon = 0;
            }
            else
            {
                pullsSinceLastFiveStar++;
                if (rarity >= 5)
                    pullsSinceLastFiveStar = 0;
            }
        }

        public bool ShouldForceFiveStar(bool isWeapon)
        {
            if (isWeapon)
                return pullsSinceLastFiveStarWeapon >= WeaponPity - 1;
            return pullsSinceLastFiveStar >= CharacterPity - 1;
        }

        public void IncrementOffRates() => consecutiveOffRates++;
        public void ResetOffRates() => consecutiveOffRates = 0;

        public void AddStardust(int amount) => stardust = Mathf.Max(0, stardust + amount);

        public void ResetAll()
        {
            pullsSinceLastFiveStar = 0;
            pullsSinceLastFiveStarWeapon = 0;
            consecutiveOffRates = 0;
            stardust = 0;
        }

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;

            data.gachaPullsSinceFiveStar = pullsSinceLastFiveStar;
            data.gachaPullsSinceFiveStarWeapon = pullsSinceLastFiveStarWeapon;
            data.consecutiveOffRates = consecutiveOffRates;
            data.stardust = stardust;
        }

        public void ImportFromSave(SaveData data)
        {
            if (data == null)
                return;

            pullsSinceLastFiveStar = Mathf.Max(0, data.gachaPullsSinceFiveStar);
            pullsSinceLastFiveStarWeapon = Mathf.Max(0, data.gachaPullsSinceFiveStarWeapon);
            consecutiveOffRates = Mathf.Max(0, data.consecutiveOffRates);
            stardust = Mathf.Max(0, data.stardust);
        }
    }
}
