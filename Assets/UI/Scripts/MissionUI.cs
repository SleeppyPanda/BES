using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BES.UI
{
    [System.Serializable]
    public class MissionSpriteEntry
    {
        public string name;
        public Sprite sprite;
    }

    public class MissionUI : MonoBehaviour
    {
        const string AssetRoot = "Assets/Art Ui/Moi/Misson/";
        const string UnicodeAssetRoot = "Assets/Art Ui/Mới/Misson/";

        [SerializeField] GameObject panel;
        [SerializeField] Button closeButton;
        [SerializeField] Sprite closeSprite;
        [SerializeField] bool closeOnEscape = true;
        [SerializeField] bool hideLegacyChildren = true;
        [SerializeField] List<MissionSpriteEntry> spriteLibrary = new();

        bool legacyChildrenHidden;
        Image background;
        TMP_Text titleText;
        readonly List<GameObject> runtimeCards = new();

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            EnsureRuntimeBindings();
            if (panel != null)
                panel.SetActive(false);
        }

        void Update()
        {
            if (closeOnEscape && IsOpen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        public void Open()
        {
            EnsureRuntimeBindings();
            if (panel == null)
                return;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Refresh();
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        void EnsureRuntimeBindings()
        {
            if (panel == null)
                panel = gameObject;

            var root = panel.transform;
            ApplyRect(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            EnsureTopCanvas(panel);
            ApplyPanelBlocker(panel);
            EnsureBackground(root);
            HideLegacyChildren(root);

            titleText ??= CreateText("RuntimeMissionTitle", root, "MISSON", 36f, TextAlignmentOptions.TopLeft);
            ApplyRect(titleText.rectTransform, new Vector2(0.01f, 0.90f), new Vector2(0.36f, 0.99f));

            if (!IsRuntimeObject(closeButton))
            {
                if (closeButton != null)
                    closeButton.gameObject.SetActive(false);
                closeButton = CreateCloseButton("RuntimeMissionCloseButton", root, Close);
            }
            ApplyRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.90f, 0.80f), new Vector2(0.94f, 0.90f));
            closeButton.transform.SetAsLastSibling();
        }

        void Refresh()
        {
            EnsureRuntimeBindings();
            for (var i = runtimeCards.Count - 1; i >= 0; i--)
                Destroy(runtimeCards[i]);
            runtimeCards.Clear();

            var missions = MissionCatalog.DefaultMissions();
            var positions = new[]
            {
                new Vector4(0.08f, 0.36f, 0.15f, 0.79f),
                new Vector4(0.245f, 0.36f, 0.315f, 0.79f),
                new Vector4(0.395f, 0.25f, 0.555f, 0.89f),
                new Vector4(0.68f, 0.36f, 0.75f, 0.79f),
                new Vector4(0.84f, 0.36f, 0.91f, 0.79f)
            };

            for (var i = 0; i < missions.Count && i < positions.Length; i++)
                CreateMissionCard(missions[i], positions[i]);
        }

        void CreateMissionCard(MissionDefinition mission, Vector4 rect)
        {
            var go = new GameObject("RuntimeMission_" + mission.id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel.transform, false);
            runtimeCards.Add(go);

            ApplyRect(go.GetComponent<RectTransform>(), new Vector2(rect.x, rect.y), new Vector2(rect.z, rect.w));
            var image = go.GetComponent<Image>();
            image.sprite = LoadSprite(mission.cardSpriteName);
            image.color = image.sprite == null ? new Color(1f, 0.95f, 0.82f, 1f) : Color.white;
            image.preserveAspect = true;

            var claim = new GameObject("RuntimeMissionClaim", typeof(RectTransform), typeof(Image), typeof(Button));
            claim.transform.SetParent(go.transform, false);
            ApplyRect(claim.GetComponent<RectTransform>(), new Vector2(0.26f, 0.07f), new Vector2(0.74f, 0.15f));
            var claimImage = claim.GetComponent<Image>();
            claimImage.sprite = LoadSprite(mission.claimSpriteName);
            claimImage.color = claimImage.sprite == null ? new Color(0.78f, 0.56f, 0.28f, 1f) : Color.white;
            claimImage.preserveAspect = true;
            var claimButton = claim.GetComponent<Button>();
            claimButton.transition = Selectable.Transition.None;
            claimButton.interactable = mission.claimable;
        }

        static void ApplyPanelBlocker(GameObject targetPanel)
        {
            var image = targetPanel.GetComponent<Image>();
            if (image == null)
                image = targetPanel.AddComponent<Image>();
            if (image == null)
                return;
            image.sprite = null;
            image.color = new Color(0.04f, 0.03f, 0.03f, 1f);
            image.raycastTarget = true;
        }

        void EnsureBackground(Transform root)
        {
            if (background == null)
            {
                var go = new GameObject("RuntimeMissionBackground", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                background = go.GetComponent<Image>();
                background.raycastTarget = false;
            }

            background.sprite = LoadSprite("Group 427323034");
            background.color = background.sprite == null ? new Color(0.08f, 0.06f, 0.05f, 1f) : Color.white;
            background.preserveAspect = false;
            ApplyRect(background.rectTransform, Vector2.zero, Vector2.one);
            background.transform.SetAsFirstSibling();
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

        Sprite LoadSprite(string spriteName)
        {
            for (var i = 0; i < spriteLibrary.Count; i++)
            {
                var entry = spriteLibrary[i];
                if (entry != null && entry.name == spriteName && entry.sprite != null)
                    return entry.sprite;
            }

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(UnicodeAssetRoot + spriteName + ".png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>(AssetRoot + spriteName + ".png");
#else
            return null;
#endif
        }

        static void EnsureTopCanvas(GameObject targetPanel)
        {
            var canvas = targetPanel.GetComponent<Canvas>();
            if (canvas == null)
                canvas = targetPanel.AddComponent<Canvas>();
            if (canvas == null)
                return;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

            if (targetPanel.GetComponent<GraphicRaycaster>() == null)
                targetPanel.AddComponent<GraphicRaycaster>();
        }

        static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        Button CreateCloseButton(string name, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = closeSprite;
            image.color = closeSprite == null ? new Color(1f, 1f, 1f, 0.01f) : Color.white;
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

        static void ApplyRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
