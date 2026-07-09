#if UNITY_EDITOR
using System.IO;
using BES.Core;
using BES.Gameplay;
using BES.Narrative;
using BES.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace BES.Editor
{
    public static class BESProjectSetup
    {
        const string ProjectRoot = "Assets/_Project";
        const string ScenesPath = ProjectRoot + "/Scenes";
        const string InputPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("BES/Setup Project")]
        public static void SetupFullProject()
        {
            Debug.Log("[BES] Bắt đầu setup toàn bộ project...");

            RunStep("Environment (TMP)", () => BESProjectFix.RunEnvironmentFix());
            RunStep("Import UI assets", () => BESUIAssetImporter.Import());
            RunStep("UI data + HUD sprite map", () =>
            {
                BESUIDataSetup.EnsureDefaultData();
                BESUIScreenBackgroundSetup.EnsureManifest();
            });
            RunStep("Rebuild UI prefabs + scenes", () => BESUIPrefabBuilder.BuildAllPrefabs());
            RunStep("Folders", CreateFolderStructure);
            RunStep("Scenes", CreateScenes);
            RunStep("Game data", CreateDefaultGameData);
            RunStep("Build settings", ConfigureBuildSettings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var manifest = AssetDatabase.LoadAssetAtPath<HUDSpriteManifest>(UIAssetPaths.HudManifestAsset);
            var filled = 0;
            if (manifest != null)
            {
                if (manifest.minimapFrame != null) filled++;
                if (manifest.navInventory != null) filled++;
                if (manifest.navCharacter != null) filled++;
                if (manifest.navMap != null) filled++;
                if (manifest.navWeapon != null) filled++;
                if (manifest.partySlotFrame != null) filled++;
                if (manifest.skillSlotFrame != null) filled++;
            }

            Debug.Log($"[BES] Setup xong. HUD manifest: {filled}/7 core slots populated. Mở scene MainMenu và nhấn Play.");
        }

        static void RunStep(string label, System.Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BES] Lỗi bước '{label}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        static void CreateFolderStructure()
        {
            string[] folders =
            {
                ProjectRoot,
                ProjectRoot + "/Scripts/Core",
                ProjectRoot + "/Scripts/Gameplay",
                ProjectRoot + "/Scripts/Gameplay/Combat",
                ProjectRoot + "/Scripts/Gameplay/Save",
                ProjectRoot + "/Scripts/Gameplay/Inventory",
                ProjectRoot + "/Scripts/Gameplay/World",
                ProjectRoot + "/Scripts/Narrative",
                ProjectRoot + "/Scripts/AI",
                ProjectRoot + "/Scripts/UI",
                ProjectRoot + "/Scripts/Editor",
                ProjectRoot + "/Data",
                ProjectRoot + "/Data/Quests",
                ProjectRoot + "/Data/Dialogue",
                ProjectRoot + "/Data/Items",
                ProjectRoot + "/Prefabs",
                ProjectRoot + "/Scenes",
                ProjectRoot + "/Art",
                ProjectRoot + "/UI",
                ProjectRoot + "/Audio",
                ProjectRoot + "/Tests"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
                    var name = Path.GetFileName(folder);
                    if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                        AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        static void CreateScenes()
        {
            CreateMainMenuScene();
            CreateLoadingScene();
            CreateGameplayScene();
            CreatePrototypeScene();
        }

        static void CreateMainMenuScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var ui = CreateSceneGroup("UI");
            var systems = CreateSceneGroup("Systems");

            var menu = BESUIPrefabBuilder.InstantiateMainMenu();
            if (menu != null)
                menu.transform.SetParent(ui, false);

            EnsureEventSystem(systems);
            SaveScene(SceneNames.MainMenu);
        }

        static void CreateLoadingScene()
        {
            BESLoadingSceneBuilder.RebuildLoadingScene();
        }

        static Slider CreateLoadingSlider(Transform parent)
        {
            var go = new GameObject("ProgressBar");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, new Vector2(680, 24));
            rect.anchoredPosition = new Vector2(0, -140);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.18f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.transition = Selectable.Transition.None;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(fillAreaRect);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(fillRect);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.95f, 0.72f, 0.18f, 1f);

            slider.fillRect = fillRect;
            slider.targetGraphic = bg;
            return slider;
        }

        static void CreateGameplayScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var environment = CreateSceneGroup("Environment");
            var world = CreateSceneGroup("World");
            var entities = CreateSceneGroup("Entities");
            var systems = CreateSceneGroup("Systems");
            var ui = CreateSceneGroup("UI");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(environment, false);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var bootstrapGo = new GameObject("GameplayBootstrap");
            bootstrapGo.transform.SetParent(systems, false);
            var bootstrap = bootstrapGo.AddComponent<GameplaySceneBootstrap>();
            SetPrivateField(bootstrap, "inputActions", inputAsset);

            CreateWorldRegions(world);
            CreateNpcAndEnemies(entities);
            CreateNarrativeSystems(systems);

            var hud = BESUIPrefabBuilder.InstantiateGameplayHud();
            if (hud != null)
                hud.transform.SetParent(ui, false);

            var worldIntegrationGo = new GameObject("WorldIntegration");
            worldIntegrationGo.transform.SetParent(systems, false);
            worldIntegrationGo.AddComponent<WorldIntegrationManager>();

            EnsureEventSystem(systems);
            SaveScene(SceneNames.Gameplay);
        }

        static Transform CreateSceneGroup(string name)
        {
            var go = new GameObject(name);
            return go.transform;
        }

        static void CreatePrototypeScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var environment = CreateSceneGroup("Environment");
            var systems = CreateSceneGroup("Systems");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(environment, false);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var bootstrapGo = new GameObject("PrototypeBootstrap");
            bootstrapGo.transform.SetParent(systems, false);
            var bootstrap = bootstrapGo.AddComponent<GameplaySceneBootstrap>();
            SetPrivateField(bootstrap, "inputActions", inputAsset);

            SaveScene(SceneNames.Prototype);
        }

        static void CreateWorldRegions(Transform worldRoot)
        {
            var regions = CreateSceneGroup("Regions");
            regions.SetParent(worldRoot, false);
            var teleports = CreateSceneGroup("Teleports");
            teleports.SetParent(worldRoot, false);
            var collectibles = CreateSceneGroup("Collectibles");
            collectibles.SetParent(worldRoot, false);

            CreateRegion(regions, "Region_CreationCity", "region_creation_city", "Creation City Outskirts", new Vector3(-15f, 0f, 0f));
            CreateRegion(regions, "Region_Ruins", "region_ruins", "Ancient Ruins", new Vector3(15f, 0f, 0f));
            CreateRegion(regions, "Region_Forest", "region_forest", "Whispering Forest", new Vector3(0f, 0f, 20f));

            var tpA = new GameObject("Teleport_CityToRuins");
            tpA.transform.SetParent(teleports, false);
            tpA.transform.position = new Vector3(-5f, 1f, 0f);
            var colA = tpA.AddComponent<BoxCollider>();
            colA.isTrigger = true;
            colA.size = new Vector3(2f, 2f, 2f);
            var destGo = new GameObject("Dest_Ruins");
            destGo.transform.SetParent(teleports, false);
            var dest = destGo.transform;
            dest.position = new Vector3(12f, 1f, 0f);
            var tpComp = tpA.AddComponent<TeleportPoint>();
            SetPrivateField(tpComp, "destination", dest);
            SetPrivateField(tpComp, "regionId", "region_ruins");

            for (var i = 0; i < 3; i++)
            {
                var herb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                herb.name = $"Collectible_Herb_{i}";
                herb.transform.SetParent(collectibles, false);
                herb.transform.position = new Vector3(2f + i * 2f, 0.5f, 3f);
                herb.transform.localScale = Vector3.one * 0.5f;
                var col = herb.GetComponent<Collider>();
                col.isTrigger = true;
                herb.AddComponent<Collectible>();
            }
        }

        static void CreateRegion(Transform parent, string name, string markerId, string displayName, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var region = go.AddComponent<WorldRegion>();
            SetPrivateField(region, "regionId", markerId);
            SetPrivateField(region, "regionName", displayName);
            var marker = go.AddComponent<QuestMarker>();
            marker.SetMarkerId(markerId);
        }

        static void CreateNpcAndEnemies(Transform entitiesRoot)
        {
            var npcs = CreateSceneGroup("NPCs");
            npcs.SetParent(entitiesRoot, false);
            var enemies = CreateSceneGroup("Enemies");
            enemies.SetParent(entitiesRoot, false);
            var bosses = CreateSceneGroup("Bosses");
            bosses.SetParent(entitiesRoot, false);

            var npc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            npc.name = "NPC_Guard";
            npc.transform.SetParent(npcs, false);
            npc.transform.position = new Vector3(3f, 1f, 2f);
            Object.DestroyImmediate(npc.GetComponent<Collider>());
            var col = npc.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            npc.AddComponent<NPCInteractable>();
            var npcMarker = npc.AddComponent<QuestMarker>();
            npcMarker.SetMarkerId("npc_guard");

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Enemy_Slime";
            enemy.transform.SetParent(enemies, false);
            enemy.tag = "Enemy";
            enemy.transform.position = new Vector3(8f, 0.5f, -4f);
            enemy.layer = LayerMask.NameToLayer("Enemy");
            enemy.AddComponent<EnemyHealth>();
            enemy.AddComponent<EnemyAI>();

            var boss = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boss.name = "Boss_VoidGuardian";
            boss.transform.SetParent(bosses, false);
            boss.tag = "Enemy";
            boss.transform.position = new Vector3(18f, 1f, 0f);
            boss.transform.localScale = new Vector3(2f, 2f, 2f);
            boss.layer = LayerMask.NameToLayer("Enemy");
            boss.AddComponent<EnemyHealth>();
            boss.AddComponent<EnemyAI>();
            boss.AddComponent<BossController>();
            var bossMarker = boss.AddComponent<QuestMarker>();
            bossMarker.SetMarkerId("Boss_VoidGuardian");
        }

        static void CreateNarrativeSystems(Transform systemsRoot)
        {
            if (Object.FindAnyObjectByType<DialogueSystem>() != null)
                return;

            var go = new GameObject("NarrativeSystems");
            go.transform.SetParent(systemsRoot, false);
            go.AddComponent<DialogueSystem>();
            go.AddComponent<AIDialogueService>();
        }

        static void CreateCanvasWithHudAndDialogue()
        {
            var canvas = CreateCanvas("GameplayCanvas");
            var uiRoot = canvas.AddComponent<UIRootController>();

            CreateHudSection(canvas.transform);
            CreateMiniMapSection(canvas.transform);
            CreateQuestTrackerSection(canvas.transform);
            CreateInteractPrompt(canvas.transform);
            var inv = CreateInventoryPanel(canvas.transform);
            var profile = CreateCharacterPanel(canvas.transform);
            var worldMap = CreateWorldMapPanel(canvas.transform);
            CreateDialogueSection(canvas.transform);

            SetPrivateField(uiRoot, "inventoryUI", inv);
            SetPrivateField(uiRoot, "characterProfileUI", profile);
            SetPrivateField(uiRoot, "gameMapUI", worldMap);
        }

        static void CreateHudSection(Transform parent)
        {
            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(parent, false);
            var hud = hudGo.AddComponent<HUDController>();
            var health = CreateSlider(hudGo.transform, "HealthBar", new Vector2(-300, 200));
            var stamina = CreateSlider(hudGo.transform, "StaminaBar", new Vector2(-300, 170));
            var mana = CreateSlider(hudGo.transform, "ManaBar", new Vector2(-300, 140));
            var quest = CreateText(hudGo.transform, "QuestText", "Quest: —", new Vector2(-300, 110));
            var region = CreateText(hudGo.transform, "RegionText", "Region: Creation City", new Vector2(-300, 80));
            SetPrivateField(hud, "healthBar", health);
            SetPrivateField(hud, "staminaBar", stamina);
            SetPrivateField(hud, "manaBar", mana);
            SetPrivateField(hud, "questText", quest);
            SetPrivateField(hud, "regionText", region);
        }

        static void CreateMiniMapSection(Transform parent)
        {
            var go = new GameObject("MiniMap");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(160, 160);
            rect.anchoredPosition = new Vector2(-20, -20);
            go.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            var miniMap = go.AddComponent<MiniMapUI>();
            var playerIcon = CreateIcon(go.transform, "PlayerIcon", new Vector2(8, 8), Color.green);
            var objectiveIcon = CreateIcon(go.transform, "ObjectiveIcon", new Vector2(8, 8), Color.yellow);
            SetPrivateField(miniMap, "mapRect", rect);
            SetPrivateField(miniMap, "playerIcon", playerIcon);
            SetPrivateField(miniMap, "objectiveIcon", objectiveIcon);
        }

        static void CreateQuestTrackerSection(Transform parent)
        {
            var go = new GameObject("QuestTracker");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20, -20);
            rect.sizeDelta = new Vector2(320, 100);

            var tracker = go.AddComponent<QuestTrackerUI>();
            var title = CreateText(go.transform, "QuestTitle", "Quest: —", new Vector2(0, 0));
            var step = CreateText(go.transform, "QuestStep", "...", new Vector2(0, -30));
            var compassGo = new GameObject("CompassArrow");
            compassGo.transform.SetParent(go.transform, false);
            var compassRect = compassGo.AddComponent<RectTransform>();
            compassRect.sizeDelta = new Vector2(24, 24);
            compassRect.anchoredPosition = new Vector2(280, -50);
            compassGo.AddComponent<Image>().color = Color.cyan;
            SetPrivateField(tracker, "questTitleText", title);
            SetPrivateField(tracker, "questStepText", step);
            SetPrivateField(tracker, "compassArrow", compassRect);
        }

        static void CreateInteractPrompt(Transform parent)
        {
            var root = new GameObject("InteractPrompt");
            root.transform.SetParent(parent, false);
            var prompt = root.AddComponent<InteractPromptUI>();

            var panel = new GameObject("PromptPanel");
            panel.transform.SetParent(root.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 80);
            rect.sizeDelta = new Vector2(400, 40);

            var text = CreateText(panel.transform, "PromptText", "Nhấn F để tương tác", Vector2.zero);
            SetPrivateField(prompt, "promptRoot", panel);
            SetPrivateField(prompt, "promptText", text);
            panel.SetActive(false);
        }

        static InventoryUI CreateInventoryPanel(Transform parent)
        {
            var go = new GameObject("InventoryUI");
            go.transform.SetParent(parent, false);
            var panelRect = go.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(360, 300);
            go.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            var inv = go.AddComponent<InventoryUI>();
            CreateText(go.transform, "Title", "Inventory (Tab/I)", new Vector2(0, 120));

            var listGo = new GameObject("ListContainer");
            listGo.transform.SetParent(go.transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            listRect.sizeDelta = new Vector2(320, 220);
            listRect.anchoredPosition = new Vector2(0, -20);

            var rowPrefab = CreateText(listGo.transform, "ItemRowPrefab", "Item x1", Vector2.zero);
            rowPrefab.gameObject.SetActive(false);

            SetPrivateField(inv, "panel", go);
            SetPrivateField(inv, "listContainer", listGo.transform);
            SetPrivateField(inv, "itemRowPrefab", rowPrefab);
            go.SetActive(false);
            return inv;
        }

        static CharacterProfileUI CreateCharacterPanel(Transform parent)
        {
            var go = new GameObject("CharacterProfileUI");
            go.transform.SetParent(parent, false);
            var panelRect = go.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(360, 340);
            go.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

            var profile = go.AddComponent<CharacterProfileUI>();
            CreateText(go.transform, "Title", "Character (C)", new Vector2(0, 140));
            var name = CreateText(go.transform, "Name", "Main Character", new Vector2(0, 100));
            var level = CreateText(go.transform, "Level", "Lv. 1", new Vector2(0, 70));
            var atk = CreateText(go.transform, "ATK", "ATK: 15", new Vector2(-80, 30));
            var hp = CreateText(go.transform, "HP", "HP: 100", new Vector2(80, 30));
            var def = CreateText(go.transform, "DEF", "DEF: 5", new Vector2(-80, 0));
            var critRate = CreateText(go.transform, "CritRate", "Crit Rate: 10%", new Vector2(80, 0));
            var critDmg = CreateText(go.transform, "CritDmg", "Crit DMG: 150%", new Vector2(0, -30));

            var equipGo = new GameObject("EquipmentUI");
            equipGo.transform.SetParent(go.transform, false);
            var equip = equipGo.AddComponent<EquipmentUI>();
            var weaponName = CreateText(equipGo.transform, "WeaponName", "Weapon: Iron Sword", new Vector2(0, -70));
            var weaponAtk = CreateText(equipGo.transform, "WeaponAtk", "ATK: 15", new Vector2(0, -100));
            SetPrivateField(equip, "weaponNameText", weaponName);
            SetPrivateField(equip, "weaponAtkText", weaponAtk);

            SetPrivateField(profile, "panel", go);
            SetPrivateField(profile, "nameText", name);
            SetPrivateField(profile, "levelText", level);
            SetPrivateField(profile, "atkText", atk);
            SetPrivateField(profile, "hpText", hp);
            SetPrivateField(profile, "defText", def);
            SetPrivateField(profile, "critRateText", critRate);
            SetPrivateField(profile, "critDmgText", critDmg);
            SetPrivateField(profile, "equipmentUI", equip);
            go.SetActive(false);
            return profile;
        }

        static GameMapUI CreateWorldMapPanel(Transform parent)
        {
            var go = new GameObject("GameMapUI");
            go.transform.SetParent(parent, false);
            var panelRect = go.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

            var map = go.AddComponent<GameMapUI>();
            CreateText(go.transform, "Title", "World Map (M)", new Vector2(0, 200));
            var r1 = CreateText(go.transform, "Region1", "Creation City Outskirts", new Vector2(0, 120));
            var r2 = CreateText(go.transform, "Region2", "Ancient Ruins", new Vector2(0, 80));
            var r3 = CreateText(go.transform, "Region3", "Whispering Forest", new Vector2(0, 40));
            SetPrivateField(map, "panel", go);
            SetPrivateField(map, "regionCreationText", r1);
            SetPrivateField(map, "regionRuinsText", r2);
            SetPrivateField(map, "regionForestText", r3);
            go.SetActive(false);
            return map;
        }

        static void CreateDialogueSection(Transform parent)
        {
            var dialogueGo = new GameObject("DialogueUI");
            dialogueGo.transform.SetParent(parent, false);
            var dialogue = dialogueGo.AddComponent<DialogueUI>();
            var panel = new GameObject("DialoguePanel");
            panel.transform.SetParent(dialogueGo.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.05f);
            panelRect.anchorMax = new Vector2(0.9f, 0.35f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            panel.SetActive(false);

            var speaker = CreateText(panel.transform, "Speaker", "NPC", new Vector2(-350, 80));
            var line = CreateText(panel.transform, "Line", "...", new Vector2(-350, 20));
            var chatInput = CreateInputField(panel.transform, "ChatInput", new Vector2(-200, -50));
            var sendBtn = CreateButton(panel.transform, "SendButton", "Send", new Vector2(150, -50));
            var closeBtn = CreateButton(panel.transform, "CloseButton", "Close", new Vector2(280, -50));
            var choices = new GameObject("Choices").transform;
            choices.SetParent(panel.transform, false);
            var choiceBtn = CreateButton(choices, "ChoiceTemplate", "Choice", Vector2.zero);
            choiceBtn.gameObject.SetActive(false);

            SetPrivateField(dialogue, "panel", panel);
            SetPrivateField(dialogue, "speakerText", speaker);
            SetPrivateField(dialogue, "dialogueText", line);
            SetPrivateField(dialogue, "chatInput", chatInput);
            SetPrivateField(dialogue, "sendButton", sendBtn);
            SetPrivateField(dialogue, "closeButton", closeBtn);
            SetPrivateField(dialogue, "choicesContainer", choices);
            SetPrivateField(dialogue, "choiceButtonPrefab", choiceBtn);
        }

        static RectTransform CreateIcon(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return rect;
        }

        static TMP_InputField CreateInputField(Transform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 36);
            rect.anchoredPosition = anchoredPos;
            go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 16;
            text.color = Color.white;

            var input = go.AddComponent<TMP_InputField>();
            input.textComponent = text;
            return input;
        }

        static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<UICanvasFit>();
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        static void EnsureEventSystem(Transform parent = null)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            if (parent != null)
                es.transform.SetParent(parent, false);
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 40);
            rect.anchoredPosition = anchoredPos;
            go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            var btn = go.AddComponent<Button>();
            CreateText(go.transform, "Label", label, Vector2.zero);
            return btn;
        }

        static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 20);
            rect.anchoredPosition = anchoredPos;
            return go.AddComponent<Slider>();
        }

        static TMP_Text CreateText(Transform parent, string name, string content, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 30);
            rect.anchoredPosition = anchoredPos;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = 18;
            text.color = Color.white;
            return text;
        }

        static void CreateDefaultGameData()
        {
            var dataPath = ProjectRoot + "/Data";
            var itemsPath = dataPath + "/Items";
            var questsPath = dataPath + "/Quests";
            var dialoguePath = dataPath + "/Dialogue";
            var resourcesPath = ProjectRoot + "/Resources";
            var resourcesDataPath = resourcesPath + "/Data";
            var resourcesDialoguePath = resourcesPath + "/Dialogue";

            foreach (var folder in new[] { itemsPath, questsPath, dialoguePath, resourcesPath, resourcesDataPath, resourcesDialoguePath })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
                    var name = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, name);
                }
            }

            var itemDb = CreateOrLoadAsset<ItemDatabase>(itemsPath + "/ItemDatabase.asset");
            if (itemDb.items.Count == 0)
            {
                itemDb.items.Add(CreateItem("herb_common", "Common Herb", ItemType.Material, 1));
                itemDb.items.Add(CreateItem("material_ore", "Ore", ItemType.Material, 2));
                itemDb.items.Add(CreateItem("material_crystal", "Crystal", ItemType.Material, 2));
                itemDb.items.Add(CreateItem("potion_heal", "Healing Potion", ItemType.Consumable, 2, 30f));
                itemDb.items.Add(CreateItem("relic_shard", "Relic Shard", ItemType.Quest, 3));
                EditorUtility.SetDirty(itemDb);
            }

            var mainQuest = CreateOrLoadAsset<QuestDefinition>(questsPath + "/Quest_Main_Awakening.asset");
            mainQuest.questId = "main_awakening";
            mainQuest.questTitle = "Awakening in Guarem";
            mainQuest.questType = QuestType.Main;
            mainQuest.summary = "Begin your journey at Creation City outskirts.";
            mainQuest.steps = new System.Collections.Generic.List<QuestStep>
            {
                new() { stepId = "s1", stepType = QuestStepType.Talk, targetId = "npc_guard", description = "Speak with the city guard." },
                new() { stepId = "s2", stepType = QuestStepType.Reach, targetId = "region_ruins", description = "Travel to the Ancient Ruins." },
                new() { stepId = "s3", stepType = QuestStepType.Defeat, targetId = "Boss_VoidGuardian", description = "Defeat the Void Guardian." },
                new() { stepId = "s4", stepType = QuestStepType.Choice, targetId = "branch_choice", description = "Choose your path." }
            };
            mainQuest.rewardItemId = "relic_shard";
            mainQuest.rewardItemCount = 1;
            EditorUtility.SetDirty(mainQuest);

            var sideQuest = CreateOrLoadAsset<QuestDefinition>(questsPath + "/Quest_Side_Herbs.asset");
            sideQuest.questId = "side_collect_herbs";
            sideQuest.questTitle = "Herbs for the Temple";
            sideQuest.questType = QuestType.Side;
            sideQuest.steps = new System.Collections.Generic.List<QuestStep>
            {
                new() { stepId = "h1", stepType = QuestStepType.Collect, targetId = "herb_common", requiredCount = 3, description = "Collect 3 common herbs." }
            };
            sideQuest.rewardItemId = "herb_common";
            sideQuest.rewardItemCount = 1;
            EditorUtility.SetDirty(sideQuest);

            var endingA = CreateOrLoadAsset<QuestDefinition>(questsPath + "/Quest_Ending_A.asset");
            endingA.questId = "ending_guardian_pact";
            endingA.questTitle = "Guardian Pact";
            endingA.questType = QuestType.Main;
            endingA.endingId = "ending_guardian_pact";
            EditorUtility.SetDirty(endingA);

            var endingB = CreateOrLoadAsset<QuestDefinition>(questsPath + "/Quest_Ending_B.asset");
            endingB.questId = "ending_void_whisper";
            endingB.questTitle = "Whisper of the Void";
            endingB.questType = QuestType.Main;
            endingB.endingId = "ending_void_whisper";
            EditorUtility.SetDirty(endingB);

            var questDb = CreateOrLoadAsset<QuestDatabase>(questsPath + "/QuestDatabase.asset");
            questDb.quests = new System.Collections.Generic.List<QuestDefinition>
            {
                mainQuest, sideQuest, endingA, endingB
            };
            EditorUtility.SetDirty(questDb);

            var introGuard = CreateOrLoadAsset<DialogueNode>(dialoguePath + "/Node_IntroGuard.asset");
            introGuard.nodeId = "intro_guard";
            introGuard.speakerId = "City Guard";
            introGuard.line = "Chào lữ khách. Creation City đang chờ bạn — bạn sẵn sàng bắt đầu hành trình chưa?";
            introGuard.questTriggerId = "main_awakening";
            introGuard.choices = new System.Collections.Generic.List<DialogueChoice>
            {
                new() { choiceText = "Tôi sẵn sàng", nextNodeId = "", affinityDelta = 5 },
                new() { choiceText = "Kể thêm về vùng đất này", nextNodeId = "intro_guard_lore" }
            };
            EditorUtility.SetDirty(introGuard);

            var introLore = CreateOrLoadAsset<DialogueNode>(dialoguePath + "/Node_IntroGuard_Lore.asset");
            introLore.nodeId = "intro_guard_lore";
            introLore.speakerId = "City Guard";
            introLore.line = "Guarem là vùng đất nơi các mảnh ký ức cũ vẫn thì thầm. Hãy đến Ancient Ruins nếu bạn muốn biết thêm.";
            introLore.choices = new System.Collections.Generic.List<DialogueChoice>
            {
                new() { choiceText = "Cảm ơn", nextNodeId = "" }
            };
            EditorUtility.SetDirty(introLore);

            AssetDatabase.CopyAsset(itemsPath + "/ItemDatabase.asset", resourcesDataPath + "/ItemDatabase.asset");
            AssetDatabase.CopyAsset(questsPath + "/QuestDatabase.asset", resourcesDataPath + "/QuestDatabase.asset");
            AssetDatabase.CopyAsset(dialoguePath + "/Node_IntroGuard.asset", resourcesDialoguePath + "/Node_IntroGuard.asset");
            AssetDatabase.CopyAsset(dialoguePath + "/Node_IntroGuard_Lore.asset", resourcesDialoguePath + "/Node_IntroGuard_Lore.asset");
        }

        static ItemDefinition CreateItem(string id, string name, ItemType type, int rarity, float healAmount = 0f)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.itemId = id;
            item.displayName = name;
            item.itemType = type;
            item.rarity = rarity;
            item.healAmount = healAmount;
            return item;
        }

        static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void ConfigureBuildSettings()
        {
            var scenes = new[]
            {
                ScenesPath + "/MainMenu.unity",
                ScenesPath + "/Loading.unity",
                ScenesPath + "/Gameplay.unity",
                ScenesPath + "/PrototypeScene.unity"
            };

            var list = new EditorBuildSettingsScene[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
                list[i] = new EditorBuildSettingsScene(scenes[i], true);

            EditorBuildSettings.scenes = list;
        }

        static void SaveScene(string sceneName)
        {
            var path = $"{ScenesPath}/{sceneName}.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        }

        static void SetPrivateField(Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
#endif
