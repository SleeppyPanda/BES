#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    // Auto-run disabled: manual UI edits must not be overwritten on editor refresh.
    public static class PlayModePanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.PlayModeSharedPanel.v3";
        static readonly Color NormalTextBrown = new Color(0.35f, 0.16f, 0.13f, 1f);

        struct TabSpec
        {
            public string tabName;
            public PlayModeTab tab;
            public string contentName;
            public string backgroundPath;
            public string artFolder;
            public bool build;
        }

        static readonly TabSpec[] Tabs =
        {
            new TabSpec
            {
                tabName = "Bí Cảnh Thánh Di Vật",
                tab = PlayModeTab.SanctumOfRelics,
                contentName = "Content_0_SanctumOfRelics",
                backgroundPath = "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/playmode/Bí Cảnh Thánh Di Vật.png",
                artFolder = "Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Bí Cảnh Thánh Di Vật",
                build = true
            },
            new TabSpec
            {
                tabName = "Arena of Echoes",
                tab = PlayModeTab.ArenaOfEchoes,
                contentName = "Content_1_ArenaOfEchoes",
                backgroundPath = "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/playmode/Bí cảnh nâng EXP Character x Vũ Khí x Vàng x TĂNG ĐỘ THIỆN CẢM.png",
                artFolder = "Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Bí cảnh nâng EXP Character x Vũ Khí x Vàng x TĂNG ĐỘ THIỆN CẢM",
                build = true
            },
            new TabSpec
            {
                tabName = "Đột Phá Nhân Vật",
                tab = PlayModeTab.CharacterBreakthrough,
                contentName = "Content_2_CharacterBreakthrough",
                backgroundPath = "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/playmode/Đột phá nhân vật theo cấp.png",
                artFolder = "Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Đột phá nhân vật theo cấp",
                build = true
            },
            new TabSpec
            {
                tabName = "Đột Phá Vũ Khí",
                tab = PlayModeTab.WeaponBreakthrough,
                contentName = "Content_3_WeaponBreakthrough",
                backgroundPath = "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/playmode/Đột phá vũ khí.png",
                artFolder = "Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Đột phá vũ khí",
                build = true
            },
            new TabSpec
            {
                tabName = "Đánh Quái",
                tab = PlayModeTab.RiftOfTheHunt,
                contentName = "Content_4_RiftOfTheHunt",
                backgroundPath = "Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/playmode/Mode Content Đánh Qúai.png",
                artFolder = "Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Đánh quái thu nguyen liệu chỉ định nâng nhân vật",
                build = true
            }
        };

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

        [MenuItem("BES/UI/Build Five Play Mode Tabs")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var modalLayer = Find(root.transform, "ModalLayer");
                if (modalLayer == null) return;

                EnsurePanelRoot(modalLayer);
                var panel = Find(modalLayer, "PlayModePanel");
                var frame = panel != null ? Find(panel, "PanelFrame") : null;
                if (panel == null || frame == null) return;

                RemoveLegacyPlayModeTabs(panel, frame);

                var controller = panel.gameObject.GetComponent<PlayModePanelController>();
                if (controller == null) return;

                var tabBindings = EnsureTabs(controller, panel, frame);

                for (var i = 0; i < Tabs.Length; i++)
                {
                    var spec = Tabs[i];
                    var binding = tabBindings[i];
                    if (binding.contentRoot == null) continue;

                    var contentImage = binding.contentRoot.GetComponent<Image>();
                    if (contentImage == null) contentImage = binding.contentRoot.gameObject.AddComponent<Image>();
                    AssignSprite(contentImage, spec.backgroundPath);

                    if (spec.build) BuildTabContent(binding.contentRoot.transform, spec, i);
                }

                ConnectLaunchButtons(root);
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Five Play Mode tabs wired with localized content and Arena of Echoes added.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void EnsurePanelRoot(Transform modalLayer)
        {
            var panel = Find(modalLayer, "PlayModePanel");
            if (panel != null) return;

            panel = Rect("PlayModePanel", modalLayer, Vector2.zero, Vector2.one).transform;
            var bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = SpriteAt("Assets/Art Ui/Tranning Mode/Background Overall.png");
            bg.color = Color.white;

            var frame = Rect("PanelFrame", panel, new Vector2(.17f, .12f), new Vector2(.96f, .90f));
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = SpriteAt("Assets/Art Ui/Tranning Mode/Background panel.png");
            frameImage.color = Color.white;

            var close = Button("CloseButton", frame.transform, "×", new Vector2(.91f, .88f), new Vector2(.99f, .99f));
            var controller = panel.gameObject.AddComponent<PlayModePanelController>();
            Set(controller, "panelRoot", panel.gameObject);
            Set(controller, "closeButton", close);
        }

        static void RemoveLegacyPlayModeTabs(Transform panel, Transform frame)
        {
            var keep = new HashSet<string>();
            for (var i = 0; i < Tabs.Length; i++)
            {
                keep.Add("Tab_" + i + "_" + Tabs[i].tabName);
            }
            keep.Add("PanelFrame");
            keep.Add("Background");
            keep.Add("CloseButton");
            keep.Add("TabList");

            var keepContents = new HashSet<string>();
            for (var i = 0; i < Tabs.Length; i++)
                keepContents.Add(Tabs[i].contentName);

            var toDestroy = new List<GameObject>();
            foreach (var t in panel.GetComponentsInChildren<Transform>(true))
            {
                if (t == panel) continue;
                if (t == frame) continue;
                if (t.name.StartsWith("Tab_") && !keep.Contains(t.name))
                    toDestroy.Add(t.gameObject);
            }
            foreach (var t in frame.GetComponentsInChildren<Transform>(true))
            {
                if (t == frame) continue;
                if (t.name.StartsWith("Content_") && !keepContents.Contains(t.name))
                    toDestroy.Add(t.gameObject);
            }
            foreach (var go in toDestroy) Object.DestroyImmediate(go);
        }

        static List<PlayModeTabBinding> EnsureTabs(PlayModePanelController controller, Transform panel, Transform frame)
        {
            var serialized = new SerializedObject(controller);
            var tabsProp = serialized.FindProperty("tabs");
            tabsProp.arraySize = Tabs.Length;
            var bindings = new List<PlayModeTabBinding>(Tabs.Length);

            for (var i = 0; i < Tabs.Length; i++)
            {
                var spec = Tabs[i];
                var tabButtonName = "Tab_" + i + "_" + spec.tabName;
                var contentName = spec.contentName;
                var selectedName = "SelectedState";

                var tabButtonGo = Find(panel, tabButtonName);
                if (tabButtonGo == null)
                {
                    var top = .84f - i * .14f;
                    tabButtonGo = Button(tabButtonName, panel, spec.tabName, new Vector2(.02f, top - .10f), new Vector2(.17f, top)).transform;
                    var hover = tabButtonGo.gameObject.AddComponent<HoverSpriteButton>();
                    Set(hover, "targetImage", tabButtonGo.GetComponent<Image>());
                    Set(hover, "normalSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Normal.png"));
                    Set(hover, "hoverSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Hover.png"));
                    ConfigureHoverText(hover, tabButtonGo.GetComponent<Button>());
                    var selected = Rect(selectedName, tabButtonGo, Vector2.zero, Vector2.one);
                    var selectedImage = selected.gameObject.AddComponent<Image>();
                    selectedImage.sprite = SpriteAt("Assets/Art Ui/Play Mode/Button Hover.png");
                    selected.transform.SetAsFirstSibling();
                }
                else
                {
                    var label = tabButtonGo.GetComponentInChildren<TMP_Text>(true);
                    if (label != null) label.text = spec.tabName;
                }

                var tabButton = tabButtonGo.GetComponent<Button>();
                if (tabButton == null) tabButton = tabButtonGo.gameObject.AddComponent<Button>();

                var contentRoot = Find(frame, contentName);
                if (contentRoot == null)
                {
                    contentRoot = Rect(contentName, frame, new Vector2(.04f, .06f), new Vector2(.96f, .88f)).transform;
                    contentRoot.gameObject.AddComponent<Image>();
                }

                var selectedState = FindChild(tabButtonGo, selectedName);
                if (selectedState == null)
                {
                    selectedState = Rect(selectedName, tabButtonGo, Vector2.zero, Vector2.one).transform;
                    var img = selectedState.gameObject.AddComponent<Image>();
                    img.sprite = SpriteAt("Assets/Art Ui/Play Mode/Button Hover.png");
                    selectedState.transform.SetAsFirstSibling();
                }

                var item = tabsProp.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("tab").enumValueIndex = (int)spec.tab;
                item.FindPropertyRelative("tabButton").objectReferenceValue = tabButton;
                item.FindPropertyRelative("contentRoot").objectReferenceValue = contentRoot.gameObject;
                item.FindPropertyRelative("selectedState").objectReferenceValue = selectedState.gameObject;

                bindings.Add(new PlayModeTabBinding
                {
                    tab = spec.tab,
                    tabButton = tabButton,
                    contentRoot = contentRoot.gameObject,
                    selectedState = selectedState.gameObject
                });
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return bindings;
        }

        static void BuildTabContent(Transform root, TabSpec spec, int index)
        {
            Clear(root);
            switch (spec.tab)
            {
                case PlayModeTab.SanctumOfRelics: BuildDomainRows(root, spec, 4, true); break;
                case PlayModeTab.ArenaOfEchoes: BuildDomainRows(root, spec, 4, true); break;
                case PlayModeTab.CharacterBreakthrough: BuildSingleStage(root, spec, "ĐỘT PHÁ NHÂN VẬT"); break;
                case PlayModeTab.WeaponBreakthrough: BuildSingleStage(root, spec, "ĐỘT PHÁ VŨ KHÍ"); break;
                case PlayModeTab.RiftOfTheHunt: BuildRift(root, spec); break;
            }
            root.gameObject.SetActive(index == 0);
        }

        static void BuildDomainRows(Transform root, TabSpec spec, int rowCount, bool includeBuffCard)
        {
            for (var i = 0; i < rowCount; i++)
            {
                var row = Rect("DomainRow_" + i, root, new Vector2(0.04f, 1f - (i + 1) * 0.20f - 0.02f), new Vector2(0.96f, 1f - i * 0.20f));
                var rowRect = row.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(0, 120);
                var rowImage = row.GetComponent<Image>();
                rowImage.sprite = SpriteAt(PathAt(spec.artFolder, "Group 427322923.png"));
                if (rowImage.sprite == null) rowImage.sprite = SpriteAt(PathAt(spec.artFolder, "Group 427322774.png"));
                rowImage.color = rowImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.95f);
                rowImage.preserveAspect = false;

                var title = Text("DomainTitle", row.transform, "Bí Cảnh " + (i + 1), new Vector2(0.04f, 0.62f), new Vector2(0.36f, 0.92f));
                Text("BuffDescription", row.transform, "+ 5% Tấn công cho cả đội\n+ 3% HP tối đa", new Vector2(0.04f, 0.08f), new Vector2(0.55f, 0.62f));
                Text("EnergyCost", row.transform, "20", new Vector2(0.88f, 0.55f), new Vector2(0.97f, 0.85f));
                var enter = Button("EnterButton", row.transform, "ENTER", new Vector2(0.74f, 0.18f), new Vector2(0.86f, 0.50f));

                var entry = row.gameObject.AddComponent<SanctumDomainEntry>();
                SetString(entry, "domainId", spec.tab + "_0" + (i + 1));
                SetInt(entry, "energyCost", 20);
                Set(entry, "titleText", title);
                Set(entry, "enterButton", enter);
                Set(entry, "energyCostText", row.transform.Find("EnergyCost")?.GetComponent<TMP_Text>());
                WireEnterToPlayMode(enter, "play_" + spec.tab.ToString().ToLowerInvariant() + "_0" + (i + 1));
            }

            if (includeBuffCard)
            {
                var buff = Rect("BuffPanel", root, new Vector2(0.02f, 0.04f), new Vector2(0.72f, 0.42f));
                var img = buff.gameObject.AddComponent<Image>();
                img.sprite = SpriteAt("Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Đánh quái thu nguyen liệu chỉ định nâng nhân vật/Vật phẩm tỷ lệ rơi.png");
                if (img.sprite == null) img.color = new Color(1, 1, 1, 0.92f);
                else img.color = Color.white;
                Text("BuffText", buff.transform, "BUFF HIỆN HÀNH", new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.97f));
            }

            Text("Timer", root, "◷ 2 ngày 20 giờ", new Vector2(0.75f, 0.36f), new Vector2(0.98f, 0.44f));
            var playAll = Button("OpenBattleButton", root, "BẮT ĐẦU", new Vector2(0.75f, 0.06f), new Vector2(0.97f, 0.20f));
            WireEnterToPlayMode(playAll, "play_" + spec.tab.ToString().ToLowerInvariant() + "_main");
        }

        static void BuildSingleStage(Transform root, TabSpec spec, string title)
        {
            var row = Rect("BreakthroughRow", root, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.94f));
            var rowImage = row.gameObject.AddComponent<Image>();
            rowImage.sprite = SpriteAt(PathAt(spec.artFolder, "Group 427322774.png"));
            rowImage.color = rowImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.95f);

            Text("BreakthroughTitle", row.transform, title, new Vector2(0.04f, 0.62f), new Vector2(0.46f, 0.90f));
            Text("BuffDescription", row.transform, "Đột phá cấp nhân vật / vũ khí sử dụng tài nguyên đã sở hữu.", new Vector2(0.04f, 0.08f), new Vector2(0.55f, 0.60f));
            var enter = Button("EnterButton", row.transform, "ĐỘT PHÁ", new Vector2(0.74f, 0.18f), new Vector2(0.88f, 0.50f));
            Text("EnergyCost", row.transform, "0", new Vector2(0.89f, 0.55f), new Vector2(0.97f, 0.85f));

            var entry = row.gameObject.AddComponent<SanctumDomainEntry>();
            SetString(entry, "domainId", spec.tab + "_main");
            SetInt(entry, "energyCost", 0);
            var costText = Text("EnergyCost", enter.transform, "0", new Vector2(0.0f, 0.0f), new Vector2(0.2f, 1.0f));
            Set(entry, "energyCostText", costText);
            Set(entry, "enterButton", enter);
            WireEnterToPlayMode(enter, "play_" + spec.tab.ToString().ToLowerInvariant() + "_main");

            var buff = Rect("BuffPanel", root, new Vector2(0.02f, 0.04f), new Vector2(0.72f, 0.28f));
            var img = buff.gameObject.AddComponent<Image>();
            img.sprite = SpriteAt("Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Đánh quái thu nguyen liệu chỉ định nâng nhân vật/Vật phẩm tỷ lệ rơi.png");
            if (img.sprite == null) img.color = new Color(1, 1, 1, 0.92f);
            else img.color = Color.white;
            Text("BuffText", buff.transform, "BUFF HIỆN HÀNH", new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.97f));

            var playAll = Button("OpenBattleButton", root, "BẮT ĐẦU", new Vector2(0.75f, 0.04f), new Vector2(0.97f, 0.18f));
            WireEnterToPlayMode(playAll, "play_" + spec.tab.ToString().ToLowerInvariant() + "_main");
        }

        static void BuildRift(Transform root, TabSpec spec)
        {
            for (var i = 0; i < 4; i++)
            {
                var x0 = 0.02f + i * 0.245f;
                var card = Rect("RiftCard_" + i, root, new Vector2(x0, 0.46f), new Vector2(x0 + 0.225f, 0.96f));
                var cardRect = card.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(0, 0);
                var image = Rect("AssignableStageImage", card.transform, new Vector2(0, 0.30f), new Vector2(1, 1));
                var imgImage = image.gameObject.AddComponent<Image>();
                imgImage.sprite = SpriteAt("Assets/Art Ui/Game Việt hóa mới/Màn hình hoàn chỉnh/playmode/Đánh quái thu nguyen liệu chỉ định nâng nhân vật.png");
                imgImage.color = imgImage.sprite != null ? Color.white : new Color(1, 1, 1, 0.95f);
                var play = Button("PlayButton", card.transform, "ĐÁNH", new Vector2(0.28f, 0.05f), new Vector2(0.72f, 0.20f));
                Text("StageTitle", card.transform, "Ải " + (i + 1), new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.33f));

                var view = card.gameObject.AddComponent<RiftStageCardView>();
                SetString(view, "stageId", "play_resource_0" + (i + 1));
                Set(view, "stageImage", imgImage);
                Set(view, "playButton", play);
                Set(view, "titleText", card.transform.Find("StageTitle").GetComponent<TMP_Text>());
                WireEnterToPlayMode(play, "play_resource_0" + (i + 1));
            }
            var buff = Rect("BuffPanel", root, new Vector2(0.02f, 0.04f), new Vector2(0.72f, 0.40f));
            var img = buff.gameObject.AddComponent<Image>();
            img.sprite = SpriteAt("Assets/Art Ui/Game Việt hóa mới/Bí Cảnh/Đánh quái thu nguyen liệu chỉ định nâng nhân vật/Vật phẩm tỷ lệ rơi.png");
            if (img.sprite == null) img.color = new Color(1, 1, 1, 0.92f);
            else img.color = Color.white;
            Text("BuffText", buff.transform, "BUFF HIỆN HÀNH", new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.97f));
            Text("Timer", root, "◷ 2 ngày 20 giờ", new Vector2(0.75f, 0.30f), new Vector2(0.98f, 0.40f));
        }

        static void WireEnterToPlayMode(Button button, string stageId)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            var captured = stageId;
            button.onClick.AddListener(() => OpenPlayParty(captured));
        }

        static void OpenPlayParty(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return;
            TurnBattleUI.IsPlayModeBattle = true;
            TurnBattleUI.ActiveStageId = stageId;
            var navigator = Object.FindAnyObjectByType<MenuNavigator>();
            navigator?.Open(MenuScreenId.PlayParty);
        }

        static void ConnectLaunchButtons(GameObject root)
        {
            var home = root.GetComponentInChildren<MenuHomeController>(true);
            if (home == null) return;
            var serialized = new SerializedObject(home);
            var actions = serialized.FindProperty("playModeActions");
            for (var i = 0; i < Mathf.Min(Tabs.Length, actions.arraySize); i++)
            {
                var action = actions.GetArrayElementAtIndex(i);
                var button = action.FindPropertyRelative("button").objectReferenceValue as Button;
                if (button == null) continue;
                var calls = action.FindPropertyRelative("action").FindPropertyRelative("m_PersistentCalls.m_Calls");
                if (calls != null) calls.arraySize = 0;

                var launcher = button.GetComponent<PlayModeLaunchButton>() ?? button.gameObject.AddComponent<PlayModeLaunchButton>();
                Set(launcher, "button", button);
                Set(launcher, "panel", root.GetComponentInChildren<PlayModePanelController>(true));
                SetEnum(launcher, "targetTab", (int)Tabs[i].tab);

                var hover = button.GetComponent<HoverSpriteButton>() ?? button.gameObject.AddComponent<HoverSpriteButton>();
                Set(hover, "targetImage", button.GetComponent<Image>());
                Set(hover, "normalSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Normal.png"));
                Set(hover, "hoverSprite", SpriteAt("Assets/Art Ui/Play Mode/Button Hover.png"));
                ConfigureHoverText(hover, button);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Clear(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            foreach (var c in root.GetComponents<MonoBehaviour>())
                if (c is SanctumDomainEntry || c is RiftStageCardView) Object.DestroyImmediate(c);
        }

        static string PathAt(string folder, string file) => folder.TrimEnd('/') + "/" + file;

        static void AssignSprite(Image image, string path)
        {
            if (image == null) return;
            var sprite = SpriteAt(path);
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
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
            var go = Rect(name, parent, min, max);
            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            Text("Label", go.transform, label, new Vector2(.04f, .05f), new Vector2(.96f, .95f));
            return button;
        }

        static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max)
        {
            var go = Rect(name, parent, min, max);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value; text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true;
            text.fontSizeMin = 10; text.fontSizeMax = 24;
            return text;
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
                if (child.name == name) return child;
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

        static void Set(Object target, string property, Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void SetString(Object target, string property, string value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void SetInt(Object target, string property, int value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void SetEnum(Object target, string property, int value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.enumValueIndex = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}
#endif