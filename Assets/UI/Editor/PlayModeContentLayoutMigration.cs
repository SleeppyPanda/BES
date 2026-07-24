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
    public static class PlayModeContentLayoutMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.PlayModeContentLayouts.v1";

        static PlayModeContentLayoutMigration() => EditorApplication.delayCall += RunOnce;
        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            Apply();
        }

        [MenuItem("BES/UI/Build Three Play Mode Content Layouts")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var resonance = Find(root.transform, "Content_0_Resonance Sanctum");
                var rift = Find(root.transform, "Content_2_Rift of the Hunt");
                var divine = Find(root.transform, "Content_3_Divine Remnant");
                if (resonance == null || rift == null || divine == null) return;
                BuildResonance(resonance);
                BuildRift(rift);
                BuildDivine(divine);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Resonance, Rift and Divine Remnant content layouts created and wired.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void BuildResonance(Transform root)
        {
            Clear(root);
            var controller = root.gameObject.AddComponent<ResonanceSubTabController>();
            var names = new[] { "Sanctum of Lost Echoes", "Sanctum of Ascension", "Sanctum of Insight", "Sanctum of Forging" };
            var bindings = new (Button button, GameObject list, GameObject selected)[4];
            for (var i = 0; i < 4; i++)
            {
                var top = .95f - i * .22f;
                var button = Button("SubTab_" + i, root, names[i], new Vector2(.02f, top - .16f), new Vector2(.28f, top));
                var selected = Rect("Selected", button.transform, Vector2.zero, Vector2.one);
                selected.AddComponent<Image>().color = new Color(1f, 1f, 1f, .08f);
                selected.transform.SetAsFirstSibling();

                var list = Rect("TabList_" + i + "_" + names[i], root, new Vector2(.32f, .04f), new Vector2(.98f, .96f));
                var scroll = list.AddComponent<ScrollRect>();
                var viewport = Rect("Viewport", list.transform, Vector2.zero, Vector2.one);
                viewport.AddComponent<Image>().color = new Color(1f, 1f, 1f, .001f);
                viewport.AddComponent<RectMask2D>();
                var content = Rect("ListContent", viewport.transform, new Vector2(0, 1), new Vector2(1, 1));
                var contentRect = content.GetComponent<RectTransform>(); contentRect.pivot = new Vector2(.5f, 1); contentRect.sizeDelta = new Vector2(0, 720);
                var layout = content.AddComponent<VerticalLayoutGroup>(); layout.spacing = 18; layout.padding = new RectOffset(12, 12, 12, 12); layout.childForceExpandHeight = false; layout.childControlHeight = false;
                for (var row = 0; row < 4; row++)
                {
                    var entry = Rect("AssignableListEntry_" + row, content.transform, Vector2.zero, Vector2.one);
                    entry.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 150);
                    entry.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
                }
                scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = contentRect; scroll.horizontal = false; scroll.vertical = true;
                list.SetActive(i == 0); selected.SetActive(i == 0);
                bindings[i] = (button, list, selected);
            }
            var so = new SerializedObject(controller); var tabs = so.FindProperty("tabs"); tabs.arraySize = 4;
            for (var i = 0; i < 4; i++)
            {
                var item = tabs.GetArrayElementAtIndex(i); item.FindPropertyRelative("tabName").stringValue = names[i];
                item.FindPropertyRelative("button").objectReferenceValue = bindings[i].button;
                item.FindPropertyRelative("listRoot").objectReferenceValue = bindings[i].list;
                item.FindPropertyRelative("selectedState").objectReferenceValue = bindings[i].selected;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildRift(Transform root)
        {
            Clear(root);
            for (var i = 0; i < 4; i++)
            {
                var x0 = .02f + i * .245f;
                var card = Rect("StageCard_" + i, root, new Vector2(x0, .48f), new Vector2(x0 + .225f, .96f));
                var view = card.AddComponent<RiftStageCardView>();
                var image = Rect("AssignableStageImage", card.transform, new Vector2(0, .35f), new Vector2(1, 1)); image.AddComponent<Image>().color = new Color(1, 1, 1, 0);
                var play = Button("PlayButton", card.transform, "PLAY", new Vector2(.28f, .38f), new Vector2(.72f, .52f));
                var title = Text("StageTitle", card.transform, "STAGE " + (i + 1), new Vector2(.04f, .27f), new Vector2(.96f, .36f));
                var lines = new List<TMP_Text>();
                for (var row = 0; row < 3; row++) lines.Add(Text("Description_" + row, card.transform, "✦ Description", new Vector2(.04f, .17f - row * .08f), new Vector2(.96f, .25f - row * .08f)));
                Set(view, "stageImage", image.GetComponent<Image>()); Set(view, "playButton", play); Set(view, "titleText", title); SetList(view, "descriptionLines", lines);
            }
            var buff = Rect("BuffPanel", root, new Vector2(.02f, .04f), new Vector2(.72f, .40f)); buff.AddComponent<Image>().color = new Color(1, 1, 1, 0); Text("BuffText", buff.transform, "◆ BUFF", new Vector2(.03f, .78f), new Vector2(.97f, .97f));
            Text("Timer", root, "◷ 2 days 20 hours", new Vector2(.75f, .36f), new Vector2(.98f, .44f));
        }

        static void BuildDivine(Transform root)
        {
            Clear(root);
            var previous = Button("Previous", root, "‹", new Vector2(0, .40f), new Vector2(.06f, .60f));
            var next = Button("Next", root, "›", new Vector2(.94f, .40f), new Vector2(1, .60f));
            var scrollGo = Rect("EnemySectionScroll", root, new Vector2(.07f, .04f), new Vector2(.93f, .96f));
            var scroll = scrollGo.AddComponent<ScrollRect>(); scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = true;
            var viewport = Rect("Viewport", scrollGo.transform, Vector2.zero, Vector2.one); viewport.AddComponent<Image>().color = new Color(1, 1, 1, .001f); viewport.AddComponent<RectMask2D>();
            var content = Rect("SectionList", viewport.transform, new Vector2(0, 0), new Vector2(0, 1));
            var contentRect = content.GetComponent<RectTransform>(); contentRect.pivot = new Vector2(0, .5f); contentRect.sizeDelta = new Vector2(1320, 0);
            var layout = content.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 24; layout.padding = new RectOffset(12, 12, 12, 12); layout.childControlWidth = false; layout.childForceExpandWidth = false;
            scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = contentRect;

            var sections = new List<(RectTransform root, Image frame, Image enemy, List<Image> drops, Button select)>();
            for (var i = 0; i < 4; i++)
            {
                var section = Rect("EnemySection_" + (i + 1), content.transform, Vector2.zero, Vector2.one);
                var sectionRect = section.GetComponent<RectTransform>(); sectionRect.sizeDelta = new Vector2(300, 0);
                var frame = section.AddComponent<Image>(); frame.color = new Color(1, 1, 1, 0);
                var variantB = i % 2 == 1;
                var enemy = Rect("AssignableEnemyImage", section.transform, new Vector2(.08f, variantB ? .16f : .35f), new Vector2(.92f, variantB ? .78f : .97f)); enemy.AddComponent<Image>().color = new Color(1, 1, 1, 0);
                var drops = new List<Image>();
                for (var d = 0; d < 3; d++)
                {
                    var x0 = .18f + d * .23f;
                    var drop = Rect("DropSlot_" + (d + 1), section.transform, new Vector2(x0, variantB ? .80f : .06f), new Vector2(x0 + .18f, variantB ? .96f : .22f));
                    drops.Add(drop.AddComponent<Image>()); drops[d].color = new Color(1, 1, 1, 0);
                }
                var select = section.AddComponent<Button>(); select.targetGraphic = frame;
                sections.Add((sectionRect, frame, enemy.GetComponent<Image>(), drops, select));
            }
            var controller = root.gameObject.AddComponent<DivineRemnantCarousel>();
            Set(controller, "scrollRect", scroll); Set(controller, "content", contentRect); Set(controller, "previousButton", previous); Set(controller, "nextButton", next);
            var so = new SerializedObject(controller); var list = so.FindProperty("sections"); list.arraySize = 4;
            for (var i = 0; i < 4; i++)
            {
                var item = list.GetArrayElementAtIndex(i); item.FindPropertyRelative("enemyId").stringValue = "enemy_" + (i + 1); item.FindPropertyRelative("layoutVariant").enumValueIndex = i % 2;
                item.FindPropertyRelative("sectionRoot").objectReferenceValue = sections[i].root; item.FindPropertyRelative("frameImage").objectReferenceValue = sections[i].frame; item.FindPropertyRelative("enemyImage").objectReferenceValue = sections[i].enemy; item.FindPropertyRelative("selectButton").objectReferenceValue = sections[i].select;
                var drops = item.FindPropertyRelative("dropSlots"); drops.arraySize = 3; for (var d = 0; d < 3; d++) drops.GetArrayElementAtIndex(d).objectReferenceValue = sections[i].drops[d];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Clear(Transform root) { for (var i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject); foreach(var c in root.GetComponents<MonoBehaviour>()) if(c is ResonanceSubTabController || c is DivineRemnantCarousel || c is RiftStageCardView) Object.DestroyImmediate(c); }
        static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max) { var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI"); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; return go; }
        static Button Button(string name, Transform parent, string label, Vector2 min, Vector2 max) { var go = Rect(name, parent, min, max); var image = go.AddComponent<Image>(); var button = go.AddComponent<Button>(); button.targetGraphic = image; Text("Label", go.transform, label, new Vector2(.04f, .05f), new Vector2(.96f, .95f)); return button; }
        static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max) { var go = Rect(name, parent, min, max); var text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true; text.fontSizeMin = 8; text.fontSizeMax = 22; return text; }
        static Transform Find(Transform root, string name) { foreach(var child in root.GetComponentsInChildren<Transform>(true)) if(child.name == name) return child; return null; }
        static void Set(Object target, string property, Object value) { var so = new SerializedObject(target); var p = so.FindProperty(property); if(p != null){p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();} }
        static void SetList<T>(Object target, string property, List<T> values) where T:Object { var so=new SerializedObject(target);var p=so.FindProperty(property);p.arraySize=values.Count;for(var i=0;i<values.Count;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
