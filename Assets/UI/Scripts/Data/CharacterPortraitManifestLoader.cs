using UnityEngine;

namespace BES.UI
{
    public static class CharacterPortraitManifestLoader
    {
        const string DefaultPath = "Data/CharacterPortraitManifest";

        public static CharacterPortraitManifest Load()
        {
            var manifest = Resources.Load<CharacterPortraitManifest>(DefaultPath);
#if UNITY_EDITOR
            if (manifest == null)
                manifest = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterPortraitManifest>(
                    "Assets/_Project/Resources/Data/CharacterPortraitManifest.asset");
#endif
            return manifest;
        }
    }
}
