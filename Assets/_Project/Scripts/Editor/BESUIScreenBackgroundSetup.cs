#if UNITY_EDITOR
using BES.UI;
using UnityEditor;
using UnityEngine;

namespace BES.Editor
{
    public static class BESUIScreenBackgroundSetup
    {
        public static void EnsureManifest()
        {
            BESUIEditorUtils.EnsureFolder("Assets/_Project/Data/UI");
            BESUIEditorUtils.EnsureFolder("Assets/_Project/Resources/Data");

            var path = "Assets/_Project/Data/UI/UIScreenBackgroundManifest.asset";
            var manifest = AssetDatabase.LoadAssetAtPath<UIScreenBackgroundManifest>(path);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<UIScreenBackgroundManifest>();
                AssetDatabase.CreateAsset(manifest, path);
            }

            ApplySprites(manifest);
            EditorUtility.SetDirty(manifest);

            var resourcesPath = "Assets/_Project/Resources/Data/UIScreenBackgroundManifest.asset";
            if (AssetDatabase.LoadAssetAtPath<UIScreenBackgroundManifest>(resourcesPath) != null)
                AssetDatabase.DeleteAsset(resourcesPath);
            AssetDatabase.CopyAsset(path, resourcesPath);
            AssetDatabase.SaveAssets();
        }

        static void ApplySprites(UIScreenBackgroundManifest manifest)
        {
            manifest.mainMenu = Load(UIAssetPaths.BgStart);
            manifest.gameplayHud = Load(UIAssetPaths.BgMainPlay);
            manifest.inventory = Load(UIAssetPaths.BgInventory);
            manifest.characterProfile = Load(UIAssetPaths.BgCharacterProfile);
            manifest.characterPreview = Load(UIAssetPaths.BgSoonviewCharacter);
            manifest.worldMap = Load(UIAssetPaths.BgEventScene);
            manifest.weapon = Load(UIAssetPaths.BgWeaponInfo) ?? Load(UIAssetPaths.BgWeapon);
            manifest.weaponEnhance = Load(UIAssetPaths.BgWeaponEnhance) ?? Load(UIAssetPaths.BgWeaponEnhanceAlt);
            manifest.weaponRankUp = Load(UIAssetPaths.BgWeaponRankUp);
            manifest.weaponRefine = Load(UIAssetPaths.BgWeaponRefine);
            manifest.artifacts = Load(UIAssetPaths.BgArtifacts);
            manifest.teamSetup = Load(UIAssetPaths.BgTeamSetup);
            manifest.teamSlotPicker = Load(UIAssetPaths.BgTeamSlotPicker) ?? manifest.characterPreview;
            manifest.eventCheckIn = Load(UIAssetPaths.BgEventCheckIn);
            manifest.wish = Load(UIAssetPaths.BgWish);
            manifest.dialogue = Load(UIAssetPaths.BgInteraction);
            manifest.loading = Load(UIAssetPaths.BgLoading) ?? Load(UIAssetPaths.BgLoadingDots);
            manifest.playerProfile = Load(UIAssetPaths.BgUsernamePlayer) ?? Load(UIAssetPaths.BgPersonal);
            manifest.questTracker = Load(UIAssetPaths.BgMission);

            var missing = 0;
            if (manifest.mainMenu == null) missing++;
            if (manifest.gameplayHud == null) missing++;
            if (manifest.inventory == null) missing++;
            if (manifest.wish == null) missing++;
            if (manifest.weapon == null) missing++;
            if (manifest.teamSetup == null) missing++;

            if (missing > 0)
                Debug.LogWarning($"[BES] UIScreenBackgroundManifest: {missing} màn hình chưa có sprite. Chạy Import UI assets.");
            else
                Debug.Log("[BES] UIScreenBackgroundManifest: đã map mockup cho tất cả màn hình chính.");
        }

        static Sprite Load(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}
#endif
