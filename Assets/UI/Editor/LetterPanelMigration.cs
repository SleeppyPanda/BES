#if UNITY_EDITOR
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class LetterPanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";

        [MenuItem("BES/UI/Build Letter Panel")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "LetterPanel");
                if (panel == null) return;

                var oldCard = FindDirect(panel, "Card");
                if (oldCard != null) Object.DestroyImmediate(oldCard.gameObject);
                var oldContent = FindDirect(panel, "LetterContent");
                if (oldContent != null) Object.DestroyImmediate(oldContent.gameObject);

                var content = Rect("LetterContent", panel, Vector2.zero, Vector2.one);
                var whiteBackground = Image("PanelBackground", content, Vector2.zero, Vector2.one);
                whiteBackground.color = Color.white;

                var artwork = Image(
                    "LetterArtwork",
                    content,
                    new Vector2(.5f, .5f),
                    new Vector2(.5f, .5f));
                artwork.rectTransform.sizeDelta = new Vector2(1764f, 973f);
                artwork.sprite = LetterSprite();
                artwork.preserveAspect = true;
                artwork.raycastTarget = false;

                var senderName = Text(
                    "SenderName",
                    artwork.transform,
                    new Vector2(.075f, .69f),
                    new Vector2(.58f, .76f),
                    "SYSTEM");
                senderName.fontSize = 34f;
                senderName.fontStyle = FontStyles.Bold;
                senderName.alignment = TextAlignmentOptions.Left;

                var body = Text(
                    "LetterBody",
                    artwork.transform,
                    new Vector2(.075f, .19f),
                    new Vector2(.59f, .68f),
                    "Welcome, Traveler.\\n\\nThank you for beginning this journey. " +
                    "Please accept the attached reward.");
                body.fontSize = 28f;
                body.alignment = TextAlignmentOptions.TopLeft;
                body.textWrappingMode = TextWrappingModes.Normal;

                var portrait = Image(
                    "SenderPortrait",
                    artwork.transform,
                    new Vector2(.035f, .08f),
                    new Vector2(.16f, .39f));
                portrait.preserveAspect = true;
                portrait.raycastTarget = false;

                var rewardIcon = Image(
                    "RewardIcon",
                    artwork.transform,
                    new Vector2(.665f, .42f),
                    new Vector2(.79f, .64f));
                rewardIcon.preserveAspect = true;
                rewardIcon.raycastTarget = false;

                var rewardText = Text(
                    "RewardText",
                    artwork.transform,
                    new Vector2(.64f, .31f),
                    new Vector2(.84f, .40f),
                    "WELCOME REWARD  ×1");
                rewardText.fontSize = 27f;
                rewardText.fontStyle = FontStyles.Bold;

                var claimHitbox = Image(
                    "ClaimButton",
                    artwork.transform,
                    new Vector2(.642f, .158f),
                    new Vector2(.829f, .246f));
                claimHitbox.color = new Color(1f, 1f, 1f, 0f);
                var claimButton = claimHitbox.gameObject.AddComponent<Button>();
                claimButton.targetGraphic = claimHitbox;
                claimButton.transition = Selectable.Transition.None;

                var claimedState = Image(
                    "ClaimedState",
                    artwork.transform,
                    new Vector2(.642f, .158f),
                    new Vector2(.829f, .246f));
                claimedState.color = new Color(.25f, .12f, .09f, .82f);
                claimedState.raycastTarget = false;
                var claimedLabel = Text(
                    "Label",
                    claimedState.transform,
                    Vector2.zero,
                    Vector2.one,
                    "CLAIMED");
                claimedLabel.fontSize = 34f;
                claimedLabel.fontStyle = FontStyles.Bold;
                claimedLabel.color = new Color(1f, .94f, .82f, 1f);
                claimedState.gameObject.SetActive(false);

                var emptyState = Text(
                    "EmptyState",
                    artwork.transform,
                    new Vector2(.075f, .30f),
                    new Vector2(.59f, .62f),
                    "NO LETTERS");
                emptyState.fontSize = 38f;
                emptyState.fontStyle = FontStyles.Bold;
                emptyState.gameObject.SetActive(false);

                var closeImage = Image(
                    "CloseButton",
                    content,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f));
                closeImage.rectTransform.pivot = new Vector2(1f, 1f);
                closeImage.rectTransform.sizeDelta = new Vector2(90f, 90f);
                closeImage.rectTransform.anchoredPosition = new Vector2(-24f, -22f);
                closeImage.color = new Color(.25f, .12f, .09f, 0f);
                var closeButton = closeImage.gameObject.AddComponent<Button>();
                closeButton.targetGraphic = closeImage;
                var closeLabel = Text("Label", closeImage.transform, Vector2.zero, Vector2.one, "×");
                closeLabel.fontSize = 72f;
                closeLabel.color = new Color(.35f, .16f, .12f, 1f);

                var database = AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(
                    "Assets/Scenes/MenuContentDatabase.asset");
                var portraitSprite = database != null && database.characters.Count > 0
                    ? database.characters[0].chibi
                    : null;
                var rewardSprite = database != null && database.currencies.Count > 1
                    ? database.currencies[1].icon
                    : null;

                portrait.sprite = portraitSprite;
                portrait.enabled = portraitSprite != null;
                rewardIcon.sprite = rewardSprite;
                rewardIcon.enabled = rewardSprite != null;

                var controller = panel.GetComponent<LetterPanelController>() ??
                                 panel.gameObject.AddComponent<LetterPanelController>();
                WireController(
                    controller,
                    senderName,
                    body,
                    portrait,
                    rewardIcon,
                    rewardText,
                    claimButton,
                    claimedState.gameObject,
                    emptyState.gameObject,
                    portraitSprite,
                    rewardSprite);
                WireModal(panel.GetComponent<SimpleModalPanel>(), panel, closeButton);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] LetterPanel rebuilt from Letter.png with editable content, reward and Claim state.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void WireController(
            LetterPanelController controller,
            TMP_Text sender,
            TMP_Text body,
            Image portrait,
            Image rewardIcon,
            TMP_Text rewardText,
            Button claimButton,
            GameObject claimedState,
            GameObject emptyState,
            Sprite portraitSprite,
            Sprite rewardSprite)
        {
            var serialized = new SerializedObject(controller);
            var letters = serialized.FindProperty("letters");
            letters.arraySize = 1;
            var letter = letters.GetArrayElementAtIndex(0);
            letter.FindPropertyRelative("id").stringValue = "welcome_letter";
            letter.FindPropertyRelative("senderName").stringValue = "SYSTEM";
            letter.FindPropertyRelative("body").stringValue =
                "Welcome, Traveler.\\n\\nThank you for beginning this journey. " +
                "Please accept the attached reward.";
            letter.FindPropertyRelative("senderPortrait").objectReferenceValue = portraitSprite;
            letter.FindPropertyRelative("rewardIcon").objectReferenceValue = rewardSprite;
            letter.FindPropertyRelative("rewardName").stringValue = "WELCOME REWARD";
            letter.FindPropertyRelative("rewardAmount").intValue = 1;
            letter.FindPropertyRelative("claimed").boolValue = false;

            serialized.FindProperty("initialLetterIndex").intValue = 0;
            serialized.FindProperty("senderNameText").objectReferenceValue = sender;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("senderPortraitImage").objectReferenceValue = portrait;
            serialized.FindProperty("rewardIconImage").objectReferenceValue = rewardIcon;
            serialized.FindProperty("rewardText").objectReferenceValue = rewardText;
            serialized.FindProperty("claimButton").objectReferenceValue = claimButton;
            serialized.FindProperty("claimedState").objectReferenceValue = claimedState;
            serialized.FindProperty("emptyState").objectReferenceValue = emptyState;
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

        static Sprite LetterSprite()
        {
            foreach (var guid in AssetDatabase.FindAssets("Letter t:Sprite", new[] { "Assets/Art Ui" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.EndsWith("/Mới/Letter.png"))
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

        static TMP_Text Text(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string value)
        {
            var text = Rect(name, parent, anchorMin, anchorMax)
                .gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.27f, .16f, .13f, 1f);
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
