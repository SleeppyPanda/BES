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

        [MenuItem("BES/UI/Fix Play Mode Tabs And Remove Main Tab Hover")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var playModePanel = Find(root.transform, "PlayModePanel");
                var mainController = playModePanel != null ? playModePanel.GetComponent<PlayModePanelController>() : null;
                if (mainController == null) return;

                RemoveMainTabHover(mainController);
                RebindResonanceSubTabs(playModePanel);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Play Mode subtabs mapped 1:1 and hover removed from the four main Tab buttons.");
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

        static void RebindResonanceSubTabs(Transform playModePanel)
        {
            var content = Find(playModePanel, "Content_0_Resonance Sanctum");
            var controller = content != null ? content.GetComponent<ResonanceSubTabController>() : null;
            if (controller == null) return;

            var serialized = new SerializedObject(controller);
            var tabs = serialized.FindProperty("tabs");
            tabs.arraySize = 4;
            var names = new[]
            {
                "Sanctum of Lost Echoes",
                "Sanctum of Ascension",
                "Sanctum of Insight",
                "Sanctum of Forging"
            };

            for (var i = 0; i < 4; i++)
            {
                var buttonRoot = Find(content, "SubTab_" + i);
                var listRoot = FindByPrefix(content, "TabList_" + i + "_");
                var selectedState = buttonRoot != null ? Find(buttonRoot, "SelectedState") : null;
                var binding = tabs.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("tabName").stringValue = names[i];
                binding.FindPropertyRelative("button").objectReferenceValue = buttonRoot != null ? buttonRoot.GetComponent<Button>() : null;
                binding.FindPropertyRelative("listRoot").objectReferenceValue = listRoot != null ? listRoot.gameObject : null;
                binding.FindPropertyRelative("selectedState").objectReferenceValue = selectedState != null ? selectedState.gameObject : null;
            }
            serialized.FindProperty("initialTab").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            for (var i = 0; i < 4; i++)
            {
                var list = FindByPrefix(content, "TabList_" + i + "_");
                if (list != null) list.gameObject.SetActive(i == 0);
            }
        }

        static Transform FindByPrefix(Transform root, string prefix)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal)) return child;
            return null;
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
