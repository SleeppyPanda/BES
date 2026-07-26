using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI
{
    public class LetterUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] LetterDatabase database;
        [SerializeField] Transform listContainer;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text senderText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text rewardText;
        [SerializeField] Button closeButton;
        [SerializeField] Button claimButton;
        [SerializeField] Sprite artworkSprite;
        [SerializeField] Sprite closeSprite;
        [SerializeField] bool buildRuntimeFallback = true;
        [SerializeField] bool closeOnEscape = true;
        [SerializeField] bool useArtworkLayout = true;
        [SerializeField] bool hideLegacyChildren = true;

        bool legacyChildrenHidden;

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            EnsureRuntimeBindings();
            EnsureDatabase();
            if (panel != null)
                panel.SetActive(false);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }
            if (claimButton != null)
            {
                claimButton.onClick.RemoveListener(ClaimSelectedLetter);
                claimButton.onClick.AddListener(ClaimSelectedLetter);
            }
        }

        void Update()
        {
            if (closeOnEscape && IsOpen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        public void Open()
        {
            EnsureRuntimeBindings();
            EnsureDatabase();
            if (panel == null)
                return;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Refresh();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public void Refresh()
        {
            EnsureRuntimeBindings();
            EnsureDatabase();
            if (listContainer == null || database == null)
                return;

            for (var i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            foreach (var letter in database.Letters)
            {
                if (letter == null)
                    continue;

                var captured = letter;
                var prefix = captured.isRead ? "Read" : "New";
                CreateRuntimeRow($"{prefix} - {captured.title}", () => SelectLetter(captured));
            }

            if (database.Letters.Count > 0)
            {
                var first = database.Letters[0];
                SetDetail(first.title, first.senderName, first.body, first.rewardLabel);
            }
            else
            {
                SetDetail("LETTERS", "", "Không có thư.", "");
            }
        }

        void SelectLetter(LetterDefinition letter)
        {
            if (letter == null)
                return;

            database.MarkRead(letter.letterId);
            SetDetail(letter.title, letter.senderName, letter.body, letter.rewardLabel);
        }

        void ClaimSelectedLetter()
        {
            if (rewardText != null && !string.IsNullOrWhiteSpace(rewardText.text))
                rewardText.text = "Claimed";
        }

        void SetDetail(string title, string sender, string body, string reward)
        {
            if (titleText != null) titleText.text = title;
            if (senderText != null) senderText.text = string.IsNullOrWhiteSpace(sender) ? "" : $"From: {sender}";
            if (bodyText != null) bodyText.text = body;
            if (rewardText != null) rewardText.text = string.IsNullOrWhiteSpace(reward) ? "" : $"Reward: {reward}";
        }

        void EnsureDatabase()
        {
            if (database != null)
                return;

            database = Resources.Load<LetterDatabase>("Data/LetterDatabase");
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<LetterDatabase>();
                database.ResetToDefaultEntries();
            }
        }

        void EnsureRuntimeBindings()
        {
            if (!buildRuntimeFallback)
                return;

            if (panel == null)
                panel = gameObject;

            var root = panel.transform;
            ApplyArtworkBackground(panel);
            HideLegacyChildren(root);

            if (useArtworkLayout)
                ApplyRect(panel.GetComponent<RectTransform>(), new Vector2(0.14f, 0.15f), new Vector2(0.86f, 0.85f));

            if (listContainer == null)
            {
                var list = CreateRect("RuntimeLetterList", root);
                list.anchorMin = useArtworkLayout ? new Vector2(0.08f, 0.14f) : new Vector2(0.06f, 0.16f);
                list.anchorMax = useArtworkLayout ? new Vector2(0.2f, 0.28f) : new Vector2(0.38f, 0.78f);
                list.offsetMin = Vector2.zero;
                list.offsetMax = Vector2.zero;
                var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.spacing = 8f;
                listContainer = list;
            }

            titleText ??= CreateText("RuntimeLetterTitle", root, "LETTERS", 22f, TextAlignmentOptions.TopLeft);
            senderText ??= CreateText("RuntimeLetterSender", root, "", 15f, TextAlignmentOptions.TopLeft);
            bodyText ??= CreateText("RuntimeLetterBody", root, "", 18f, TextAlignmentOptions.TopLeft);
            rewardText ??= CreateText("RuntimeLetterReward", root, "", 16f, TextAlignmentOptions.TopLeft);
            if (claimButton == null || !claimButton.gameObject.activeSelf)
                claimButton = CreateInvisibleButton("RuntimeClaimHitbox", root, ClaimSelectedLetter);
            if (!IsRuntimeObject(closeButton))
            {
                if (closeButton != null)
                    closeButton.gameObject.SetActive(false);
                closeButton = CreateCloseButton("RuntimeCloseButton", root, Close, closeSprite);
            }

            if (useArtworkLayout)
            {
                ApplyRect(titleText.rectTransform, new Vector2(0.15f, 0.67f), new Vector2(0.62f, 0.73f));
                ApplyRect(senderText.rectTransform, new Vector2(0.15f, 0.61f), new Vector2(0.62f, 0.66f));
                ApplyRect(bodyText.rectTransform, new Vector2(0.15f, 0.38f), new Vector2(0.62f, 0.60f));
                ApplyRect(rewardText.rectTransform, new Vector2(0.68f, 0.22f), new Vector2(0.85f, 0.29f));
                ApplyRect(claimButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.20f), new Vector2(0.85f, 0.28f));
                ApplyRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.91f, 0.82f), new Vector2(0.97f, 0.93f));
                closeButton.transform.SetAsLastSibling();
                SetPaperTextStyle(titleText, 17f);
                SetPaperTextStyle(senderText, 13f);
                SetPaperTextStyle(bodyText, 15f);
                SetPaperTextStyle(rewardText, 12f);
            }
            else
            {
                ApplyRect(titleText.rectTransform, new Vector2(0.44f, 0.68f), new Vector2(0.92f, 0.78f));
                ApplyRect(senderText.rectTransform, new Vector2(0.44f, 0.61f), new Vector2(0.92f, 0.67f));
                ApplyRect(bodyText.rectTransform, new Vector2(0.44f, 0.28f), new Vector2(0.92f, 0.59f));
                ApplyRect(rewardText.rectTransform, new Vector2(0.44f, 0.16f), new Vector2(0.92f, 0.24f));
                ApplyRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.88f, 0.82f), new Vector2(0.97f, 0.94f));
                closeButton.transform.SetAsLastSibling();
            }
        }

        void HideLegacyChildren(Transform root)
        {
            if (!hideLegacyChildren || legacyChildrenHidden || root == null)
                return;

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!child.name.StartsWith("Runtime", System.StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }

            legacyChildrenHidden = true;
        }

        void ApplyArtworkBackground(GameObject targetPanel)
        {
            if (artworkSprite == null || targetPanel == null)
                return;

            var image = targetPanel.GetComponent<Image>() ?? targetPanel.AddComponent<Image>();
            image.sprite = artworkSprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = true;
        }

        void CreateRuntimeRow(string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("LetterRow", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(listContainer, false);
            go.GetComponent<Image>().color = useArtworkLayout ? new Color(1f, 1f, 1f, 0.01f) : new Color(1f, 1f, 1f, 0.08f);
            go.GetComponent<RectTransform>().sizeDelta = useArtworkLayout ? new Vector2(64f, 28f) : new Vector2(0f, 46f);
            var button = go.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);

            var text = CreateText("Label", go.transform, label, 17f, TextAlignmentOptions.MidlineLeft);
            ApplyRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));
            if (useArtworkLayout)
                text.gameObject.SetActive(false);
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static Button CreateInvisibleButton(string name, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);
            return button;
        }

        static Button CreateCloseButton(string name, Transform parent, UnityEngine.Events.UnityAction onClick, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite == null ? new Color(1f, 1f, 1f, 0.01f) : Color.white;
            image.preserveAspect = true;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            return button;
        }

        static bool IsRuntimeObject(Component component)
        {
            return component != null && component.gameObject.name.StartsWith("Runtime", System.StringComparison.Ordinal);
        }

        static void SetPaperTextStyle(TMP_Text text, float maxSize)
        {
            text.color = new Color(0.24f, 0.14f, 0.09f, 1f);
            text.fontSize = maxSize;
            text.fontSizeMax = maxSize;
        }

        static void ApplyRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            ApplyRect(rect, min, max, Vector2.zero, Vector2.zero);
        }

        static void ApplyRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
