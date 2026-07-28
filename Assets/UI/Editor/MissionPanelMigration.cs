#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class MissionPanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Build Mission Hover Panel")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "MissionPanel");
                if (panel == null) return;

                var oldCard = FindDirect(panel, "Card");
                if (oldCard != null) Object.DestroyImmediate(oldCard.gameObject);
                var oldContent = FindDirect(panel, "MissionContent");
                if (oldContent != null) Object.DestroyImmediate(oldContent.gameObject);

                var backgroundSprite = MissionSprite("Group 427323034.png");
                var expandedSprite = MissionSprite("Group 427323033.png");
                var claimSprite = MissionSprite("Group 427323020.png");
                var claimedSprite = MissionSprite("Group 427323023.png");
                var smallSprites = new[]
                {
                    MissionSprite("Group 427323029.png"),
                    MissionSprite("Group 427323031.png"),
                    MissionSprite("Group 427323032.png")
                };

                var content = Rect("MissionContent", panel, Vector2.zero, Vector2.one);
                var background = Image("Background", content, Vector2.zero, Vector2.one);
                background.sprite = backgroundSprite;
                background.preserveAspect = false;
                background.raycastTarget = true;

                var cardArea = Rect("MissionCardArea", content, Vector2.zero, Vector2.one);
                var controller = panel.GetComponent<MissionPanelController>() ??
                                 panel.gameObject.AddComponent<MissionPanelController>();
                var cards = new List<MissionCardBinding>();
                var xPositions = new[] { -620f, -310f, 0f, 310f, 620f };

                for (var i = 0; i < 5; i++)
                {
                    var cardImage = Image(
                        "MissionCard_" + i,
                        cardArea,
                        new Vector2(.5f, .5f),
                        new Vector2(.5f, .5f));
                    var rect = cardImage.rectTransform;
                    rect.sizeDelta = new Vector2(189f, 740f);
                    rect.anchoredPosition = new Vector2(xPositions[i], -12f);
                    rect.localScale = Vector3.one;
                    cardImage.sprite = smallSprites[i % smallSprites.Length];
                    cardImage.preserveAspect = false;

                    var hover = cardImage.gameObject.AddComponent<MissionHoverCard>();
                    hover.Configure(controller, i);

                    var claimImage = Image(
                        "ClaimButton",
                        rect,
                        new Vector2(.5f, 0f),
                        new Vector2(.5f, 0f));
                    claimImage.rectTransform.sizeDelta = new Vector2(95f, 41f);
                    claimImage.rectTransform.anchoredPosition = new Vector2(0f, 92f);
                    claimImage.sprite = claimSprite;
                    claimImage.preserveAspect = true;
                    var claimButton = claimImage.gameObject.AddComponent<Button>();
                    claimButton.targetGraphic = claimImage;
                    claimButton.transition = Selectable.Transition.ColorTint;

                    cards.Add(new MissionCardBinding
                    {
                        missionId = "mission_" + i,
                        root = rect,
                        cardImage = cardImage,
                        claimButton = claimButton,
                        claimButtonImage = claimImage,
                        normalSprite = smallSprites[i % smallSprites.Length],
                        expandedSprite = expandedSprite,
                        claimAvailableSprite = claimSprite,
                        claimedSprite = claimedSprite,
                        normalSize = new Vector2(189f, 740f),
                        expandedSize = new Vector2(343f, 944f)
                    });
                }

                var closeImage = Image(
                    "CloseButton",
                    content,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));
                closeImage.rectTransform.pivot = new Vector2(1f, 1f);
                closeImage.rectTransform.sizeDelta = new Vector2(90f, 90f);
                closeImage.rectTransform.anchoredPosition = new Vector2(-50f, -40f);
                closeImage.color = new Color(1f, 1f, 1f, 0f);
                var closeButton = closeImage.gameObject.AddComponent<Button>();
                closeButton.targetGraphic = closeImage;
                var closeLabel = Text("Label", closeImage.transform, "×");
                closeLabel.fontSize = 72f;

                WireController(controller, cards);
                WireModal(panel.GetComponent<SimpleModalPanel>(), panel, closeButton);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] MissionPanel rebuilt with five hover-expand mission cards and Claim actions.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void WireController(
            MissionPanelController controller,
            List<MissionCardBinding> cards)
        {
            var serialized = new SerializedObject(controller);
            var list = serialized.FindProperty("cards");
            list.arraySize = cards.Count;
            for (var i = 0; i < cards.Count; i++)
            {
                var target = list.GetArrayElementAtIndex(i);
                var source = cards[i];
                target.FindPropertyRelative("missionId").stringValue = source.missionId;
                target.FindPropertyRelative("root").objectReferenceValue = source.root;
                target.FindPropertyRelative("cardImage").objectReferenceValue = source.cardImage;
                target.FindPropertyRelative("claimButton").objectReferenceValue = source.claimButton;
                target.FindPropertyRelative("claimButtonImage").objectReferenceValue = source.claimButtonImage;
                target.FindPropertyRelative("normalSprite").objectReferenceValue = source.normalSprite;
                target.FindPropertyRelative("expandedSprite").objectReferenceValue = source.expandedSprite;
                target.FindPropertyRelative("claimAvailableSprite").objectReferenceValue = source.claimAvailableSprite;
                target.FindPropertyRelative("claimedSprite").objectReferenceValue = source.claimedSprite;
                target.FindPropertyRelative("normalSize").vector2Value = source.normalSize;
                target.FindPropertyRelative("expandedSize").vector2Value = source.expandedSize;
                target.FindPropertyRelative("claimed").boolValue = false;
            }
            serialized.FindProperty("neighborShift").floatValue = 72f;
            serialized.FindProperty("expandedYOffset").floatValue = 0f;
            serialized.FindProperty("smoothTime").floatValue = .11f;
            serialized.FindProperty("saveClaimedState").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireModal(
            SimpleModalPanel modal,
            Transform panel,
            Button closeButton)
        {
            if (modal == null) return;
            var serialized = new SerializedObject(modal);
            serialized.FindProperty("panelRoot").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("closeButton").objectReferenceValue = closeButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite MissionSprite(string fileName)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art Ui" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Replace('\\', '/').EndsWith("/Misson/" + fileName))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        static RectTransform Rect(
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
            rect.localScale = Vector3.one;
            return rect;
        }

        static Image Image(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax) =>
            Rect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Image>();

        static TMP_Text Text(string name, Transform parent, string value)
        {
            var text = Rect(name, parent, Vector2.zero, Vector2.one)
                .gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static Transform FindDirect(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
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
