#if UNITY_EDITOR
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    public static class PlayModePanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.PlayModeSharedPanel.v3";
        static readonly Color NormalTextBrown = new Color(0.35f, 0.16f, 0.13f, 1f);
        static readonly string[] Names = { "Resonance Sanctum", "Sanctum of Lost Echoes", "Rift of the Hunt", "Divine Remnant" };

        static PlayModePanelMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            PatchHoverTextOnly();
        }

        static void PatchHoverTextOnly()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                foreach (var hover in root.GetComponentsInChildren<HoverSpriteButton>(true))
                {
                    var button = hover.GetComponent<Button>();
                    if (button != null) ConfigureHoverText(hover, button);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Play Mode hover text configured: current brown when normal, white when hovered.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("BES/UI/Create Shared Play Mode Panel")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var home = root.GetComponentInChildren<MenuHomeController>(true);
                var modalLayer = Find(root.transform, "ModalLayer");
                if (home == null || modalLayer == null) return;

                var oldPanel = Find(modalLayer, "PlayModePanel");
                if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

                var panel = Rect("PlayModePanel", modalLayer, Vector2.zero, Vector2.one);
                var background = panel.AddComponent<Image>();
                background.sprite = SpriteAt("Assets/Art Ui/Tranning Mode/Background Overall.png");
                background.color = Color.white;

                var frame = Rect("PanelFrame", panel.transform, new Vector2(.17f, .12f), new Vector2(.96f, .90f));
                var frameImage = frame.AddComponent<Image>();
                frameImage.sprite = SpriteAt("Assets/Art Ui/Tranning Mode/Background panel.png");
                frameImage.color = Color.white;

                var close = Button("CloseButton", frame.transform, "×", new Vector2(.91f, .88f), new Vector2(.99f, .99f));
                var controller = panel.AddComponent<PlayModePanelController>();
                Set(controller, "panelRoot", panel);
                Set(controller, "closeButton", close);

                var tabList = new (Button button, GameObject content, GameObject selected)[4];
                for (var i = 0; i < Names.Length; i++)
                {
                    var top = .84f - i * .14f;
                    var tabButton = Button("Tab_" + i + "_" + Names[i], panel.transform, Names[i], new Vector2(.02f, top - .10f), new Vector2(.17f, top));
                    var hover = tabButton.gameObject.AddComponent<HoverSpriteButton>();
                    Set(hover, "targetImage", tabButton.GetComponent<Image>());
                    Set(hover, "normalSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Normal.png"));
                    Set(hover, "hoverSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Hover.png"));
                    ConfigureHoverText(hover, tabButton);
                    var selected = Rect("SelectedState", tabButton.transform, Vector2.zero, Vector2.one);
                    selected.AddComponent<Image>().sprite = SpriteAt("Assets/Art Ui/Play Mode/Button Hover.png");
                    selected.transform.SetAsFirstSibling();

                    var content = Rect("Content_" + i + "_" + Names[i], frame.transform, new Vector2(.04f, .06f), new Vector2(.96f, .88f));
                    Text("ContentTitle", content.transform, Names[i].ToUpperInvariant(), new Vector2(.05f, .86f), new Vector2(.95f, .98f));
                    var imageSlot = Rect("AssignableContentImage", content.transform, new Vector2(.03f, .03f), new Vector2(.97f, .84f));
                    imageSlot.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
                    tabList[i] = (tabButton, content, selected);
                }

                var serialized = new SerializedObject(controller);
                var tabs = serialized.FindProperty("tabs");
                tabs.arraySize = 4;
                for (var i = 0; i < 4; i++)
                {
                    var item = tabs.GetArrayElementAtIndex(i);
                    item.FindPropertyRelative("tab").enumValueIndex = i;
                    item.FindPropertyRelative("tabButton").objectReferenceValue = tabList[i].button;
                    item.FindPropertyRelative("contentRoot").objectReferenceValue = tabList[i].content;
                    item.FindPropertyRelative("selectedState").objectReferenceValue = tabList[i].selected;
                    tabList[i].content.SetActive(i == 0);
                    tabList[i].selected.SetActive(i == 0);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();

                ConnectLaunchButtons(home, controller);
                panel.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Four Play Mode buttons now open one shared panel on their matching tabs.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void ConnectLaunchButtons(MenuHomeController home, PlayModePanelController panel)
        {
            var serialized = new SerializedObject(home);
            var actions = serialized.FindProperty("playModeActions");
            for (var i = 0; i < Mathf.Min(4, actions.arraySize); i++)
            {
                var action = actions.GetArrayElementAtIndex(i);
                var button = action.FindPropertyRelative("button").objectReferenceValue as Button;
                if (button == null) continue;
                var calls = action.FindPropertyRelative("action").FindPropertyRelative("m_PersistentCalls.m_Calls");
                if (calls != null) calls.arraySize = 0;

                var launcher = button.GetComponent<PlayModeLaunchButton>() ?? button.gameObject.AddComponent<PlayModeLaunchButton>();
                Set(launcher, "button", button);
                Set(launcher, "panel", panel);
                SetEnum(launcher, "targetTab", i);

                var hover = button.GetComponent<HoverSpriteButton>() ?? button.gameObject.AddComponent<HoverSpriteButton>();
                Set(hover, "targetImage", button.GetComponent<Image>());
                Set(hover, "normalSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Normal.png"));
                Set(hover, "hoverSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Hover.png"));
                ConfigureHoverText(hover, button);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        static Button Button(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var go = Rect(name, parent, min, max); var image = go.AddComponent<Image>(); var button = go.AddComponent<Button>(); button.targetGraphic = image;
            Text("Label", go.transform, label, new Vector2(.04f, .05f), new Vector2(.96f, .95f));
            return button;
        }

        static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max)
        {
            var go = Rect(name, parent, min, max); var text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true; text.fontSizeMin = 10; text.fontSizeMax = 24; return text;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static void ConfigureHoverText(HoverSpriteButton hover, Button button)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) return;
            label.color = NormalTextBrown;
            var so = new SerializedObject(hover);
            so.FindProperty("targetText").objectReferenceValue = label;
            so.FindProperty("normalTextColor").colorValue = NormalTextBrown;
            so.FindProperty("hoverTextColor").colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void Set(Object target, string property, Object value) { var so = new SerializedObject(target); var p = so.FindProperty(property); if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); } }
        static void SetEnum(Object target, string property, int value) { var so = new SerializedObject(target); var p = so.FindProperty(property); if (p != null) { p.enumValueIndex = value; so.ApplyModifiedPropertiesWithoutUndo(); } }
    }
}
#endif
