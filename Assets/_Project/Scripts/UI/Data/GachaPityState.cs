using BES.Gameplay;
using UnityEngine;

namespace BES.UI
{
    public class GachaPityState : MonoBehaviour
    {
        public const int HardPity = 90;
        public const int DuplicateShardReward = 15;

        public static GachaPityState Instance { get; private set; }

        [SerializeField] int pullsSinceLastFiveStar;
        [SerializeField] int stardust;

        public int PullsSinceLastFiveStar => pullsSinceLastFiveStar;
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

        public void RegisterPull(int rarity)
        {
            pullsSinceLastFiveStar++;
            if (rarity >= 5)
                pullsSinceLastFiveStar = 0;
        }

        public bool ShouldForceFiveStar() => pullsSinceLastFiveStar >= HardPity - 1;

        public void AddStardust(int amount) => stardust = Mathf.Max(0, stardust + amount);

        public void ResetAll()
        {
            pullsSinceLastFiveStar = 0;
            stardust = 0;
        }

        public void ExportToSave(SaveData data)
        {
            if (data == null)
                return;

            data.gachaPullsSinceFiveStar = pullsSinceLastFiveStar;
            data.stardust = stardust;
        }

        public void ImportFromSave(SaveData data)
        {
            if (data == null)
                return;

            pullsSinceLastFiveStar = Mathf.Max(0, data.gachaPullsSinceFiveStar);
            stardust = Mathf.Max(0, data.stardust);
        }
    }
}
