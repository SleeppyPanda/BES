using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace BES.UI.Editor
{
    public static class FixProjectAssets
    {

        [MenuItem("BES/Fix Project Assets")]
        public static void RunFixes()
        {
            if (EditorApplication.isPlaying) return;

            Debug.Log("[BES Fix] Starting project assets fix...");
            FixVideos();
            FixTerrainTreePrototypes();
            Debug.Log("[BES Fix] Finished project assets fix.");
        }

        private static void FixVideos()
        {
            string[] paths = new string[]
            {
                "Assets/Art Ui/Game Việt hóa mới/Character/Auerlian/Auerlian.mp4",
                "Assets/Art Ui/animation/5 star summon.mp4",
                "Assets/Art Ui/animation/Summon no 5 star.mp4",
                "Assets/Art Ui/Game Việt hóa mới/test.mp4"
            };

            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[BES Fix] Video file not found: {path}");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as VideoClipImporter;
                if (importer != null)
                {
                    var settings = importer.defaultTargetSettings;
                    if (!settings.enableTranscoding || settings.codec != VideoCodec.VP8)
                    {
                        settings.enableTranscoding = true;
                        settings.codec = VideoCodec.VP8;
                        importer.defaultTargetSettings = settings;
                        importer.SaveAndReimport();
                        Debug.Log($"[BES Fix] Transcoded video to VP8: {path}");
                    }
                    else
                    {
                        Debug.Log($"[BES Fix] Video already transcoded to VP8: {path}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[BES Fix] Could not find VideoClipImporter for video at: {path}");
                }
            }
        }

        private static void FixTerrainTreePrototypes()
        {
            string placeholderPath = "Assets/Map/Vegetation_Stylized_Pack_ByLuxArtStudios/Prefabs/Trees/S_Tree_A.prefab";
            GameObject placeholderTree = AssetDatabase.LoadAssetAtPath<GameObject>(placeholderPath);
            if (placeholderTree == null)
            {
                // Fallback to another tree prefab if not found
                placeholderPath = "Assets/Map/3D set of stylized nature - GHIBLI style/Art/Prefabs/Tree_01.prefab";
                placeholderTree = AssetDatabase.LoadAssetAtPath<GameObject>(placeholderPath);
            }

            if (placeholderTree == null)
            {
                Debug.LogError("[BES Fix] Could not find any placeholder tree prefab!");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:TerrainData");
            bool anyChanged = false;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (terrainData != null)
                {
                    TreePrototype[] prototypes = terrainData.treePrototypes;
                    bool changed = false;
                    for (int i = 0; i < prototypes.Length; i++)
                    {
                        if (prototypes[i].prefab == null)
                        {
                            Debug.LogWarning($"[BES Fix] TerrainData '{terrainData.name}' ({path}) has missing tree prefab at index {i}. Assigning placeholder tree.");
                            prototypes[i].prefab = placeholderTree;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        terrainData.treePrototypes = prototypes;
                        EditorUtility.SetDirty(terrainData);
                        anyChanged = true;
                        Debug.Log($"[BES Fix] Repaired tree prototypes on TerrainData: {path}");
                    }
                }
            }

            if (anyChanged)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[BES Fix] Saved repaired TerrainData assets.");
            }
            else
            {
                Debug.Log("[BES Fix] No missing tree prototypes found on any TerrainData.");
            }
        }
    }
}
