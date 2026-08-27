using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    public static class ListFBXClips
    {
        static ListFBXClips()
        {
            CheckClips();
        }

        public static void CheckClips()
        {
            string acolyteFolder = "Assets/MeshyImports/Meshy_AI_Gilded_Shadow_Acolyte_biped";
            string walkPath = $"{acolyteFolder}/Meshy_AI_Gilded_Shadow_Acolyte_biped_Animation_Walking_frame_rate_60.fbx";
            
            AnimationClip walkClip = LoadClipFromFBX(walkPath);
            Debug.Log($"[BES Debug Load] Walk clip path: '{walkPath}'");
            Debug.Log($"[BES Debug Load] LoadClipFromFBX result: " + (walkClip != null ? $"'{walkClip.name}'" : "null"));

            // Let's also print all sub-assets of this path
            if (File.Exists(walkPath))
            {
                var subAssets = AssetDatabase.LoadAllAssetsAtPath(walkPath);
                Debug.Log($"[BES Debug Load] Total sub-assets in FBX: {subAssets.Length}");
                foreach (var a in subAssets)
                {
                    Debug.Log($"  Sub-asset: name='{a.name}', type={a.GetType()}");
                }
            }
        }

        static AnimationClip LoadClipFromFBX(string fbxPath)
        {
            if (!File.Exists(fbxPath)) return null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in subAssets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
