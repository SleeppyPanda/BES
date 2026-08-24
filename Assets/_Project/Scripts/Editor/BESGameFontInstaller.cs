using System.IO;
using BES.UI.Fonts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BES.Editor
{
    public static class BESGameFontInstaller
    {
        const string DefaultFontPath = "Assets/Art Ui/Game Việt hóa mới/hywenhei/zhcn.ttf";
        const string CharacterInfoFontPath = "Assets/Art Ui/Game Việt hóa mới/SVN-MoneyGame (1).otf";
        const string OutputFolder = "Assets/_Project/UI/Fonts";
        const string DefaultFontAssetPath = OutputFolder + "/BES_Default_zhcn.asset";
        const string CharacterInfoFontAssetPath = OutputFolder + "/BES_CharacterInfo_SVN_MoneyGame.asset";

        [MenuItem("BES/Fonts/Create TMP Fonts And Apply To Project")]
        public static void CreateAndApplyFonts()
        {
            Directory.CreateDirectory(OutputFolder);
            var defaultFontAsset = CreateOrUpdateFontAsset(DefaultFontPath, DefaultFontAssetPath);
            var characterInfoFontAsset = CreateOrUpdateFontAsset(CharacterInfoFontPath, CharacterInfoFontAssetPath);
            var defaultLegacyFont = AssetDatabase.LoadAssetAtPath<Font>(DefaultFontPath);
            var characterInfoLegacyFont = AssetDatabase.LoadAssetAtPath<Font>(CharacterInfoFontPath);

            if (defaultFontAsset != null && TMP_Settings.instance != null)
            {
                var tmpSettings = new SerializedObject(TMP_Settings.instance);
                var defaultFontProperty = tmpSettings.FindProperty("m_defaultFontAsset");
                if (defaultFontProperty != null)
                {
                    defaultFontProperty.objectReferenceValue = defaultFontAsset;
                    tmpSettings.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(TMP_Settings.instance);
                }
            }

            ApplyToOpenScenes(defaultFontAsset, characterInfoFontAsset, defaultLegacyFont, characterInfoLegacyFont);
            ApplyToPrefabs(defaultFontAsset, characterInfoFontAsset, defaultLegacyFont, characterInfoLegacyFont);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BES] Game fonts installed and applied. Default: zhcn.ttf, Character info: SVN-MoneyGame (1).otf");
        }

        static TMP_FontAsset CreateOrUpdateFontAsset(string sourceFontPath, string assetPath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
            if (font == null)
            {
                Debug.LogWarning($"[BES] Font not found: {sourceFontPath}");
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var fontAsset = TMP_FontAsset.CreateFontAsset(font);
            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        static void ApplyToOpenScenes(TMP_FontAsset defaultFontAsset, TMP_FontAsset characterInfoFontAsset, Font defaultLegacyFont, Font characterInfoLegacyFont)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    ApplyToRoot(root, defaultFontAsset, characterInfoFontAsset, defaultLegacyFont, characterInfoLegacyFont);
                }

                if (scene.isDirty)
                    EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        static void ApplyToPrefabs(TMP_FontAsset defaultFontAsset, TMP_FontAsset characterInfoFontAsset, Font defaultLegacyFont, Font characterInfoLegacyFont)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = PrefabUtility.LoadPrefabContents(path);
                var changed = ApplyToRoot(prefab, defaultFontAsset, characterInfoFontAsset, defaultLegacyFont, characterInfoLegacyFont);
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        static bool ApplyToRoot(GameObject root, TMP_FontAsset defaultFontAsset, TMP_FontAsset characterInfoFontAsset, Font defaultLegacyFont, Font characterInfoLegacyFont)
        {
            if (root == null) return false;
            var changed = false;

            var applier = root.GetComponentInChildren<GameFontApplier>(true);
            var canvas = root.GetComponentInChildren<Canvas>(true);
            if (applier == null && canvas != null)
            {
                applier = canvas.gameObject.AddComponent<GameFontApplier>();
                changed = true;
            }

            if (applier != null)
            {
                var serialized = new SerializedObject(applier);
                serialized.FindProperty("defaultGameFont").objectReferenceValue = defaultFontAsset;
                serialized.FindProperty("characterInfoFont").objectReferenceValue = characterInfoFontAsset;
                serialized.FindProperty("defaultLegacyFont").objectReferenceValue = defaultLegacyFont;
                serialized.FindProperty("characterInfoLegacyFont").objectReferenceValue = characterInfoLegacyFont;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(applier);
                changed = true;
            }

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                var targetFont = IsCharacterInfoText(text) ? characterInfoFontAsset : defaultFontAsset;
                if (targetFont == null || text.font == targetFont) continue;
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                changed = true;
            }

            var legacyTexts = root.GetComponentsInChildren<Text>(true);
            foreach (var text in legacyTexts)
            {
                var targetFont = IsCharacterInfoText(text.transform) ? characterInfoLegacyFont : defaultLegacyFont;
                if (targetFont == null || text.font == targetFont) continue;
                text.font = targetFont;
                EditorUtility.SetDirty(text);
                changed = true;
            }

            return changed;
        }

        static bool IsCharacterInfoText(TMP_Text text)
        {
            if (text == null) return false;
            var current = text.transform;
            while (current != null)
            {
                var name = current.name;
                if (name.Contains("CharacterProfile") ||
                    name.Contains("CharacterCollection") ||
                    name.Contains("CharacterDetail") ||
                    name.Contains("CharacterInfo") ||
                    name.Contains("CharacterStats") ||
                    name.Contains("CharacterDescription") ||
                    name.Contains("SelectedCharacterName") ||
                    name.Contains("LevelCharacterName") ||
                    name.Contains("DetailName") ||
                    name.Contains("DetailDescription"))
                    return true;
                current = current.parent;
            }
            return false;
        }

        static bool IsCharacterInfoText(Transform target)
        {
            if (target == null) return false;
            var current = target;
            while (current != null)
            {
                var name = current.name;
                if (name.Contains("CharacterProfile") ||
                    name.Contains("CharacterCollection") ||
                    name.Contains("CharacterDetail") ||
                    name.Contains("CharacterInfo") ||
                    name.Contains("CharacterStats") ||
                    name.Contains("CharacterDescription") ||
                    name.Contains("SelectedCharacterName") ||
                    name.Contains("LevelCharacterName") ||
                    name.Contains("DetailName") ||
                    name.Contains("DetailDescription"))
                    return true;
                current = current.parent;
            }
            return false;
        }
    }
}
