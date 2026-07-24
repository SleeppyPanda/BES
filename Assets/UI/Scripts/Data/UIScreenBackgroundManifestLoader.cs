using UnityEngine;

namespace BES.UI
{
    public static class UIScreenBackgroundManifestLoader
    {
        const string ResourcePath = "Data/UIScreenBackgroundManifest";

        public static UIScreenBackgroundManifest Load()
        {
            return Resources.Load<UIScreenBackgroundManifest>(ResourcePath);
        }
    }
}
