#if UNITY_EDITOR
using BES.UI;
using UnityEditor;
using UnityEngine;

namespace BES.Editor
{
    public class BESUIHudMapper : EditorWindow
    {
        HUDSpriteManifest manifest;
        Vector2 scroll;
        Sprite[] hudSprites;
        string[] hudPaths;

        public static void Open()
        {
            GetWindow<BESUIHudMapper>("BES HUD Mapper");
        }

        void OnEnable() => Reload();

        void Reload()
        {
            manifest = AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(UIAssetPaths.HudManifestAsset);
            if (manifest == null)
            {
                BESUIDataSetup.EnsureHudManifest();
                manifest = AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(UIAssetPaths.HudManifestAsset);
            }

            var guids = AssetDatabase.FindAssets("t:Sprite", new[]
            {
                UIAssetPaths.HudArt,
                UIAssetPaths.Icons,
                UIAssetPaths.Frames
            });

            hudPaths = new string[guids.Length];
            hudSprites = new Sprite[guids.Length];
            for (var i = 0; i < guids.Length; i++)
            {
                hudPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                hudSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(hudPaths[i]);
            }
        }

        void OnGUI()
        {
            if (manifest == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy HUDSpriteManifest. Chạy BES → Setup Project.", MessageType.Warning);
                if (GUILayout.Button("Create Manifest"))
                    BESUIDataSetup.EnsureHudManifest();
                return;
            }

            EditorGUILayout.LabelField("HUD Sprite Manifest", EditorStyles.boldLabel);
            var refTex = AssetDatabase.LoadAssetAtPath<Texture2D>(UIAssetPaths.BgMainPlay);
            if (refTex != null)
            {
                var rect = GUILayoutUtility.GetRect(320, 180);
                GUI.DrawTexture(rect, refTex, ScaleMode.ScaleToFit);
                EditorGUILayout.LabelField("Reference: Main play.png", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Auto-suggest mapping"))
            {
                BESUIHudAutoSuggest.Apply(manifest);
                AssetDatabase.SaveAssets();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSlot("Minimap Frame", ref manifest.minimapFrame);
            DrawSlot("Player Dot", ref manifest.playerDot);
            DrawSlot("Objective Dot", ref manifest.objectiveDot);
            DrawSlot("HP Bar BG", ref manifest.hpBarBackground);
            DrawSlot("HP Bar Fill", ref manifest.hpBarFill);
            DrawSlot("Stamina Bar BG", ref manifest.staminaBarBackground);
            DrawSlot("Stamina Bar Fill", ref manifest.staminaBarFill);
            DrawSlot("Mana Bar BG", ref manifest.manaBarBackground);
            DrawSlot("Mana Bar Fill", ref manifest.manaBarFill);
            DrawSlot("Nav Inventory", ref manifest.navInventory);
            DrawSlot("Nav Character", ref manifest.navCharacter);
            DrawSlot("Nav Map", ref manifest.navMap);
            DrawSlot("Nav Wish", ref manifest.navWish);
            DrawSlot("Nav Team", ref manifest.navTeam);
            DrawSlot("Nav Event", ref manifest.navEvent);
            DrawSlot("Nav Artifacts", ref manifest.navArtifacts);
            DrawSlot("Nav Weapon", ref manifest.navWeapon);
            DrawSlot("Party Slot Frame", ref manifest.partySlotFrame);
            DrawSlot("Skill Slot Frame", ref manifest.skillSlotFrame);
            DrawSlot("Compass Arrow", ref manifest.compassArrow);
            DrawSlot("Interact Frame", ref manifest.interactPromptFrame);
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
                EditorUtility.SetDirty(manifest);
        }

        void DrawSlot(string label, ref Sprite slot)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(140));
            slot = (Sprite)EditorGUILayout.ObjectField(slot, typeof(Sprite), false);
            EditorGUILayout.EndHorizontal();
            if (slot != null)
            {
                var r = GUILayoutUtility.GetRect(64, 64);
                GUI.DrawTexture(r, AssetPreview.GetAssetPreview(slot) ?? Texture2D.grayTexture, ScaleMode.ScaleToFit);
            }
        }
    }
}
#endif
