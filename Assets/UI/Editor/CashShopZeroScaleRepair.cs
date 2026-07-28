#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BES.EditorTools
{
    /// <summary>
    /// Repairs RectTransforms that were collapsed while shop children were
    /// reparented into the new sub-tab hierarchy. Position, anchors and size are
    /// intentionally left untouched.
    /// </summary>
    public static class CashShopZeroScaleRepair
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Repair Cash Shop Zero Scales")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var shop = Find(root.transform, "CashShopPanel");
                if (shop == null) return;

                var repaired = 0;
                foreach (var rect in shop.GetComponentsInChildren<RectTransform>(true))
                {
                    var scale = rect.localScale;
                    if (Mathf.Abs(scale.x) > .0001f &&
                        Mathf.Abs(scale.y) > .0001f &&
                        Mathf.Abs(scale.z) > .0001f)
                        continue;

                    rect.localScale = Vector3.one;
                    repaired++;
                }

                if (repaired <= 0)
                {
                    Debug.Log("[BES] Cash Shop zero-scale scan completed; nothing required repair.");
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[BES] Restored {repaired} collapsed Cash Shop RectTransform scale(s) to (1,1,1).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
#endif
