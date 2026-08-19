using UnityEditor;
using UnityEngine;

namespace BES.UI.Editor
{
    public static class OptimizeGachaVideos
    {
        [MenuItem("BES/Video/Optimize Gacha Videos")]
        public static void Optimize()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Optimization Error", "Please exit Play Mode before running video optimization.", "OK");
                return;
            }

            string[] paths = new string[]
            {
                "Assets/Art Ui/animation/5 star summon.mp4",
                "Assets/Art Ui/animation/Animation summon.mp4",
                "Assets/Art Ui/animation/Summon no 5 star.mp4"
            };

            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as VideoClipImporter;
                if (importer != null)
                {
                    // Set transcode settings to make it VP8 (software decoded, DX12 safe)
                    var settings = importer.defaultTargetSettings;
                    settings.enableTranscoding = true;
                    settings.codec = VideoCodec.VP8;
                    importer.defaultTargetSettings = settings;
                    
                    importer.SaveAndReimport();
                    Debug.Log($"[BES] Successfully optimized and transcoded video clip to VP8 at: {path}");
                }
                else
                {
                    Debug.LogWarning($"[BES] Could not find video clip importer at: {path}");
                }
            }

            EditorUtility.DisplayDialog("Optimization Success", "All gacha video clips have been optimized and transcoded to VP8 successfully!", "OK");
        }
    }
}
