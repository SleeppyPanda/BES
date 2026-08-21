using UnityEditor;
using UnityEngine;
using BES.UI;

namespace BES.UI.Editor
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class RebuildAuthUIOnCompile
    {
        private const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MainMenuScreen.prefab";

        static RebuildAuthUIOnCompile()
        {
            // Run on project compile / startup
            EditorApplication.delayCall += Run;
        }

        [MenuItem("BES/UI/Enable and Generate Auth UI")]
        public static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[BES] Could not load prefab at {PrefabPath}");
                return;
            }

            var profileUI = root.GetComponentInChildren<PlayerProfileUI>(true);
            if (profileUI == null)
            {
                Debug.LogError("[BES] PlayerProfileUI component not found in MainMenuScreen prefab!");
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            // 1. Activate the PlayerProfileUI GameObject so it is visible in the editor scene
            profileUI.gameObject.SetActive(true);

            // 2. Generate all the password fields, codes, and style them
            profileUI.GenerateEditorUIFields();

            // 3. Save changes back to the Prefab
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log("[BES] Successfully activated and generated Auth UI fields in MainMenuScreen.prefab!");
        }

        [MenuItem("BES/UI/Rebuild Settings Panel")]
        public static void RebuildSettings()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null) return;

            var settings = root.GetComponentInChildren<SettingsUI>(true);
            if (settings != null)
            {
                var content = settings.transform.Find("SettingsContent");
                if (content != null)
                {
                    for (int i = content.childCount - 1; i >= 0; i--)
                    {
                        Object.DestroyImmediate(content.GetChild(i).gameObject);
                    }

                    var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/UI/Prefabs/Atoms/UISettingsRow.prefab");
                    if (rowPrefab != null)
                    {
                        var musicRow = Object.Instantiate(rowPrefab, content);
                        musicRow.name = "MusicVolumeRow";
                        musicRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
                        musicRow.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);

                        var sfxRow = Object.Instantiate(rowPrefab, content);
                        sfxRow.name = "SfxVolumeRow";
                        sfxRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
                        sfxRow.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);

                        var fsRow = Object.Instantiate(rowPrefab, content);
                        fsRow.name = "FullscreenRow";
                        fsRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);
                        fsRow.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);

                        var so = new SerializedObject(settings);
                        so.FindProperty("musicVolumeRow").objectReferenceValue = musicRow.GetComponent<UISettingsRow>();
                        so.FindProperty("sfxVolumeRow").objectReferenceValue = sfxRow.GetComponent<UISettingsRow>();
                        so.FindProperty("fullscreenRow").objectReferenceValue = fsRow.GetComponent<UISettingsRow>();
                        so.ApplyModifiedProperties();
                    }
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[BES] Successfully rebuilt settings volume rows in MainMenuScreen.prefab!");
        }
    }
}
