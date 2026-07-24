#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    public static class StoryRosterScrollAndProgressMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.StoryRosterScrollAndProgress.v1";

        static StoryRosterScrollAndProgressMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        [MenuItem("BES/UI/Build Story Roster Scroll And Progress")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var story = root.GetComponentInChildren<StoryModePanelController>(true);
                var selection = Find(root.transform, "CharacterSelectionPanel");
                var rosterPanel = Find(selection, "RosterPanel");
                var viewport = Find(rosterPanel, "RosterViewport");
                var content = Find(viewport, "RosterContent");
                var cardTemplate = Find(content, "RosterCard_0");
                if (story == null || rosterPanel == null || viewport == null || content == null || cardTemplate == null) return;

                ConfigureScroll(rosterPanel, viewport, content, cardTemplate);
                RebuildCardsFromTemplate(story, content, cardTemplate);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Story character roster is now a vertical 3-column scroll list.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ConfigureScroll(Transform panel, Transform viewport, Transform content, Transform cardTemplate)
        {
            var scroll = panel.GetComponent<ScrollRect>() ?? panel.gameObject.AddComponent<ScrollRect>();
            var mask = viewport.GetComponent<RectMask2D>() ?? viewport.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;
            scroll.viewport = viewport as RectTransform;
            scroll.content = content as RectTransform;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 35f;

            var contentRect = content as RectTransform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            var templateRect = cardTemplate as RectTransform;
            var cellSize = templateRect.sizeDelta;
            if (cellSize.x <= 1f || cellSize.y <= 1f) cellSize = new Vector2(205f, 286f);

            var grid = content.GetComponent<GridLayoutGroup>() ?? content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(18f, 20f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            var fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void RebuildCardsFromTemplate(StoryModePanelController story, Transform content, Transform template)
        {
            var storySo = new SerializedObject(story);
            var cards = storySo.FindProperty("rosterCards");
            var count = Mathf.Max(9, cards.arraySize);
            var templateBinding = cards.arraySize > 0 ? cards.GetArrayElementAtIndex(0) : null;
            var portraitPath = RelativeComponentPath(template, templateBinding, "portrait");
            var elementPath = RelativeComponentPath(template, templateBinding, "elementIcon");
            var namePath = RelativeComponentPath(template, templateBinding, "nameText");
            var levelPath = RelativeComponentPath(template, templateBinding, "levelText");
            var selectedPath = RelativeObjectPath(template, templateBinding, "selectedState");

            var oldCards = new List<GameObject>();
            foreach (Transform child in content)
                if (child != template && child.name.StartsWith("RosterCard_")) oldCards.Add(child.gameObject);
            foreach (var old in oldCards) Object.DestroyImmediate(old);

            var cardObjects = new List<Transform> { template };
            template.name = "RosterCard_0";
            for (var i = 1; i < count; i++)
            {
                var clone = Object.Instantiate(template.gameObject, content, false);
                clone.name = "RosterCard_" + i;
                cardObjects.Add(clone.transform);
            }

            cards.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var card = cardObjects[i];
                var binding = cards.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("button").objectReferenceValue = card.GetComponent<Button>();
                binding.FindPropertyRelative("portrait").objectReferenceValue = ComponentAt<Image>(card, portraitPath);
                binding.FindPropertyRelative("elementIcon").objectReferenceValue = ComponentAt<Image>(card, elementPath);
                binding.FindPropertyRelative("nameText").objectReferenceValue = ComponentAt<TMP_Text>(card, namePath);
                binding.FindPropertyRelative("levelText").objectReferenceValue = ComponentAt<TMP_Text>(card, levelPath);
                var selected = TransformAt(card, selectedPath);
                binding.FindPropertyRelative("selectedState").objectReferenceValue = selected != null ? selected.gameObject : null;
            }
            storySo.ApplyModifiedPropertiesWithoutUndo();
        }

        static string RelativeComponentPath(Transform template, SerializedProperty binding, string field)
        {
            if (binding == null) return string.Empty;
            var component = binding.FindPropertyRelative(field).objectReferenceValue as Component;
            return component != null ? AnimationUtility.CalculateTransformPath(component.transform, template) : string.Empty;
        }

        static string RelativeObjectPath(Transform template, SerializedProperty binding, string field)
        {
            if (binding == null) return string.Empty;
            var gameObject = binding.FindPropertyRelative(field).objectReferenceValue as GameObject;
            return gameObject != null ? AnimationUtility.CalculateTransformPath(gameObject.transform, template) : string.Empty;
        }

        static T ComponentAt<T>(Transform root, string path) where T : Component
        {
            var transform = TransformAt(root, path);
            return transform != null ? transform.GetComponent<T>() : null;
        }

        static Transform TransformAt(Transform root, string path) =>
            string.IsNullOrEmpty(path) ? root : root.Find(path);

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
