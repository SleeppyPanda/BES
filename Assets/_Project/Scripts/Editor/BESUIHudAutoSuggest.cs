#if UNITY_EDITOR
using BES.UI;
using UnityEditor;
using UnityEngine;

namespace BES.Editor
{
    public static class BESUIHudAutoSuggest
    {
        public static void Apply(HUDSpriteManifest manifest)
        {
            if (manifest == null)
                return;

            manifest.minimapFrame = null;
            manifest.minimapMap = null;
            manifest.minimapRing = LoadFrame("Union.png");
            manifest.questTrackerFrame = null;
            manifest.navBarBackground = null;

            manifest.playerDot = LoadIcon("Star 11.png");
            manifest.objectiveDot = LoadIcon("Star 14.png");
            manifest.lockIcon = LoadIcon("Vector.png");
            manifest.lockBtnFrame = LoadFrame("Rectangle 39782.png");
            manifest.chatBubbleIcon = LoadIcon("Vector-1.png");
            manifest.chatEnterFrame = LoadFrame("Rectangle 39781.png");
            manifest.portraitChipRing = LoadFrame("Ellipse 3042.png");

            manifest.hpBarBackground = null;
            manifest.staminaBarBackground = null;
            manifest.manaBarBackground = null;
            manifest.hpBarFill = null;
            manifest.staminaBarFill = null;
            manifest.manaBarFill = null;

            manifest.navEvent = LoadIcon("Star 46.png");
            manifest.navMap = LoadIcon("Object-1.png");
            manifest.navWish = LoadIcon("Star 47.png");
            manifest.navTeam = LoadIcon("Object-2.png");
            manifest.navInventory = LoadIcon("Object.png");
            manifest.navCharacter = LoadIcon("Mask group.png");
            manifest.navBattlePass = manifest.navTeam;
            manifest.navBag = manifest.navInventory;
            manifest.navPersonal = manifest.navCharacter;
            manifest.navArtifacts = LoadIcon("Object-4.png");
            manifest.navWeapon = LoadIcon("Object-5.png");
            manifest.settingsIcon = LoadIcon("Vector.png");
            manifest.guideLineIcon = LoadIcon("Object-1.png");
            manifest.missionIcon = LoadIcon("Object-4.png");

            manifest.partySlotFrame = LoadFrame("Rectangle 39782.png");
            manifest.skillSlotFrame = LoadFrame("Rectangle 40003.png") ?? manifest.partySlotFrame;
            manifest.partyPortraitDefault = LoadCommon("image 306.png");

            manifest.skillIconAttack = LoadIcon("Vector-2.png") ?? LoadIcon("Vector.png");
            manifest.skillIconSkill1 = LoadIcon("Star 46.png");
            manifest.skillIconSkill2 = LoadIcon("Star 47.png");
            manifest.skillIconDodge = null;

            manifest.questBookIcon = LoadIcon("Object-4.png");
            manifest.questStarIcon = LoadIcon("Star 11.png");
            manifest.compassArrow = LoadIcon("Polygon 5.png");
            manifest.interactPromptFrame = null;

            EditorUtility.SetDirty(manifest);
            LogManifestStatus(manifest);
            EnsurePortraitManifest();
        }

        static void EnsurePortraitManifest()
        {
            var path = "Assets/_Project/Resources/Data/CharacterPortraitManifest.asset";
            var manifest = AssetDatabase.LoadAssetAtPath<CharacterPortraitManifest>(path);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<CharacterPortraitManifest>();
                AssetDatabase.CreateAsset(manifest, path);
            }

            manifest.hero01 = LoadCommon("image 306.png");
            manifest.hero02 = LoadIcon("Object-1.png");
            manifest.hero03 = LoadIcon("Object-2.png");
            manifest.hero04 = LoadIcon("Object-3.png");
            manifest.limitedHero = LoadIcon("Star 47.png");
            manifest.defaultPortrait = manifest.hero01;
            EditorUtility.SetDirty(manifest);
        }

        static Sprite LoadIcon(string fileName) =>
            BESUIEditorUtils.LoadSpriteInFolder(fileName, UIAssetPaths.Icons);

        static Sprite LoadFrame(string fileName) =>
            BESUIEditorUtils.LoadSpriteInFolder(fileName, UIAssetPaths.Frames);

        static Sprite LoadCommon(string fileName) =>
            BESUIEditorUtils.LoadSpriteInFolder(fileName, UIAssetPaths.Common);

        static void LogManifestStatus(HUDSpriteManifest manifest)
        {
            var nullCount = 0;
            if (manifest.playerDot == null) nullCount++;
            if (manifest.navInventory == null) nullCount++;
            if (manifest.navCharacter == null) nullCount++;
            if (manifest.navMap == null) nullCount++;
            if (manifest.navWish == null) nullCount++;
            if (manifest.navTeam == null) nullCount++;
            if (manifest.navEvent == null) nullCount++;
            if (manifest.partySlotFrame == null) nullCount++;
            if (manifest.skillSlotFrame == null) nullCount++;
            if (manifest.compassArrow == null) nullCount++;
            if (manifest.questBookIcon == null) nullCount++;
            if (manifest.lockIcon == null) nullCount++;
            if (manifest.chatBubbleIcon == null) nullCount++;

            if (nullCount > 0)
                Debug.LogWarning($"[BES] HUDSpriteManifest: {nullCount} slot(s) chưa map. Chạy import PNG trước.");
            else
                Debug.Log("[BES] HUDSpriteManifest: gameplay HUD Figma tokens — không map mockup slice.");
        }
    }
}
#endif
