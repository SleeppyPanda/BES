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
    public static class StoryModePanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string DatabasePath = "Assets/Scenes/MenuContentDatabase.asset";
        const string SessionKey = "BES.StoryModePanel.Requirements.v2";
        static readonly Color Clear = new(1, 1, 1, 0);
        static readonly Color Cream = new(.97f, .93f, .83f, 1);
        static readonly Color Brown = new(.35f, .16f, .13f, 1);

        static StoryModePanelMigration() => EditorApplication.delayCall += RunOnce;

        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            PatchRequirementDisplay();
        }

        [MenuItem("BES/UI/Patch Story Party Requirements")]
        public static void PatchRequirementDisplay()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var panel = Find(root.transform, "StoryModePanel");
                var controller = panel != null ? panel.GetComponent<StoryModePanelController>() : null;
                if (controller == null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    root = null;
                    Apply();
                    return;
                }

                var selecting = Find(panel, "CharacterSelectionPanel");
                var selectionText = Find(selecting, "PartyRequirementStatus")?.GetComponent<TMP_Text>();
                if (selectionText == null)
                    selectionText = Text("PartyRequirementStatus", selecting, "Party: 0/4", new Vector2(.49f, .345f), new Vector2(.98f, .39f), 18);
                selectionText.alignment = TextAlignmentOptions.Left;
                Set(controller, "selectionRequirementText", selectionText);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Story parties now require exactly four characters and validate stage attribute requirements.");
            }
            finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("BES/UI/Create Story Mode Party Screen")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var navigator = root.GetComponent<MenuNavigator>();
                if (navigator == null) return;
                var old = Find(root.transform, "StoryModePanel");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var screen = Rect("StoryModePanel", root.transform, Vector2.zero, Vector2.one);
                var controller = screen.AddComponent<StoryModePanelController>();
                Set(controller, "database", AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(DatabasePath));
                Set(controller, "navigator", navigator);

                var before = BuildPhase(screen.transform, "BeforeSelectionPanel", false, out var beforeCommon);
                var selecting = BuildPhase(screen.transform, "CharacterSelectionPanel", true, out var selectingCommon);
                Set(controller, "beforeSelectionPanel", before);
                Set(controller, "characterSelectionPanel", selecting);

                SetArray(controller, "chapterBackgrounds", new Object[] { beforeCommon.background, selectingCommon.background });
                SetArray(controller, "chapterTitles", new Object[] { beforeCommon.title, selectingCommon.title });
                SetArray(controller, "chapterSummaries", new Object[] { beforeCommon.summary, selectingCommon.summary });

                Set(controller, "openSelectionButton", beforeCommon.action);
                Set(controller, "beforeBackButton", beforeCommon.back);
                Set(controller, "selectionBackButton", selectingCommon.back);
                Set(controller, "confirmPartyButton", selectingCommon.action);

                SetSlots(controller, "beforeSlots", beforeCommon.slots);
                SetSlots(controller, "selectionSlots", selectingCommon.slots);
                SetRoster(controller, selectingCommon.rosterCards);
                AddScreenBinding(navigator, screen, beforeCommon.action);

                before.SetActive(true);
                selecting.SetActive(false);
                screen.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Story Mode screen created with main and character-selection panels.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        struct PhaseParts
        {
            public Image background;
            public TMP_Text title;
            public TMP_Text summary;
            public Button back;
            public Button action;
            public List<SlotParts> slots;
            public List<RosterParts> rosterCards;
        }

        struct SlotParts
        {
            public Button button;
            public Image portrait;
            public Image element;
            public TMP_Text name;
            public TMP_Text level;
            public GameObject empty;
        }

        struct RosterParts
        {
            public Button button;
            public Image portrait;
            public Image element;
            public TMP_Text name;
            public TMP_Text level;
            public GameObject selected;
        }

        static GameObject BuildPhase(Transform parent, string name, bool rosterOpen, out PhaseParts parts)
        {
            var phase = Rect(name, parent, Vector2.zero, Vector2.one);
            parts = new PhaseParts { slots = new List<SlotParts>(), rosterCards = new List<RosterParts>() };

            parts.background = Image("AssignableChapterBackground", phase.transform, Vector2.zero, Vector2.one, Color.white);
            parts.background.raycastTarget = false;
            var currency = Rect("CurrencyBar_Assignable", phase.transform, new Vector2(.58f, .89f), new Vector2(.98f, .98f));
            for (var i = 0; i < 3; i++) Image("Currency_" + i, currency.transform, new Vector2(i / 3f + .01f, .15f), new Vector2((i + 1) / 3f - .02f, .85f), Clear);

            parts.back = Button("BackButton", phase.transform, "BACK", new Vector2(.02f, .91f), new Vector2(.09f, .98f));
            var storyInfo = Image("StoryInformationFrame", phase.transform, new Vector2(.02f, .02f), new Vector2(rosterOpen ? .46f : .50f, .34f), Cream);
            parts.title = Text("ChapterTitle", storyInfo.transform, "STORY MODE", new Vector2(.05f, .72f), new Vector2(.95f, .96f), 30);
            parts.summary = Text("ChapterSummary", storyInfo.transform, "Chapter description can be configured in MenuContentDatabase.", new Vector2(.06f, .20f), new Vector2(.94f, .68f), 18);

            var dots = Rect("StoryProgress", storyInfo.transform, new Vector2(.12f, .04f), new Vector2(.72f, .19f));
            for (var i = 0; i < 5; i++) Image("Progress_" + i, dots.transform, new Vector2(i * .19f, .1f), new Vector2(i * .19f + .12f, .9f), Clear);

            var partyRoot = Rect("PartySlots", phase.transform, new Vector2(rosterOpen ? .49f : .52f, .02f), new Vector2(.98f, .34f));
            for (var i = 0; i < 4; i++) parts.slots.Add(BuildSlot(partyRoot.transform, i, i * .205f, i * .205f + .19f));
            var add = Button("AddPartyMember", partyRoot.transform, "+", new Vector2(.83f, .08f), new Vector2(.99f, .92f));

            if (rosterOpen)
            {
                var roster = Image("RosterPanel", phase.transform, new Vector2(.0f, .0f), new Vector2(.46f, 1f), Brown);
                var viewport = Rect("RosterViewport", roster.transform, new Vector2(.04f, .12f), new Vector2(.96f, .91f));
                viewport.AddComponent<RectMask2D>();
                var content = Rect("RosterContent", viewport.transform, Vector2.zero, Vector2.one);
                for (var i = 0; i < 9; i++)
                {
                    var column = i % 3;
                    var row = i / 3;
                    parts.rosterCards.Add(BuildRosterCard(content.transform, i,
                        new Vector2(column / 3f + .025f, .68f - row * .32f),
                        new Vector2((column + 1) / 3f - .025f, .97f - row * .32f)));
                }
                parts.action = Button("ConfirmPartyButton", roster.transform, "CONFIRM PARTY", new Vector2(.31f, .025f), new Vector2(.69f, .10f));
                add.gameObject.SetActive(false);
            }
            else if (name.StartsWith("Before"))
            {
                parts.action = add;
            }
            return phase;
        }

        static SlotParts BuildSlot(Transform parent, int index, float minX, float maxX)
        {
            var button = Button("PartySlot_" + index, parent, string.Empty, new Vector2(minX, .08f), new Vector2(maxX, .92f));
            var portrait = Image("AssignablePortrait", button.transform, new Vector2(.03f, .03f), new Vector2(.97f, .97f), Color.white);
            var element = Image("AssignableElementIcon", button.transform, new Vector2(.02f, .80f), new Vector2(.22f, .98f), Color.white);
            var name = Text("CharacterName", button.transform, string.Empty, new Vector2(.03f, .02f), new Vector2(.97f, .20f), 15);
            var level = Text("CharacterLevel", button.transform, string.Empty, new Vector2(.60f, .80f), new Vector2(.97f, .98f), 14);
            var empty = Image("EmptyState", button.transform, new Vector2(.25f, .25f), new Vector2(.75f, .75f), Color.white).gameObject;
            return new SlotParts { button = button, portrait = portrait, element = element, name = name, level = level, empty = empty };
        }

        static RosterParts BuildRosterCard(Transform parent, int index, Vector2 min, Vector2 max)
        {
            var button = Button("RosterCard_" + index, parent, string.Empty, min, max);
            var portrait = Image("AssignablePortrait", button.transform, new Vector2(.03f, .03f), new Vector2(.97f, .97f), Color.white);
            var element = Image("AssignableElementIcon", button.transform, new Vector2(.02f, .80f), new Vector2(.22f, .98f), Color.white);
            var name = Text("CharacterName", button.transform, "Character", new Vector2(.04f, .02f), new Vector2(.96f, .22f), 15);
            var level = Text("CharacterLevel", button.transform, "Lv. 1", new Vector2(.60f, .80f), new Vector2(.96f, .98f), 14);
            var selected = Image("SelectedState", button.transform, Vector2.zero, Vector2.one, new Color(1, 1, 1, .2f)).gameObject;
            selected.SetActive(false);
            return new RosterParts { button = button, portrait = portrait, element = element, name = name, level = level, selected = selected };
        }

        static void AddScreenBinding(MenuNavigator navigator, GameObject panel, Button focus)
        {
            var so = new SerializedObject(navigator);
            var screens = so.FindProperty("screens");
            var index = -1;
            for (var i = 0; i < screens.arraySize; i++)
                if (screens.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex == (int)MenuScreenId.StoryParty) index = i;
            if (index < 0) { index = screens.arraySize; screens.arraySize++; }
            var item = screens.GetArrayElementAtIndex(index);
            item.FindPropertyRelative("id").enumValueIndex = (int)MenuScreenId.StoryParty;
            item.FindPropertyRelative("panel").objectReferenceValue = panel;
            item.FindPropertyRelative("defaultFocus").objectReferenceValue = focus;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetSlots(Object target, string field, List<SlotParts> values)
        {
            var so = new SerializedObject(target); var list = so.FindProperty(field); list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                var e = list.GetArrayElementAtIndex(i); var v = values[i];
                e.FindPropertyRelative("button").objectReferenceValue = v.button;
                e.FindPropertyRelative("portrait").objectReferenceValue = v.portrait;
                e.FindPropertyRelative("elementIcon").objectReferenceValue = v.element;
                e.FindPropertyRelative("nameText").objectReferenceValue = v.name;
                e.FindPropertyRelative("levelText").objectReferenceValue = v.level;
                e.FindPropertyRelative("emptyState").objectReferenceValue = v.empty;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRoster(Object target, List<RosterParts> values)
        {
            var so = new SerializedObject(target); var list = so.FindProperty("rosterCards"); list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
            {
                var e = list.GetArrayElementAtIndex(i); var v = values[i];
                e.FindPropertyRelative("button").objectReferenceValue = v.button;
                e.FindPropertyRelative("portrait").objectReferenceValue = v.portrait;
                e.FindPropertyRelative("elementIcon").objectReferenceValue = v.element;
                e.FindPropertyRelative("nameText").objectReferenceValue = v.name;
                e.FindPropertyRelative("levelText").objectReferenceValue = v.level;
                e.FindPropertyRelative("selectedState").objectReferenceValue = v.selected;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return go;
        }

        static Image Image(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var image = Rect(name, parent, min, max).AddComponent<Image>(); image.color = color; return image;
        }

        static Button Button(string name, Transform parent, string label, Vector2 min, Vector2 max)
        {
            var image = Image(name, parent, min, max, Clear); var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            if (!string.IsNullOrEmpty(label)) Text("Label", button.transform, label, new Vector2(.03f, .04f), new Vector2(.97f, .96f), 22);
            return button;
        }

        static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
        {
            var text = Rect(name, parent, min, max).AddComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = 9; text.fontSizeMax = size; text.alignment = TextAlignmentOptions.Center; text.color = Brown; return text;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target); var property = so.FindProperty(field); if (property != null) { property.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target); var property = so.FindProperty(field); property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
