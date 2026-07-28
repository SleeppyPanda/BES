#if UNITY_EDITOR
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    /// <summary>
    /// Removes clipping masks accidentally added to shop navigation containers.
    /// Those containers preserve manually-authored child coordinates and are not
    /// clipping viewports.
    /// </summary>
    public static class CashShopViewportMaskRepair
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Repair Cash Shop Viewport Masks")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var changed = false;
                foreach (var group in root.GetComponentsInChildren<SmoothTabGroup>(true))
                {
                    var serialized = new SerializedObject(group);
                    var clip = serialized.FindProperty("clipToViewport");
                    if (clip != null && clip.boolValue)
                    {
                        clip.boolValue = false;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }

                foreach (var rectMask in root.GetComponentsInChildren<RectMask2D>(true))
                {
                    var objectName = rectMask.gameObject.name;
                    if (objectName != "ShopContents" &&
                        !objectName.StartsWith("MainContent_") &&
                        !objectName.EndsWith("SubTabSystem"))
                        continue;

                    Object.DestroyImmediate(rectMask, true);
                    changed = true;
                }

                if (!changed) return;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Removed invalid Cash Shop navigation masks; authored UI positions are preserved.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
