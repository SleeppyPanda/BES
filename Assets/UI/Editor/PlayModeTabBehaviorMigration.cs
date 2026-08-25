#if UNITY_EDITOR
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class PlayModeTabBehaviorMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.PlayModeTabBehavior.v1";

        static PlayModeTabBehaviorMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        [MenuItem("BES/UI/Strip Play Mode Main Tab Hover")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var playModePanel = Find(root.transform, "PlayModePanel");
                var mainController = playModePanel != null ? playModePanel.GetComponent<PlayModePanelController>() : null;
                if (mainController == null) return;

                RemoveMainTabHover(mainController);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Play Mode main Tab buttons no longer use hover sprites.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void RemoveMainTabHover(PlayModePanelController controller)
        {
            var serialized = new SerializedObject(controller);
            var tabs = serialized.FindProperty("tabs");
            for (var i = 0; i < tabs.arraySize; i++)
            {
                var button = tabs.GetArrayElementAtIndex(i).FindPropertyRelative("tabButton").objectReferenceValue as Button;
                if (button == null) continue;
                var hover = button.GetComponent<HoverSpriteButton>();
                if (hover != null) Object.DestroyImmediate(hover);
                button.transition = Selectable.Transition.None;
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