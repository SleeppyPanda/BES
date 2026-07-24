#if UNITY_EDITOR
using BES.UI.Menu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    public static class StoryRequirementMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.StoryRequirementImages.v1";

        static StoryRequirementMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        [MenuItem("BES/UI/Convert Story Progress To Requirements")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var controller = root.GetComponentInChildren<StoryModePanelController>(true);
                var mainPanel = Find(root.transform, "MainStoryPanel");
                var information = Find(mainPanel, "StoryInformationFrame");
                var requirementRoot = Find(information, "StoryProgress") ?? Find(information, "StoryRequirement");
                if (controller == null || information == null || requirementRoot == null) return;

                requirementRoot.name = "StoryRequirement";
                RemoveWrongProgressController(requirementRoot);

                var serialized = new SerializedObject(controller);
                var bindings = serialized.FindProperty("storyRequirements");
                bindings.arraySize = 5;
                for (var i = 0; i < 5; i++)
                {
                    var frame = Find(requirementRoot, "Progress_" + i) ?? Find(requirementRoot, "Requirement_" + i);
                    if (frame == null) continue;
                    frame.name = "Requirement_" + i;
                    var icon = FindDirect(frame, "RequirementImage");
                    if (icon == null)
                    {
                        var go = new GameObject("RequirementImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        go.layer = LayerMask.NameToLayer("UI");
                        icon = go.transform;
                        icon.SetParent(frame, false);
                        var rect = icon as RectTransform;
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = Vector2.zero;
                        rect.offsetMax = Vector2.zero;
                        var image = icon.GetComponent<Image>();
                        image.color = Color.white;
                        image.raycastTarget = false;
                    }

                    var binding = bindings.GetArrayElementAtIndex(i);
                    binding.FindPropertyRelative("root").objectReferenceValue = frame.gameObject;
                    binding.FindPropertyRelative("requirementImage").objectReferenceValue = icon.GetComponent<Image>();
                    var satisfied = FindDirect(frame, "SatisfiedState");
                    binding.FindPropertyRelative("satisfiedState").objectReferenceValue =
                        satisfied != null ? satisfied.gameObject : null;
                }

                var active = Find(information, "ActiveButton");
                if (active == null)
                {
                    var go = new GameObject("ActiveButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    go.layer = LayerMask.NameToLayer("UI");
                    active = go.transform;
                    active.SetParent(information, false);
                    var rect = active as RectTransform;
                    rect.anchorMin = new Vector2(.76f, .04f);
                    rect.anchorMax = new Vector2(.96f, .18f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    var image = active.GetComponent<Image>();
                    image.color = Color.white;
                    var button = active.GetComponent<Button>();
                    button.targetGraphic = image;
                }
                serialized.FindProperty("confirmPartyButton").objectReferenceValue = active.GetComponent<Button>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                active.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] StoryProgress converted to image-only StoryRequirement and ActiveButton wired.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void RemoveWrongProgressController(Transform root)
        {
            foreach (var component in root.GetComponents<MonoBehaviour>())
                if (component != null && component.GetType().Name == "StoryProgressPathController")
                    Object.DestroyImmediate(component);
            var marker = FindDirect(root, "StoryProgressMarker");
            if (marker != null) Object.DestroyImmediate(marker.gameObject);
        }

        static Transform FindDirect(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root) if (child.name == name) return child;
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
