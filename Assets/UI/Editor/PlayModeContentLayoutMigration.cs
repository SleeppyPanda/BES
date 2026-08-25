using UnityEditor;
using UnityEngine;

namespace BES.EditorTools
{
    // Auto-run disabled: Play Mode now uses the five localized tabs produced by PlayModePanelMigration.
    public static class PlayModeContentLayoutMigration
    {
        [MenuItem("BES/UI/Build Three Play Mode Content Layouts")]
        public static void Apply()
        {
            Debug.Log("[BES] PlayModeContentLayoutMigration is deprecated. The five tabs in PlayModePanelMigration render their own content via PlayModeContentBuilder.");
        }
    }
}