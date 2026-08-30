#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace BES.EditorTools
{
    public static class InspectCharacterModels
    {
        [MenuItem("BES/Gameplay/Inspect Character Models")]
        public static void Inspect()
        {
            string folder = "Assets/Model character";
            if (!Directory.Exists(folder))
            {
                Debug.LogError($"Directory not found: {folder}");
                return;
            }

            var fbxs = Directory.GetFiles(folder, "*.fbx", SearchOption.AllDirectories);
            Debug.Log($"[INSPECTOR] Found {fbxs.Length} FBX files in {folder}.");

            foreach (var fbx in fbxs)
            {
                string relativePath = fbx.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(relativePath) as ModelImporter;
                if (importer == null) continue;

                Debug.Log($"[INSPECTOR] Model: {Path.GetFileName(fbx)} | Animation Type: {importer.animationType}");

                // Load all sub-assets to find animation clips
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(relativePath);
                int clipCount = 0;
                foreach (var asset in subAssets)
                {
                    if (asset is AnimationClip clip)
                    {
                        clipCount++;
                        Debug.Log($"   -> Clip: '{clip.name}' | Length: {clip.length}s | Legacy: {clip.legacy}");
                    }
                }
                if (clipCount == 0)
                {
                    Debug.Log($"   -> (No animation clips found in FBX file)");
                }
            }
        }
    }
}
#endif
