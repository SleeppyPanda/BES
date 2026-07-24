#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BES.EditorTools
{
    [InitializeOnLoad]
    public static class BESMenuHubBuilder
    {
        const string ScenePath = "Assets/Scenes/menuhub.unity";
        const string PrefabFolder = "Assets/_Project/UI/Prefabs/Screens";
        const string PrefabPath = PrefabFolder + "/MenuHub.prefab";
        const string DatabasePath = "Assets/Scenes/MenuContentDatabase.asset";
        const string BuildKey = "BES.MenuHub.AutoBuild.v6";
        static readonly Color Cream = new(0.96f, 0.92f, 0.82f, 1f);
        static readonly Color Brown = new(0.35f, 0.16f, 0.13f, 1f);
        static readonly Color Red = new(0.52f, 0.20f, 0.17f, 1f);
        static readonly Color Gold = new(0.78f, 0.65f, 0.36f, 1f);

        static BESMenuHubBuilder()
        {
            EditorApplication.delayCall += AutoBuildOnce;
        }

        static void AutoBuildOnce()
        {
            if (SessionState.GetBool(BuildKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(BuildKey, true);
            BuildAndInstall();
        }

        [MenuItem("BES/UI/Rebuild MenuHub Prefab And Scene")]
        public static void BuildAndInstall()
        {
            EnsureFolder(PrefabFolder);
            EnsureArtSprites();
            EnsureDatabase();
            var root = BuildPrefabRoot();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            InstallInScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BES] MenuHub prefab created and installed: {PrefabPath} -> {ScenePath}");
        }

        static void EnsureArtSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art Ui" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer || importer.textureType == TextureImporterType.Sprite) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        static void EnsureDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(DatabasePath);
            if (db == null) return;
            var ids = new[] { "energy", "gems", "coins" };
            var icons = new[] { "Enegry Count.png", "Gem Count.png", "Money count.png" };
            while (db.currencies.Count < 3) db.currencies.Add(new CurrencyEntry());
            for (var i = 0; i < 3; i++)
            {
                db.currencies[i].id = ids[i];
                db.currencies[i].icon = SpriteAt("Assets/Art Ui/Story Mode/" + icons[i]);
            }
            EditorUtility.SetDirty(db);
        }

        static GameObject BuildPrefabRoot()
        {
            var root = UI("MenuHub", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var navigator = root.AddComponent<MenuNavigator>();

            var background = Image("Background", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.09f, 0.08f, 0.08f));
            background.sprite = SpriteAt("Assets/Art Ui/Background.png");
            background.preserveAspect = false;

            var home = UI("HomePanel", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var controller = home.AddComponent<MenuHomeController>();
            var modalLayer = UI("ModalLayer", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            BuildHome(home.transform, controller, navigator, modalLayer.transform);
            SetNavigatorHome(navigator, home);
            return root;
        }

        static void BuildHome(Transform parent, MenuHomeController controller, MenuNavigator navigator, Transform modalLayer)
        {
            var db = AssetDatabase.LoadAssetAtPath<MenuContentDatabase>(DatabasePath);
            Set(controller, "database", db);
            Set(controller, "navigator", navigator);

            var left = Panel("PersistentLeft", parent, new Vector2(0.02f, 0.04f), new Vector2(0.51f, 0.97f), new Color(0, 0, 0, 0.10f));
            var right = Panel("PersistentRight", parent, new Vector2(0.52f, 0.04f), new Vector2(0.98f, 0.97f), new Color(0, 0, 0, 0.08f));

            BuildProfile(left.transform, controller, modalLayer);
            BuildLeftButtons(left.transform, controller, modalLayer);
            BuildCharacterAndRank(left.transform, controller, modalLayer);
            BuildCurrencies(right.transform, controller, modalLayer);
            BuildFixedBottom(right.transform, controller, modalLayer);
            BuildModes(right.transform, controller, navigator, modalLayer);
        }

        static void BuildProfile(Transform parent, MenuHomeController controller, Transform modalLayer)
        {
            var avatar = Image("AccountAvatar", parent, new Vector2(0.03f, 0.84f), new Vector2(0.14f, 0.97f), Vector2.zero, Vector2.zero, Cream);
            var nameBox = Panel("AccountInfo", parent, new Vector2(0.15f, 0.86f), new Vector2(0.50f, 0.97f), Cream);
            var name = Text("PlayerName", nameBox.transform, "TRAVELER", 28, Brown, TextAlignmentOptions.Left, new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.94f));
            var id = Text("AccountId", nameBox.transform, "ID: 000000001", 18, Brown, TextAlignmentOptions.Left, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.50f));
            var levelDisc = Panel("Level", parent, new Vector2(0.03f, 0.68f), new Vector2(0.14f, 0.82f), Brown);
            var level = Text("LevelText", levelDisc.transform, "1", 38, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            var settings = Button("Settings", parent, "⚙", new Vector2(0.51f, 0.90f), new Vector2(0.57f, 0.97f), Brown);
            var settingsPanel = Modal("SettingsPanel", modalLayer, "SETTINGS", new[] { "Music", "Sound", "Language", "Graphics" });
            Set(controller, "accountAvatar", avatar);
            Set(controller, "playerNameText", name);
            Set(controller, "accountIdText", id);
            Set(controller, "levelText", level);
            Set(controller, "settingsButton", settings);
            Set(controller, "settingsPanel", settingsPanel);
        }

        static void BuildLeftButtons(Transform parent, MenuHomeController controller, Transform modalLayer)
        {
            var letter = ArtButton("LetterButton", parent, "LETTER", "Assets/Art Ui/Story Mode/Letter.png", .03f, .54f, .16f, .66f);
            var evt = ArtButton("EventButton", parent, "EVENT", "Assets/Art Ui/Story Mode/Event.png", .03f, .39f, .16f, .51f);
            var bag = ArtButton("BagButton", parent, "BAG", "Assets/Art Ui/Story Mode/BAG.png", .03f, .24f, .16f, .36f);
            var chat = ArtButton("ChatButton", parent, "CHAT", "Assets/Art Ui/Play Mode/Chat ICON.png", .03f, .13f, .09f, .20f);
            Set(controller, "letterButton", letter); Set(controller, "eventButton", evt); Set(controller, "bagButton", bag); Set(controller, "chatButton", chat);
            Set(controller, "letterPanel", Modal("LetterPanel", modalLayer, "LETTERS", new[] { "No letters yet." }));
            Set(controller, "eventPanel", Modal("EventPanel", modalLayer, "EVENTS", new[] { "Current events will appear here." }));
            Set(controller, "inventoryPanel", Modal("InventoryPanel", modalLayer, "INVENTORY", new[] { "Items", "Materials", "Equipment" }));
            Set(controller, "chatPanel", Modal("ChatPanel", modalLayer, "CHAT", new[] { "Tap here to start chatting." }, new Vector2(.03f, .06f), new Vector2(.38f, .34f)));
        }

        static void BuildCharacterAndRank(Transform parent, MenuHomeController controller, Transform modalLayer)
        {
            var character = Image("CurrentCharacter", parent, new Vector2(.14f, .08f), new Vector2(.98f, .88f), Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0));
            character.preserveAspect = true;
            var rankButton = Button("RankUpButton", parent, "RANK UP", new Vector2(.03f, .02f), new Vector2(.33f, .12f), Red);
            var rankBanner = rankButton.GetComponent<Image>();
            var rankPanel = Modal("RankUpPanel", modalLayer, "CHARACTER RANK UP", new[] { "Required fragments", "Current rank", "RANK UP" });
            var starsRoot = UI("RankStars", parent, new Vector2(.03f, .00f), new Vector2(.34f, .04f), Vector2.zero, Vector2.zero);
            var stars = new List<Image>();
            for (var i = 0; i < 5; i++) stars.Add(Image("Star" + (i + 1), starsRoot.transform, new Vector2(i / 5f, 0), new Vector2((i + 1) / 5f, 1), new Vector2(3, 0), new Vector2(-3, 0), Gold));
            Set(controller, "currentCharacterImage", character);
            Set(controller, "rankUpButton", rankButton); Set(controller, "rankUpBanner", rankBanner); Set(controller, "rankUpPanel", rankPanel);
            SetObjectList(controller, "rankStars", stars);
        }

        static void BuildCurrencies(Transform parent, MenuHomeController controller, Transform modalLayer)
        {
            var views = new List<(string id, Image bg, Image icon, TMP_Text amount, Button add, SimpleModalPanel shop)>();
            var ids = new[] { "energy", "gems", "coins" };
            var art = new[] { "Enegry Count.png", "Gem Count.png", "Money count.png" };
            for (var i = 0; i < 3; i++)
            {
                var x0 = .02f + i * .325f;
                var box = Panel("Currency_" + ids[i], parent, new Vector2(x0, .89f), new Vector2(x0 + .30f, .97f), Cream);
                box.GetComponent<Image>().sprite = SpriteAt("Assets/Art Ui/Story Mode/" + art[i]);
                var icon = Image("Icon", box.transform, new Vector2(.02f, .16f), new Vector2(.25f, .84f), Vector2.zero, Vector2.zero, Gold);
                var amount = Text("Amount", box.transform, "0", 21, Brown, TextAlignmentOptions.Center, new Vector2(.24f, 0), new Vector2(.78f, 1));
                var add = Button("Add", box.transform, "+", new Vector2(.80f, .08f), new Vector2(.98f, .92f), Brown);
                var shop = Modal("CurrencyShop_" + ids[i], modalLayer, ids[i].ToUpperInvariant() + " SHOP", new[] { "Purchase options" });
                views.Add((ids[i], box.GetComponent<Image>(), icon, amount, add, shop));
            }
            var so = new SerializedObject(controller);
            var list = so.FindProperty("currencies"); list.arraySize = views.Count;
            for (var i = 0; i < views.Count; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("currencyId").stringValue = views[i].id;
                e.FindPropertyRelative("background").objectReferenceValue = views[i].bg;
                e.FindPropertyRelative("icon").objectReferenceValue = views[i].icon;
                e.FindPropertyRelative("amountText").objectReferenceValue = views[i].amount;
                e.FindPropertyRelative("addButton").objectReferenceValue = views[i].add;
                AddPersistentCall(e.FindPropertyRelative("onAddPressed"), views[i].shop, "Open");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildFixedBottom(Transform parent, MenuHomeController controller, Transform modalLayer)
        {
            var mission = ArtButton("MissionButton", parent, "MISSION", "Assets/Art Ui/Story Mode/Misson.png", .02f, .02f, .68f, .14f);
            var battle = ArtButton("BattlePassButton", parent, "BATTLE PASS", "Assets/Art Ui/Story Mode/Battle Pass.png", .02f, .15f, .46f, .28f);
            var cash = ArtButton("CashShopButton", parent, "CASH SHOP", "Assets/Art Ui/Story Mode/CashShop.png", .70f, .02f, .98f, .28f);
            Set(controller, "missionButton", mission); Set(controller, "battlePassButton", battle); Set(controller, "cashShopButton", cash);
            Set(controller, "missionPanel", Modal("MissionPanel", modalLayer, "MISSIONS", new[] { "Daily", "Weekly", "Story" }));
            Set(controller, "battlePassPanel", Modal("BattlePassPanel", modalLayer, "BATTLE PASS", new[] { "Free rewards", "Premium rewards" }));
            Set(controller, "cashShopPanel", Modal("CashShopPanel", modalLayer, "CASH SHOP", new[] { "Bundles", "Currency", "Special offers" }));
        }

        static void BuildModes(Transform parent, MenuHomeController controller, MenuNavigator navigator, Transform modalLayer)
        {
            var swipe = Panel("ModeSwipeArea", parent, new Vector2(.02f, .30f), new Vector2(.98f, .87f), new Color(0, 0, 0, 0));
            var switcher = swipe.AddComponent<HomeModeSwitcher>();
            var story = Panel("StoryModeContent", swipe.transform, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
            var play = Panel("PlayModeContent", swipe.transform, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0));
            story.GetComponent<Image>().sprite = SpriteAt("Assets/Art Ui/Story Mode/Panel Story Mode.png");
            play.GetComponent<Image>().sprite = SpriteAt("Assets/Art Ui/Play Mode/Play Mode.png");
            var prev = Button("PreviousMode", swipe.transform, "‹", new Vector2(0, .42f), new Vector2(.06f, .58f), Brown);
            var next = Button("NextMode", swipe.transform, "›", new Vector2(.94f, .42f), new Vector2(1, .58f), Brown);
            Set(switcher, "storyModeContent", story); Set(switcher, "playModeContent", play);
            Set(switcher, "storyAnimatedRoot", story.transform as RectTransform); Set(switcher, "playAnimatedRoot", play.transform as RectTransform);
            Set(switcher, "previousButton", prev); Set(switcher, "nextButton", next);
            Set(controller, "modeSwitcher", switcher);
            BuildStoryContent(story.transform, controller, modalLayer);
            BuildPlayContent(play.transform, controller, modalLayer);
        }

        static void BuildStoryContent(Transform parent, MenuHomeController controller, Transform modalLayer)
        {
            var title = Text("ModeTitle", parent, "✦ STORY MODE ✦", 32, Brown, TextAlignmentOptions.Center, new Vector2(.08f, .82f), new Vector2(.65f, .98f));
            var chapter = Text("CurrentChapter", parent, "CHAPTER I: THE INHERITED FLAME", 19, Brown, TextAlignmentOptions.Center, new Vector2(.08f, .68f), new Vector2(.65f, .82f));
            var quest = Text("CurrentQuest", parent, "DIVINE SEAL QUEST", 18, Cream, TextAlignmentOptions.Center, new Vector2(.68f, .77f), new Vector2(.94f, .94f));
            var stage = Text("CurrentStage", parent, "1-1", 24, Brown, TextAlignmentOptions.Center, new Vector2(.68f, .59f), new Vector2(.94f, .77f));
            var enter = ArtButton("EnterStory", parent, "ENTER", "Assets/Art Ui/Story Mode/Enter.png", .68f, .42f, .94f, .59f);
            var wish = Button("Wish", parent, "WISH", new Vector2(.08f, .16f), new Vector2(.36f, .56f), Cream);
            var management = Panel("Management", parent, new Vector2(.40f, .16f), new Vector2(.94f, .56f), Cream);
            var info = ArtButton("CharacterInformation", management.transform, "CHARACTER INFORMATION", "Assets/Art Ui/Story Mode/Character Information.png", .05f, .50f, .68f, .82f);
            var gallery = ArtButton("Gallery", management.transform, "GALLERY", "Assets/Art Ui/Story Mode/Gallery.png", .05f, .14f, .68f, .46f);
            Set(controller, "currentChapterText", chapter); Set(controller, "currentQuestText", quest); Set(controller, "currentStageText", stage);
            Set(controller, "enterStoryButton", enter); Set(controller, "wishButton", wish); Set(controller, "characterInfoButton", info); Set(controller, "galleryButton", gallery);
            Set(controller, "wishPanel", Modal("WishPanel", modalLayer, "WISH / GACHA", new[] { "Banner", "Single pull", "Ten pulls" }));
            Set(controller, "characterInfoPanel", Modal("CharacterInfoPanel", modalLayer, "CHARACTER INFORMATION", new[] { "Stats", "Skills", "Equipment" }));
            Set(controller, "galleryPanel", Modal("GalleryPanel", modalLayer, "GALLERY", new[] { "Character archive", "CG archive", "Music" }));
        }

        static void BuildPlayContent(Transform parent, MenuHomeController controller, Transform modalLayer)
        {
            Text("PlayTitle", parent, "✦ PLAY MODE ✦", 32, Brown, TextAlignmentOptions.Center, new Vector2(.08f, .84f), new Vector2(.72f, .98f));
            var labels = new[] { "Resonance Sanctum", "Sanctum of Lost Echoes", "Rift of the Hunt", "Divine Remnant", "Crossroads of Fate", "Companion Moments" };
            var actions = new List<(string, Button, SimpleModalPanel)>();
            for (var i = 0; i < labels.Length; i++)
            {
                var row = i < 4 ? i : 4;
                float x0, x1, y0, y1;
                if (i < 4) { x0 = .08f; x1 = .72f; y1 = .82f - i * .14f; y0 = y1 - .11f; }
                else { x0 = i == 4 ? .08f : .41f; x1 = i == 4 ? .39f : .72f; y0 = .08f; y1 = .24f; }
                var button = Button("Mode_" + i, parent, labels[i], new Vector2(x0, y0), new Vector2(x1, y1), i == 0 ? Red : Cream);
                var panel = Modal("ModePanel_" + i, modalLayer, labels[i].ToUpperInvariant(), new[] { "Stage selection", "Rewards", "Enter" });
                actions.Add((labels[i], button, panel));
            }
            var gathering = Button("GatheringVale", parent, "THE GATHERING VALE", new Vector2(.76f, .08f), new Vector2(.94f, .52f), Cream);
            var gatheringPanel = Modal("GatheringValePanel", modalLayer, "THE GATHERING VALE", new[] { "Gathering stages", "Available rewards" });
            Set(controller, "gatheringValeButton", gathering); Set(controller, "gatheringValePanel", gatheringPanel);
            var so = new SerializedObject(controller); var list = so.FindProperty("playModeActions"); list.arraySize = actions.Count;
            for (var i = 0; i < actions.Count; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("label").stringValue = actions[i].Item1;
                e.FindPropertyRelative("button").objectReferenceValue = actions[i].Item2;
                AddPersistentCall(e.FindPropertyRelative("action"), actions[i].Item3, "Open");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static SimpleModalPanel Modal(string name, Transform parent, string title, string[] rows, Vector2? min = null, Vector2? max = null)
        {
            var root = Panel(name, parent, min ?? new Vector2(.20f, .18f), max ?? new Vector2(.80f, .82f), new Color(.08f, .06f, .05f, .94f));
            var card = Panel("Card", root.transform, new Vector2(.03f, .05f), new Vector2(.97f, .95f), Cream);
            Text("Title", card.transform, title, 34, Brown, TextAlignmentOptions.Center, new Vector2(.08f, .82f), new Vector2(.92f, .96f));
            for (var i = 0; i < rows.Length; i++)
                Text("Row" + i, card.transform, rows[i], 22, Brown, TextAlignmentOptions.Center, new Vector2(.10f, .62f - i * .13f), new Vector2(.90f, .73f - i * .13f));
            var close = Button("Close", card.transform, "×", new Vector2(.90f, .86f), new Vector2(.98f, .98f), Red);
            var modal = root.AddComponent<SimpleModalPanel>();
            Set(modal, "panelRoot", root); Set(modal, "closeButton", close);
            return modal;
        }

        static void InstallInScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) Object.DestroyImmediate(canvas.gameObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            instance.name = "MenuHub";
            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        static void SetNavigatorHome(MenuNavigator navigator, GameObject home)
        {
            var so = new SerializedObject(navigator); var list = so.FindProperty("screens"); list.arraySize = 1;
            var e = list.GetArrayElementAtIndex(0); e.FindPropertyRelative("id").enumValueIndex = (int)MenuScreenId.Home;
            e.FindPropertyRelative("panel").objectReferenceValue = home; so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject UI(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return go;
        }
        static GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color) { var go = UI(name, parent, min, max, Vector2.zero, Vector2.zero); go.AddComponent<Image>().color = color; return go; }
        static Image Image(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color) { var go = UI(name, parent, min, max, offsetMin, offsetMax); var image = go.AddComponent<Image>(); image.color = color; return image; }
        static TMP_Text Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            var go = UI(name, parent, min, max, Vector2.zero, Vector2.zero); var text = go.AddComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment; text.enableAutoSizing = true; text.fontSizeMin = 10; text.fontSizeMax = size; return text;
        }
        static Button Button(string name, Transform parent, string label, Vector2 min, Vector2 max, Color color)
        {
            var go = Panel(name, parent, min, max, color); var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); Text("Label", go.transform, label, 22, color == Cream ? Brown : Color.white, TextAlignmentOptions.Center, new Vector2(.05f, .08f), new Vector2(.95f, .92f)); return button;
        }
        static Button ArtButton(string name, Transform parent, string label, string artPath, float x0, float y0, float x1, float y1)
        {
            var button = Button(name, parent, label, new Vector2(x0, y0), new Vector2(x1, y1), Cream); var sprite = SpriteAt(artPath); if (sprite != null) { var image = button.GetComponent<Image>(); image.sprite = sprite; image.color = Color.white; image.preserveAspect = true; } return button;
        }
        static Sprite SpriteAt(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static void Set(Object target, string property, Object value) { var so = new SerializedObject(target); var p = so.FindProperty(property); if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); } }
        static void SetObjectList<T>(Object target, string property, List<T> values) where T : Object { var so = new SerializedObject(target); var p = so.FindProperty(property); p.arraySize = values.Count; for (var i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
        static void AddPersistentCall(SerializedProperty unityEvent, Object target, string method)
        {
            var calls = unityEvent.FindPropertyRelative("m_PersistentCalls.m_Calls"); calls.arraySize++;
            var call = calls.GetArrayElementAtIndex(calls.arraySize - 1); call.FindPropertyRelative("m_Target").objectReferenceValue = target; call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = target.GetType().AssemblyQualifiedName; call.FindPropertyRelative("m_MethodName").stringValue = method; call.FindPropertyRelative("m_Mode").enumValueIndex = 1; call.FindPropertyRelative("m_CallState").enumValueIndex = 2;
        }
        static void EnsureFolder(string path)
        {
            var parts = path.Split('/'); var current = parts[0];
            for (var i = 1; i < parts.Length; i++) { var next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
        }
    }
}
#endif
