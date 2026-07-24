using BES.Core;
using BES.Narrative;
using BES.UI;
using UnityEngine;

namespace BES.Gameplay
{
    public class WorldIntegrationManager : MonoBehaviour
    {
        [SerializeField] WorldRegion[] regions;

        void Start()
        {
            regions ??= FindObjectsByType<WorldRegion>(FindObjectsSortMode.None);

            var save = GameManager.Instance?.Save?.Current;
            if (save != null && !string.IsNullOrEmpty(save.currentRegionId))
            {
                foreach (var region in regions)
                {
                    if (region != null && region.RegionId == save.currentRegionId)
                    {
                        var hud = FindAnyObjectByType<HUDController>();
                        hud?.SetRegion(region.RegionName);
                        MetaProgressState.Instance?.DiscoverRegion(region.RegionId);
                        break;
                    }
                }
            }
            else if (regions.Length > 0)
            {
                var hud = FindAnyObjectByType<HUDController>();
                hud?.SetRegion(regions[0].RegionName);
                MetaProgressState.Instance?.DiscoverRegion(regions[0].RegionId);
            }

            if (GetComponent<QuestObjectiveTracker>() == null)
                gameObject.AddComponent<QuestObjectiveTracker>();

            if (GameManager.Instance?.Save?.LoadedFromContinue != true)
                GameManager.Instance?.Quests.StartQuest("side_collect_herbs");

            GrantStarterMaterials();
        }

        static void GrantStarterMaterials()
        {
            var inv = GameManager.Instance?.Inventory;
            if (inv == null)
                return;

            if (inv.GetCount("material_ore") == 0)
                inv.AddItem("material_ore", 20);
            if (inv.GetCount("material_crystal") == 0)
                inv.AddItem("material_crystal", 10);
        }
    }
}
