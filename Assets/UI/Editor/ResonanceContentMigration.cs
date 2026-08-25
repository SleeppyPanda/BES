using UnityEditor;
using UnityEngine;

namespace BES.EditorTools
{
    // Auto-run disabled: The Resonance Subtabs were replaced by the five Play Mode tabs in PlayModePanelMigration.
    public static class ResonanceContentMigration
    {
        [MenuItem("BES/UI/Build Resonance Tab Entries")]
        public static void Apply()
        {
            Debug.Log("[BES] ResonanceContentMigration is deprecated. The five tabs in PlayModePanelMigration render their own content via PlayModeContentBuilder.");
        }
    }
}