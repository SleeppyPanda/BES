#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class BattleVictoryAndStoryProgressMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string MarkerSpritePath =
            "Assets/Art Ui/Mới/Play Screen story FIX/Cursho tiến độ.png";

        [MenuItem("BES/UI/Build Battle Win And Story Progress")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var navigator = root.GetComponent<MenuNavigator>();
                var story = root.GetComponentInChildren<StoryModePanelController>(true);
                var battlePanel = Find(root.transform, "BattlePanel");
                var battle = battlePanel != null ? battlePanel.GetComponent<TurnBattleUI>() : null;
                if (navigator == null || story == null || battle == null) return;

                RenameStoryBackground(root.transform);
                WireStoryProgress(root.transform, story);
                var winPanel = BuildWinPanel(battlePanel, out var returnButton);
                WireBattle(battle, navigator, story, winPanel, returnButton);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Battle WinPanel, Story return and seven-position story progress wired.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void RenameStoryBackground(Transform root)
        {
            var background = Find(root, "AssignableChapterBackground");
            if (background != null) background.name = "StoryBackground";
        }

        static void WireStoryProgress(Transform root, StoryModePanelController story)
        {
            var progressBar = Find(root, "ProgressBar");
            if (progressBar == null) return;

            var positions = new List<RectTransform>();
            for (var i = 1; i <= 7; i++)
            {
                var position = FindDirect(progressBar, "Position " + i) as RectTransform;
                if (position != null) positions.Add(position);
            }

            var marker = FindDirect(progressBar, "StoryProgressMarker") as RectTransform;
            if (marker == null)
            {
                var go = new GameObject(
                    "StoryProgressMarker",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.layer = LayerMask.NameToLayer("UI");
                marker = go.GetComponent<RectTransform>();
                marker.SetParent(progressBar, false);
                marker.sizeDelta = new Vector2(72f, 72f);
                var image = go.GetComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            marker.SetAsLastSibling();

            var serialized = new SerializedObject(story);
            serialized.FindProperty("storyProgressMarker").objectReferenceValue = marker;
            serialized.FindProperty("storyProgressMoveDuration").floatValue = .65f;
            serialized.FindProperty("storyProgressMoveCurve").animationCurveValue =
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            var list = serialized.FindProperty("storyProgressPositions");
            list.arraySize = positions.Count;
            for (var i = 0; i < positions.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = positions[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(story);
        }

        static GameObject BuildWinPanel(
            Transform battlePanel,
            out Button returnButton)
        {
            var existing = FindDirect(battlePanel, "WinPanel");
            if (existing != null)
            {
                existing.SetAsLastSibling();
                returnButton = Find(existing, "ReturnToStoryButton")?.GetComponent<Button>();
                return existing.gameObject;
            }

            var panel = CreateRect("WinPanel", battlePanel, Vector2.zero, Vector2.one);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, .72f);
            panelImage.raycastTarget = true;

            var frame = CreateRect(
                "WinFrame",
                panel,
                new Vector2(.32f, .30f),
                new Vector2(.68f, .70f));
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(.96f, .92f, .82f, 1f);

            var title = CreateText(
                "WinTitle",
                frame,
                "VICTORY",
                new Vector2(.08f, .56f),
                new Vector2(.92f, .88f),
                52f);
            title.color = new Color(.35f, .16f, .13f, 1f);

            var buttonRect = CreateRect(
                "ReturnToStoryButton",
                frame,
                new Vector2(.22f, .15f),
                new Vector2(.78f, .40f));
            var buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(.35f, .16f, .13f, 1f);
            returnButton = buttonRect.gameObject.AddComponent<Button>();
            returnButton.targetGraphic = buttonImage;
            var label = CreateText(
                "Label",
                buttonRect,
                "RETURN TO STORY",
                Vector2.zero,
                Vector2.one,
                24f);
            label.color = Color.white;

            panel.gameObject.SetActive(false);
            panel.SetAsLastSibling();
            return panel.gameObject;
        }

        static void WireBattle(
            TurnBattleUI battle,
            MenuNavigator navigator,
            StoryModePanelController story,
            GameObject winPanel,
            Button returnButton)
        {
            var serialized = new SerializedObject(battle);
            serialized.FindProperty("winPanel").objectReferenceValue = winPanel;
            serialized.FindProperty("winReturnButton").objectReferenceValue = returnButton;
            serialized.FindProperty("navigator").objectReferenceValue = navigator;
            serialized.FindProperty("storyModeController").objectReferenceValue = story;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float size)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static Transform FindDirect(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
                if (child.name == name) return child;
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
