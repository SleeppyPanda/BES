#if UNITY_EDITOR
using System.IO;
using BES.Core;
using BES.Gameplay;
using BES.Narrative;
using BES.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.Editor
{
    public static class BESUIDataSetup
    {
        public static void EnsureDefaultData()
        {
            BESUIEditorUtils.EnsureFolder("Assets/_Project/Data/UI");
            BESUIEditorUtils.EnsureFolder("Assets/_Project/Resources/Data");

            CreateWeaponDatabase();
            CreateArtifactDatabase();
            CreateGachaBanner();
            CreateEventDefinition();
            BESUIEditorUtils.LoadOrCreateTheme();
            EnsureHudManifest();
            BESUIScreenBackgroundSetup.EnsureManifest();
            AssetDatabase.SaveAssets();
        }

        public static void EnsureHudManifest()
        {
            BESUIEditorUtils.EnsureFolder("Assets/_Project/Resources/Data");
            var path = UIAssetPaths.HudManifestAsset;
            var manifest = AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(path);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<HUDSpriteManifest>();
                AssetDatabase.CreateAsset(manifest, path);
            }

            BESUIHudAutoSuggest.Apply(manifest);
            EditorUtility.SetDirty(manifest);
            var resourcesPath = "Assets/_Project/Resources/Data/HUDSpriteManifest.asset";
            if (AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(resourcesPath) != null)
                AssetDatabase.DeleteAsset(resourcesPath);
            AssetDatabase.CopyAsset(path, resourcesPath);
            AssetDatabase.SaveAssets();
        }

        static void CreateWeaponDatabase()
        {
            var path = "Assets/_Project/Resources/Data/WeaponDatabase.asset";
            if (AssetDatabase.LoadAssetAtPath<WeaponDatabase>(path) != null)
                return;

            var db = ScriptableObject.CreateInstance<WeaponDatabase>();
            AddWeapon(db, CreateWeapon("weapon_iron_sword", "Iron Sword", 120, ItemRarity.ThreeStar));
            AddWeapon(db, CreateWeapon("weapon_flame_blade", "Bane of Flame and Water", 420, ItemRarity.FiveStar));
            AddWeapon(db, CreateWeapon("weapon_void_edge", "Void Edge", 310, ItemRarity.FourStar));
            AssetDatabase.CreateAsset(db, path);
        }

        static void AddWeapon(WeaponDatabase db, WeaponDefinition weapon)
        {
            db.weapons.Add(weapon);
            AssetDatabase.AddObjectToAsset(weapon, db);
        }

        static WeaponDefinition CreateWeapon(string id, string name, int atk, ItemRarity rarity)
        {
            var w = ScriptableObject.CreateInstance<WeaponDefinition>();
            w.weaponId = id;
            w.displayName = name;
            w.baseAtk = atk;
            w.rarity = rarity;
            w.maxLevel = 100;
            w.description = "Placeholder weapon from BES 2.0 mockup.";
            return w;
        }

        static void CreateArtifactDatabase()
        {
            var path = "Assets/_Project/Resources/Data/ArtifactDatabase.asset";
            if (AssetDatabase.LoadAssetAtPath<ArtifactDatabase>(path) != null)
                return;

            var db = ScriptableObject.CreateInstance<ArtifactDatabase>();
            var a = ScriptableObject.CreateInstance<ArtifactDefinition>();
            a.artifactId = "artifact_starter";
            a.displayName = "Starter Relic";
            a.description = "Basic artifact set piece.";
            a.rarity = ItemRarity.FourStar;
            a.atkBonus = 40;
            db.artifacts.Add(a);
            AssetDatabase.AddObjectToAsset(a, db);
            AssetDatabase.CreateAsset(db, path);
        }

        static void CreateGachaBanner()
        {
            PopulateGachaBanner(CreateOrLoadGacha("Assets/_Project/Resources/Data/DefaultGachaBanner.asset"));
            PopulateGachaBanner(CreateOrLoadGacha("Assets/_Project/Data/UI/DefaultGachaBanner.asset"));
        }

        static GachaBannerDefinition CreateOrLoadGacha(string path)
        {
            var banner = AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>(path);
            if (banner != null)
                return banner;

            banner = ScriptableObject.CreateInstance<GachaBannerDefinition>();
            AssetDatabase.CreateAsset(banner, path);
            return banner;
        }

        static void PopulateGachaBanner(GachaBannerDefinition banner)
        {
            if (banner == null)
                return;

            banner.bannerId = "banner_standard";
            banner.displayName = "Character Wish";
            banner.description = "Standard wish banner.";
            banner.singleCostGems = 160;
            banner.tenPullCostGems = 1600;

            if (banner.drops != null && banner.drops.Count > 0)
                return;

            banner.drops = new System.Collections.Generic.List<GachaDropEntry>
            {
                new() { entryId = "w5", rewardType = GachaRewardType.Weapon, rewardId = "weapon_void_blade", rarity = 5, weight = 5, displayLabel = "Void Blade" },
                new() { entryId = "w4", rewardType = GachaRewardType.Weapon, rewardId = "weapon_steel_greatsword", rarity = 4, weight = 25, displayLabel = "Steel Greatsword" },
                new() { entryId = "w3", rewardType = GachaRewardType.Item, rewardId = "material_ore", itemAmount = 5, rarity = 3, weight = 40, displayLabel = "Ore Bundle" },
                new() { entryId = "c5", rewardType = GachaRewardType.Character, rewardId = "char_limited_01", rarity = 5, weight = 3, displayLabel = "Limited Hero" },
                new() { entryId = "c4", rewardType = GachaRewardType.Character, rewardId = "hero_02", rarity = 4, weight = 27, displayLabel = "Ally A" }
            };
            EditorUtility.SetDirty(banner);
        }

        static void CreateEventDefinition()
        {
            CreateResourceAsset<EventDefinition>("Assets/_Project/Resources/Data/DefaultEvent.asset", evt =>
            {
                evt.eventId = "event_daily_checkin";
                evt.displayName = "Daily Check-In";
                evt.description = "Claim daily rewards.";
                evt.totalDays = 7;
                evt.gemsPerDay = 60;
            });
            CreateUiAsset<EventDefinition>("Assets/_Project/Data/UI/DefaultEvent.asset", evt =>
            {
                evt.eventId = "event_daily_checkin";
                evt.displayName = "Daily Check-In";
                evt.description = "Claim daily rewards.";
                evt.totalDays = 7;
                evt.gemsPerDay = 60;
            });
        }

        static void CreateResourceAsset<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
                return;
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
        }

        static void CreateUiAsset<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
                return;
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
#endif
