#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;

namespace BES.EditorTools
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class UnusedPanelCleanup
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.UnusedPanelCleanup.v1";

        static UnusedPanelCleanup() => EditorApplication.delayCall += RunOnce;
        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Clean();
        }

        [MenuItem("BES/UI/Remove Unreferenced Panels")]
        public static void Clean()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            var removed = new List<string>();
            try
            {
                foreach (var modal in root.GetComponentsInChildren<SimpleModalPanel>(true))
                {
                    if (IsReferencedOutside(modal, root)) continue;
                    removed.Add(modal.name);
                    Object.DestroyImmediate(modal.gameObject);
                }

                // These were the four obsolete per-mode panels replaced by the
                // single shared PlayModePanel. Remove remnants if an older prefab
                // revision still contains them.
                for (var i = 0; i < 4; i++)
                {
                    var obsolete = Find(root.transform, "ModePanel_" + i);
                    if (obsolete == null) continue;
                    removed.Add(obsolete.name);
                    Object.DestroyImmediate(obsolete.gameObject);
                }

                if (removed.Count > 0) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log(removed.Count == 0
                    ? "[BES] Panel cleanup: no unreferenced panels remain."
                    : "[BES] Removed unused panels: " + string.Join(", ", removed));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static bool IsReferencedOutside(SimpleModalPanel modal, GameObject prefabRoot)
        {
            foreach (var component in prefabRoot.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component.transform.IsChildOf(modal.transform)) continue;
                SerializedObject serialized;
                try { serialized = new SerializedObject(component); }
                catch { continue; }
                var property = serialized.GetIterator();
                var enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = true;
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var reference = property.objectReferenceValue;
                    if (reference == modal || reference == modal.gameObject ||
                        reference is Component targetComponent && targetComponent.transform.IsChildOf(modal.transform))
                        return true;
                }
            }
            return false;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }
    }
}
#endif
