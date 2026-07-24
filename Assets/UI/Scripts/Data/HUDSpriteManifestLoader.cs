using UnityEngine;

namespace BES.UI
{
    public static class HUDSpriteManifestLoader
    {
        const string DefaultPath = "Data/HUDSpriteManifest";

        public static HUDSpriteManifest Load()
        {
            var manifest = Resources.Load<HUDSpriteManifest>(DefaultPath);
#if UNITY_EDITOR
            if (manifest == null)
                manifest = UnityEditor.AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(UIAssetPaths.HudManifestAsset);
#endif
            return manifest;
        }
    }
}
