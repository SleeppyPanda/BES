#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class TurnBattlePanelMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string SessionKey = "BES.TurnBattlePanel.v1";
        static readonly bool AutoRunMigration = false;
        static readonly Color Clear = new(1f, 1f, 1f, 0f);
        static readonly Color Cream = new(.96f, .92f, .82f, 1f);
        static readonly Color Brown = new(.35f, .16f, .13f, 1f);
        static readonly Color Green = new(.35f, .8f, .35f, 1f);

        struct UnitParts { public GameObject root; public Button target; public Image body; public Image portrait; public Slider hp; public TMP_Text hpText; }
        struct OrderParts { public GameObject root; public Image portrait; public GameObject ally; public GameObject enemy; }

        static TurnBattlePanelMigration()
        {
            if (AutoRunMigration) EditorApplication.delayCall += RunOnce;
        }
        static void RunOnce()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true); Apply();
        }

        [MenuItem("BES/UI/Create Turn Battle Panel")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var navigator = root.GetComponent<MenuNavigator>();
                if (navigator == null) return;
                var old = Find(root.transform, "BattlePanel");
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var panel = Rect("BattlePanel", root.transform, Vector2.zero, Vector2.one);
                var background = panel.AddComponent<Image>();
                background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Mới/Battle/Demo.png");
                background.color = Color.white; background.raycastTarget = true;
                var battle = panel.AddComponent<TurnBattleUI>();

                var header = Rect("BattleHeader", panel.transform, new Vector2(.17f, .86f), new Vector2(.98f, .98f));
                var round = Text("RoundText", header.transform, "ROUND 1", new Vector2(0f, 0f), new Vector2(.28f, 1f), 32);
                var actor = Text("CurrentActorText", header.transform, "", new Vector2(.28f, 0f), new Vector2(.55f, 1f), 22);
                var speed = Button("SpeedButton", header.transform, "1X", new Vector2(.62f, .15f), new Vector2(.73f, .85f), out var speedLabel);
                var auto = Button("AutoButton", header.transform, "AUTO", new Vector2(.75f, .15f), new Vector2(.88f, .85f), out var autoLabel);
                var pause = Button("PauseButton", header.transform, "II", new Vector2(.90f, .15f), new Vector2(.98f, .85f), out _);

                var orderRail = Rect("TurnOrderRail", panel.transform, new Vector2(.015f, .08f), new Vector2(.15f, .96f));
                var orders = new List<OrderParts>();
                for (var i = 0; i < 8; i++)
                {
                    var top = .99f - i * .12f;
                    var entry = Image("TurnOrderEntry_" + i, orderRail.transform, new Vector2(.08f, top - .105f), new Vector2(.92f, top), Cream);
                    var portrait = Image("Portrait", entry.transform, new Vector2(.08f, .08f), new Vector2(.92f, .92f), Color.white);
                    portrait.raycastTarget = false;
                    var allyMark = Image("AllyMarker", entry.transform, new Vector2(0f, 0f), new Vector2(.12f, 1f), Green).gameObject;
                    var enemyMark = Image("EnemyMarker", entry.transform, new Vector2(.88f, 0f), new Vector2(1f, 1f), new Color(.75f, .2f, .2f, 1f)).gameObject;
                    orders.Add(new OrderParts { root = entry.gameObject, portrait = portrait, ally = allyMark, enemy = enemyMark });
                }

                var allyParts = new List<UnitParts>();
                var enemyParts = new List<UnitParts>();
                for (var i = 0; i < 4; i++)
                {
                    var x = .18f + i * .145f;
                    allyParts.Add(BuildUnit("Ally_" + i, panel.transform, new Vector2(x, .05f), new Vector2(x + .13f, .35f), false));
                }
                for (var i = 0; i < 4; i++)
                {
                    var column = i % 2; var row = i / 2;
                    var x = .58f + column * .17f; var y = .48f + row * .20f;
                    enemyParts.Add(BuildUnit("Enemy_" + i, panel.transform, new Vector2(x, y), new Vector2(x + .15f, y + .18f), true));
                }

                var skillPanel = Rect("SkillPanel", panel.transform, new Vector2(.67f, .04f), new Vector2(.985f, .34f));
                var hint = Text("SelectionHint", skillPanel.transform, "SELECT A SKILL", new Vector2(0f, .80f), new Vector2(1f, 1f), 18);
                var skillButtons = new List<Button>(); var skillIcons = new List<Image>(); var skillLabels = new List<TMP_Text>();
                for (var i = 0; i < 4; i++)
                {
                    var col = i % 2; var row = i / 2;
                    var min = new Vector2(col * .5f + .02f, .40f - row * .38f);
                    var max = new Vector2(col * .5f + .48f, .76f - row * .38f);
                    var button = Button("SkillButton_" + i, skillPanel.transform, "SKILL " + (i + 1), min, max, out var label);
                    var icon = Image("SkillIcon", button.transform, new Vector2(.04f, .10f), new Vector2(.34f, .90f), Color.white);
                    label.rectTransform.anchorMin = new Vector2(.35f, .05f); label.rectTransform.anchorMax = new Vector2(.96f, .95f);
                    skillButtons.Add(button); skillIcons.Add(icon); skillLabels.Add(label);
                }

                var pausePanel = Image("PauseOverlay", panel.transform, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, .6f)).gameObject;
                Text("PausedText", pausePanel.transform, "PAUSED", new Vector2(.35f, .42f), new Vector2(.65f, .58f), 52);
                pausePanel.SetActive(false);

                Set(battle, "roundText", round); Set(battle, "currentActorText", actor);
                Set(battle, "speedButton", speed); Set(battle, "speedText", speedLabel);
                Set(battle, "autoButton", auto); Set(battle, "autoText", autoLabel);
                Set(battle, "pauseButton", pause); Set(battle, "pausePanel", pausePanel);
                Set(battle, "skillPanel", skillPanel); Set(battle, "selectionHintText", hint);
                SetObjectList(battle, "skillButtons", skillButtons); SetObjectList(battle, "skillIcons", skillIcons); SetObjectList(battle, "skillLabels", skillLabels);
                SetUnits(battle, "allies", allyParts, true); SetUnits(battle, "enemies", enemyParts, false);
                SetOrders(battle, orders);
                AddScreen(navigator, panel, skillButtons[0]);
                panel.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Configurable Speed-based turn battle panel created and wired to MenuScreenId.Battle.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static UnitParts BuildUnit(string name, Transform parent, Vector2 min, Vector2 max, bool enemy)
        {
            var root = Rect(name, parent, min, max); var hit = root.AddComponent<Image>(); hit.color = Clear;
            Button target = null; if (enemy) { target = root.AddComponent<Button>(); target.targetGraphic = hit; }
            var body = Image("AssignableBattlefieldSprite", root.transform, new Vector2(.05f, .20f), new Vector2(.95f, 1f), Color.white); body.preserveAspect = true;
            var portrait = Image("AssignablePortrait", root.transform, new Vector2(.02f, 0f), new Vector2(.26f, .22f), Color.white); portrait.preserveAspect = true;
            var hp = HealthSlider("HealthBar", root.transform, new Vector2(.28f, .04f), new Vector2(.98f, .16f));
            var hpText = Text("HealthText", root.transform, "100/100", new Vector2(.30f, .13f), new Vector2(.98f, .23f), 13);
            return new UnitParts { root = root, target = target, body = body, portrait = portrait, hp = hp, hpText = hpText };
        }

        static void SetUnits(TurnBattleUI battle, string field, List<UnitParts> units, bool player)
        {
            var names = player ? new[] { "Astra", "Blaze", "Terra", "Zephyr" } : new[] { "Wyrmling", "Golem", "Shade", "Titan" };
            var hp = player ? new[] { 130, 110, 180, 95 } : new[] { 120, 150, 90, 210 };
            var attack = player ? new[] { 25, 32, 20, 40 } : new[] { 22, 25, 35, 28 };
            var defense = player ? new[] { 10, 7, 18, 5 } : new[] { 8, 12, 4, 16 };
            var speed = player ? new[] { 18, 25, 10, 30 } : new[] { 14, 11, 23, 8 };
            var so = new SerializedObject(battle); var list = so.FindProperty(field); list.arraySize = 4;
            for (var i = 0; i < 4; i++)
            {
                var item = list.GetArrayElementAtIndex(i); var definition = item.FindPropertyRelative("definition");
                definition.FindPropertyRelative("id").stringValue = (player ? "hero_" : "enemy_") + (i + 1);
                definition.FindPropertyRelative("displayName").stringValue = names[i];
                definition.FindPropertyRelative("maxHealth").intValue = hp[i]; definition.FindPropertyRelative("attack").intValue = attack[i];
                definition.FindPropertyRelative("defense").intValue = defense[i]; definition.FindPropertyRelative("speed").intValue = speed[i];
                var skills = definition.FindPropertyRelative("skills"); skills.arraySize = player ? 4 : 1;
                for (var s = 0; s < skills.arraySize; s++)
                {
                    var skill = skills.GetArrayElementAtIndex(s); skill.FindPropertyRelative("id").stringValue = "skill_" + (s + 1);
                    skill.FindPropertyRelative("displayName").stringValue = s == 0 ? "Attack" : "Skill " + (s + 1);
                    skill.FindPropertyRelative("powerMultiplier").floatValue = new[] { 1f, 1.35f, 1.7f, 2.2f }[s];
                }
                item.FindPropertyRelative("root").objectReferenceValue = units[i].root;
                item.FindPropertyRelative("targetButton").objectReferenceValue = units[i].target;
                item.FindPropertyRelative("battlefieldImage").objectReferenceValue = units[i].body;
                item.FindPropertyRelative("portrait").objectReferenceValue = units[i].portrait;
                item.FindPropertyRelative("healthBar").objectReferenceValue = units[i].hp;
                item.FindPropertyRelative("healthText").objectReferenceValue = units[i].hpText;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetOrders(TurnBattleUI battle, List<OrderParts> values)
        {
            var so = new SerializedObject(battle); var list = so.FindProperty("turnOrderEntries"); list.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) { var e = list.GetArrayElementAtIndex(i); e.FindPropertyRelative("root").objectReferenceValue = values[i].root; e.FindPropertyRelative("portrait").objectReferenceValue = values[i].portrait; e.FindPropertyRelative("playerMarker").objectReferenceValue = values[i].ally; e.FindPropertyRelative("enemyMarker").objectReferenceValue = values[i].enemy; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddScreen(MenuNavigator navigator, GameObject panel, Button focus)
        {
            var so = new SerializedObject(navigator); var list = so.FindProperty("screens"); var index = -1;
            for (var i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex == (int)MenuScreenId.Battle) index = i;
            if (index < 0) { index = list.arraySize; list.arraySize++; }
            var item = list.GetArrayElementAtIndex(index); item.FindPropertyRelative("id").enumValueIndex = (int)MenuScreenId.Battle; item.FindPropertyRelative("panel").objectReferenceValue = panel; item.FindPropertyRelative("defaultFocus").objectReferenceValue = focus;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Rect(string name, Transform parent, Vector2 min, Vector2 max) { var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI"); var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; return go; }
        static Image Image(string name, Transform parent, Vector2 min, Vector2 max, Color color) { var image = Rect(name, parent, min, max).AddComponent<Image>(); image.color = color; return image; }
        static TMP_Text Text(string name, Transform parent, string value, Vector2 min, Vector2 max, float size) { var text = Rect(name, parent, min, max).AddComponent<TextMeshProUGUI>(); text.text = value; text.color = Brown; text.fontSize = size; text.enableAutoSizing = true; text.fontSizeMin = 8; text.fontSizeMax = size; text.alignment = TextAlignmentOptions.Center; return text; }
        static Button Button(string name, Transform parent, string value, Vector2 min, Vector2 max, out TMP_Text label) { var image = Image(name, parent, min, max, Cream); var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; label = Text("Label", image.transform, value, Vector2.zero, Vector2.one, 18); return button; }
        static Slider HealthSlider(string name, Transform parent, Vector2 min, Vector2 max) { var root = Rect(name, parent, min, max); var background = Image("Background", root.transform, Vector2.zero, Vector2.one, Brown); var fillArea = Rect("Fill Area", root.transform, new Vector2(.02f, .15f), new Vector2(.98f, .85f)); var fill = Image("Fill", fillArea.transform, Vector2.zero, Vector2.one, Green); var slider = root.AddComponent<Slider>(); slider.targetGraphic = background; slider.fillRect = fill.rectTransform; slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight; slider.interactable = false; return slider; }
        static Transform Find(Transform root, string name) { foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child; return null; }
        static void Set(Object target, string property, Object value) { var so = new SerializedObject(target); var p = so.FindProperty(property); if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); } }
        static void SetObjectList<T>(Object target, string property, List<T> values) where T : Object { var so = new SerializedObject(target); var list = so.FindProperty(property); list.arraySize = values.Count; for (var i = 0; i < values.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
#endif
