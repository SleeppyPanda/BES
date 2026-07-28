#if UNITY_EDITOR
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;

namespace BES.EditorTools
{
    public static class CashShopSubtabPerformanceMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Optimize Cash Shop Subtab Animation")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var exchange = Find(root.transform, "ExchangeSubTabSystem");
                var group = exchange?.GetComponent<SmoothTabGroup>();
                if (group == null) return;

                var serialized = new SerializedObject(group);
                serialized.FindProperty("keepPanelsActive").boolValue = true;
                serialized.FindProperty("isolatePanelCanvases").boolValue = true;
                serialized.FindProperty("hiddenAlpha").floatValue = 0f;

                var panels = serialized.FindProperty("panels");
                for (var i = 0; i < panels.arraySize; i++)
                {
                    var panel = panels.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                    if (panel == null) continue;
                    if (panel.GetComponent<Canvas>() == null)
                        panel.AddComponent<Canvas>();
                    if (panel.GetComponent<CanvasGroup>() == null)
                        panel.AddComponent<CanvasGroup>();
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Exchange Shop subtabs are prewarmed and isolated from full Canvas rebuilds.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
#endif
