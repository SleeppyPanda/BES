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
            CreateCharacterDatabase();
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
            var db = CreateOrLoadAsset<WeaponDatabase>(path);
            db.weapons ??= new System.Collections.Generic.List<WeaponDefinition>();
            db.weapons.RemoveAll(weapon => weapon == null || string.IsNullOrEmpty(weapon.weaponId));

            UpsertWeapon(db, "weapon_iron_sword", "Iron Sword", 120, ItemRarity.ThreeStar);
            UpsertWeapon(db, "weapon_void_edge", "Void Edge", 310, ItemRarity.FourStar);
            UpsertWeapon(db, "weapon_flame_blade", "Bane of Flame and Water", 420, ItemRarity.FiveStar);
            EditorUtility.SetDirty(db);
        }

        static void UpsertWeapon(WeaponDatabase db, string id, string displayName, int atk, ItemRarity rarity)
        {
            var weapon = db.weapons.Find(candidate => candidate != null && candidate.weaponId == id);
            if (weapon == null)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
                weapon.name = id;
                AssetDatabase.AddObjectToAsset(weapon, db);
                db.weapons.Add(weapon);
            }

            weapon.weaponId = id;
            weapon.displayName = displayName;
            weapon.baseAtk = atk;
            weapon.rarity = rarity;
            weapon.maxLevel = 100;
            weapon.description = "Default weapon for the BES MVP.";
            EditorUtility.SetDirty(weapon);
        }

        static void CreateArtifactDatabase()
        {
            var path = "Assets/_Project/Resources/Data/ArtifactDatabase.asset";
            var db = CreateOrLoadAsset<ArtifactDatabase>(path);
            db.artifacts ??= new System.Collections.Generic.List<ArtifactDefinition>();
            db.artifacts.RemoveAll(artifact => artifact == null || string.IsNullOrEmpty(artifact.artifactId));

            var artifact = db.artifacts.Find(candidate => candidate != null && candidate.artifactId == "artifact_starter");
            if (artifact == null)
            {
                artifact = ScriptableObject.CreateInstance<ArtifactDefinition>();
                artifact.name = "artifact_starter";
                AssetDatabase.AddObjectToAsset(artifact, db);
                db.artifacts.Add(artifact);
            }

            artifact.artifactId = "artifact_starter";
            artifact.displayName = "Starter Relic";
            artifact.description = "Basic artifact set piece.";
            artifact.rarity = ItemRarity.FourStar;
            artifact.atkBonus = 40;
            EditorUtility.SetDirty(artifact);
            EditorUtility.SetDirty(db);
        }

        static void CreateCharacterDatabase()
        {
            var db = CreateOrLoadAsset<CharacterDatabase>("Assets/_Project/Resources/Data/CharacterDatabase.asset");
            db.ResetToDefaultEntries();
            EditorUtility.SetDirty(db);
        }

        static void CreateGachaBanner()
        {
            PopulateGachaBanner(CreateOrLoadGacha("Assets/_Project/Resources/Data/DefaultGachaBanner.asset"));
            PopulateGachaBanner(CreateOrLoadGacha("Assets/_Project/Data/UI/DefaultGachaBanner.asset"));
        }

        static GachaBannerDefinition CreateOrLoadGacha(string path)
        {
            return CreateOrLoadAsset<GachaBannerDefinition>(path);
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

            banner.drops = new System.Collections.Generic.List<GachaDropEntry>
            {
                new() { entryId = "w5", rewardType = GachaRewardType.Weapon, rewardId = "weapon_flame_blade", rarity = 5, weight = 5, displayLabel = "Bane of Flame and Water" },
                new() { entryId = "w4", rewardType = GachaRewardType.Weapon, rewardId = "weapon_void_edge", rarity = 4, weight = 25, displayLabel = "Void Edge" },
                new() { entryId = "w3", rewardType = GachaRewardType.Item, rewardId = "material_ore", itemAmount = 5, rarity = 3, weight = 40, displayLabel = "Ore Bundle" },
                new() { entryId = "c5", rewardType = GachaRewardType.Character, rewardId = "char_limited_01", rarity = 5, weight = 3, displayLabel = "Limited Hero" },
                new() { entryId = "c4", rewardType = GachaRewardType.Character, rewardId = "hero_02", rarity = 4, weight = 27, displayLabel = "Mất cô ấy rồi" }
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
            var asset = CreateOrLoadAsset<T>(path);
            init(asset);
            EditorUtility.SetDirty(asset);
        }

        static void CreateUiAsset<T>(string path, System.Action<T> init) where T : ScriptableObject
        {
            var asset = CreateOrLoadAsset<T>(path);
            init(asset);
            EditorUtility.SetDirty(asset);
        }

        static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null || File.Exists(path))
                AssetDatabase.DeleteAsset(path);

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
