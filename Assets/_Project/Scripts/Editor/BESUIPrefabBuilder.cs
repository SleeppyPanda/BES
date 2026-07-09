#if UNITY_EDITOR
using BES.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.Editor
{
    public static class BESUIPrefabBuilder
    {
        public static void RebuildAllFromMenu()
        {
            BESUIAssetImporter.Import();
            BESUIDataSetup.EnsureDefaultData();
            BESUIScreenBackgroundSetup.EnsureManifest();
            BuildAllPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BES] UI prefabs rebuilt.");
        }

        public static void BuildAllPrefabs()
        {
            BuildAtomPrefabs();
            BuildMainMenuPrefab();
            BuildGameplayHudPrefab();
        }

        public static void BuildAtomPrefabs() => BESUIAtomBuilder.BuildAll();

        public static GameObject InstantiateGameplayHud()
        {
            var path = UIAssetPaths.ScreenPrefabs + "/GameplayHUD.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                BuildGameplayHudPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : BuildGameplayHudRoot();
        }

        public static GameObject InstantiateMainMenu()
        {
            var path = UIAssetPaths.ScreenPrefabs + "/MainMenuScreen.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                BuildMainMenuPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : BuildMainMenuRoot();
        }

        public static void BuildMainMenuPrefab()
        {
            var root = BuildMainMenuRoot();
            BESUIEditorUtils.SavePrefab(root, UIAssetPaths.ScreenPrefabs + "/MainMenuScreen.prefab");
        }

        public static void BuildGameplayHudPrefab()
        {
            var root = BuildGameplayHudRoot();
            BESUIEditorUtils.SavePrefab(root, UIAssetPaths.ScreenPrefabs + "/GameplayHUD.prefab");
        }

        static GameObject BuildMainMenuRoot()
        {
            var canvasGo = BESUIEditorUtils.CreateCanvasRoot("MainMenuScreen", out _);
            BESUIEditorUtils.CreateBackground(canvasGo.transform, BESUIEditorUtils.LoadBg(UIAssetPaths.BgStart));
            BESUIEditorUtils.AttachScreenBackground(canvasGo, UIScreenBackgroundId.MainMenu, false);

            var controllerGo = new GameObject("MainMenuController");
            controllerGo.transform.SetParent(canvasGo.transform, false);
            var controller = controllerGo.AddComponent<MainMenuController>();

            var clickBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "ClickToBegin", UIAnchorPresets.ApplyMainMenuClickHit);
            var regionBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "RegionButton", UIAnchorPresets.ApplyMainMenuRegionHit);
            var eventBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "EventButton", UIAnchorPresets.ApplyMainMenuEventHit);
            var quitBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "QuitButton", UIAnchorPresets.ApplyMainMenuQuitHit);
            var profileBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "ProfileButton", UIAnchorPresets.ApplyMainMenuProfileHit);
            var settingsBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "SettingsButton", UIAnchorPresets.ApplyMainMenuSettingsHit);

            var profilePanel = BuildPlayerProfilePanel(canvasGo.transform);
            var settingsPanel = BuildSettingsPanel(canvasGo.transform);
            var serverPicker = BuildServerPickerPanel(canvasGo.transform);
            var eventPanel = BuildEventPanel(canvasGo.transform);

            BESUIEditorUtils.SetPrivateField(controller, "regionButton", regionBtn);
            BESUIEditorUtils.SetPrivateField(controller, "quitButton", quitBtn);
            BESUIEditorUtils.SetPrivateField(controller, "clickToBeginButton", clickBtn);
            BESUIEditorUtils.SetPrivateField(controller, "profileButton", profileBtn);
            BESUIEditorUtils.SetPrivateField(controller, "settingsButton", settingsBtn);
            BESUIEditorUtils.SetPrivateField(controller, "eventButton", eventBtn);
            BESUIEditorUtils.SetPrivateField(controller, "playerProfileUI", profilePanel);
            BESUIEditorUtils.SetPrivateField(controller, "settingsUI", settingsPanel);
            BESUIEditorUtils.SetPrivateField(controller, "serverPickerUI", serverPicker);
            BESUIEditorUtils.SetPrivateField(controller, "eventUI", eventPanel);

            return canvasGo;
        }

        static GameObject BuildGameplayHudRoot()
        {
            var manifest = BESUIEditorUtils.LoadHudManifest();
            var canvasGo = BESUIEditorUtils.CreateCanvasRoot("GameplayHUD", out _);
            var hudLayout = canvasGo.AddComponent<GameplayHudLayout>();
            BESUIEditorUtils.SetPrivateField(hudLayout, "manifest", manifest);

            var nav = canvasGo.AddComponent<UINavigationController>();

            var hudLayer = CreateChild(canvasGo.transform, "HUDLayer");
            BuildHudSection(hudLayer.transform, manifest);
            BuildMiniMap(hudLayer.transform, manifest);
            BuildQuestTracker(hudLayer.transform, manifest);
            BuildQuestLog(hudLayer.transform);
            BuildInteractPrompt(hudLayer.transform, manifest);
            BuildHudNavBar(hudLayer.transform, manifest);
            BuildPartyStrip(hudLayer.transform, manifest);
            BuildSkillBar(hudLayer.transform, manifest);
            ChatEnterWidgets.Build(hudLayer.transform, manifest);
            TopLeftHudWidgets.ApplyPortraitChip(hudLayer.transform, manifest);
            TopLeftHudWidgets.ApplyLockBtn(hudLayer.transform, manifest);

            // Layer 1 overlays
            var overlayLayer = CreateChild(canvasGo.transform, "OverlayLayer");
            var inventory = BuildInventoryPanel(overlayLayer.transform);
            var character = BuildCharacterPanel(overlayLayer.transform);
            var map = BuildWorldMapPanel(overlayLayer.transform);
            var weapon = BuildWeaponPanel(overlayLayer.transform);
            var artifacts = BuildArtifactsPanel(overlayLayer.transform);

            // Layer 2 meta
            var metaLayer = CreateChild(canvasGo.transform, "MetaLayer");
            var team = BuildTeamPanel(metaLayer.transform);
            var evt = BuildEventPanel(metaLayer.transform);
            var wish = BuildWishPanel(metaLayer.transform);

            // Layer 3 modals + weapon flow
            var modalLayer = CreateChild(canvasGo.transform, "ModalLayer");
            var dialogue = BuildDialoguePanel(modalLayer.transform);
            var loading = BuildLoadingPanel(modalLayer.transform);
            var enhance = BuildWeaponEnhancePanel(modalLayer.transform);
            var rankUp = BuildWeaponRankUpPanel(modalLayer.transform);
            var refine = BuildWeaponRefinePanel(modalLayer.transform);

            BESUIEditorUtils.SetPrivateField(weapon, "enhanceUI", enhance);
            BESUIEditorUtils.SetPrivateField(enhance, "rankUpUI", rankUp);
            BESUIEditorUtils.SetPrivateField(rankUp, "refineUI", refine);

            BESUIEditorUtils.SetPrivateField(nav, "hud", hudLayer.GetComponentInChildren<HUDController>(true));
            BESUIEditorUtils.SetPrivateField(nav, "miniMap", hudLayer.GetComponentInChildren<MiniMapUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "questTracker", hudLayer.GetComponentInChildren<QuestTrackerUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "questLogUI", hudLayer.GetComponentInChildren<QuestLogUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "interactPrompt", hudLayer.GetComponentInChildren<InteractPromptUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "hudNavBar", hudLayer.GetComponentInChildren<HudNavBarUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "inventoryUI", inventory);
            BESUIEditorUtils.SetPrivateField(nav, "characterProfileUI", character);
            BESUIEditorUtils.SetPrivateField(nav, "gameMapUI", map);
            BESUIEditorUtils.SetPrivateField(nav, "weaponScreenUI", weapon);
            BESUIEditorUtils.SetPrivateField(nav, "artifactsUI", artifacts);
            BESUIEditorUtils.SetPrivateField(nav, "teamSetupUI", team);
            BESUIEditorUtils.SetPrivateField(nav, "eventUI", evt);
            BESUIEditorUtils.SetPrivateField(nav, "wishUI", wish);
            BESUIEditorUtils.SetPrivateField(nav, "dialogueUI", dialogue);
            BESUIEditorUtils.SetPrivateField(nav, "loadingScreenUI", loading);
            BESUIEditorUtils.SetPrivateField(nav, "weaponEnhanceUI", enhance);
            BESUIEditorUtils.SetPrivateField(nav, "weaponRankUpUI", rankUp);
            BESUIEditorUtils.SetPrivateField(nav, "weaponRefineUI", refine);

            canvasGo.AddComponent<UIScreenBackgroundBootstrap>();

            return canvasGo;
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            return go;
        }

        static void BuildHudSection(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyHudBarsRegion(rect);

            var hud = go.AddComponent<HUDController>();
            var level = BESUIEditorUtils.CreateText(go.transform, "LevelText", "Level 1.", HUDLayoutTokens.LevelTextPos, 16f, TextAlignmentOptions.MidlineLeft);
            level.color = Color.white;

            var health = BESUIEditorUtils.CreateFilledSlider(go.transform, "HealthBar", HUDLayoutTokens.HealthBarPos, HUDLayoutTokens.HealthBarSize,
                null, null, HUDPrimitiveStyles.HpBarFill);
            ApplyPrimitiveBar(health, HUDPrimitiveStyles.HpBarBackground, HUDPrimitiveStyles.HpBarFill);
            var barFrame = LoadEditorFrame("Rectangle 39782.png");
            if (barFrame != null)
            {
                var bg = health.transform.Find("Background")?.GetComponent<Image>();
                if (bg != null)
                {
                    bg.sprite = barFrame;
                    bg.type = Image.Type.Sliced;
                    bg.color = Color.white;
                }
            }

            var hpValue = BESUIEditorUtils.CreateText(health.transform, "HpValue", "100/100", Vector2.zero, 14f, TextAlignmentOptions.Center);
            hpValue.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            var stamina = BESUIEditorUtils.CreateFilledSlider(go.transform, "StaminaBar", new Vector2(HUDLayoutTokens.HealthBarPos.x, HUDLayoutTokens.HealthBarPos.y - 18f), new Vector2(HUDLayoutTokens.HealthBarSize.x, 4f),
                null, null, HUDPrimitiveStyles.StaminaBarFill);
            ApplyPrimitiveBar(stamina, HUDPrimitiveStyles.HpBarBackground, HUDPrimitiveStyles.StaminaBarFill);
            stamina.gameObject.SetActive(false);

            var mana = BESUIEditorUtils.CreateFilledSlider(go.transform, "ManaBar", new Vector2(HUDLayoutTokens.HealthBarPos.x, HUDLayoutTokens.HealthBarPos.y - 24f), new Vector2(HUDLayoutTokens.HealthBarSize.x, 3f),
                null, null, HUDPrimitiveStyles.ManaBarFill);
            ApplyPrimitiveBar(mana, new Color(0.1f, 0.1f, 0.14f, 0.35f), HUDPrimitiveStyles.ManaBarFill);
            mana.gameObject.SetActive(false);

            var region = BESUIEditorUtils.CreateText(go.transform, "RegionText", string.Empty, HUDLayoutTokens.RegionTextPos, 13f, TextAlignmentOptions.BottomLeft);
            region.color = new Color(1f, 1f, 1f, 0.75f);

            BESUIEditorUtils.SetPrivateField(hud, "healthBar", health);
            BESUIEditorUtils.SetPrivateField(hud, "staminaBar", stamina);
            BESUIEditorUtils.SetPrivateField(hud, "manaBar", mana);
            BESUIEditorUtils.SetPrivateField(hud, "levelText", level);
            BESUIEditorUtils.SetPrivateField(hud, "hpValueText", hpValue);
            BESUIEditorUtils.SetPrivateField(hud, "regionText", region);
        }

        static void ApplyPrimitiveBar(Slider slider, Color bgColor, Color fillColor)
        {
            if (slider == null)
                return;

            var bg = slider.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = null;
                bg.color = bgColor;
            }

            var fill = slider.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.sprite = null;
                fill.color = fillColor;
            }
        }

        static void BuildMiniMap(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("MiniMap");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<MiniMapUI>();
            TopLeftHudWidgets.ApplyMiniMap(go.transform, manifest);
        }

        static void BuildQuestTracker(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("QuestTracker");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<QuestTrackerUI>();
            TopLeftHudWidgets.ApplyQuestTracker(go.transform, manifest);
        }

        static void BuildQuestLog(Transform parent)
        {
            var go = new GameObject("QuestLog");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            var log = go.AddComponent<QuestLogUI>();
            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(520, 360);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.08f, 0.07f, 0.12f, 0.95f);
            var listGo = new GameObject("List");
            listGo.transform.SetParent(panel.transform, false);
            var listRect = listGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(listRect);
            listRect.offsetMin = new Vector2(16, 16);
            listRect.offsetMax = new Vector2(-16, -48);
            var rowPrefab = BESUIEditorUtils.CreateText(listGo.transform, "RowTemplate", "Quest row", Vector2.zero, 14f);
            rowPrefab.gameObject.SetActive(false);
            var closeBtn = BESUIEditorUtils.CreateButton(panel.transform, "CloseBtn", "Đóng", new Vector2(0, 20), new Vector2(120, 32));
            BESUIEditorUtils.SetPrivateField(log, "panel", panel);
            BESUIEditorUtils.SetPrivateField(log, "listContainer", listGo.transform);
            BESUIEditorUtils.SetPrivateField(log, "rowPrefab", rowPrefab);
            BESUIEditorUtils.SetPrivateField(log, "closeButton", closeBtn);
            panel.SetActive(false);
        }

        static void BuildInteractPrompt(Transform parent, HUDSpriteManifest manifest)
        {
            var root = new GameObject("InteractPrompt");
            root.transform.SetParent(parent, false);
            var prompt = root.AddComponent<InteractPromptUI>();
            var panel = new GameObject("PromptPanel");
            panel.transform.SetParent(root.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyInteractPromptRegion(rect);
            var panelImg = panel.AddComponent<Image>();
            if (manifest?.interactPromptFrame != null)
            {
                panelImg.sprite = manifest.interactPromptFrame;
                panelImg.type = Image.Type.Sliced;
                panelImg.color = Color.white;
            }
            else
                panelImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            var text = BESUIEditorUtils.CreateText(panel.transform, "PromptText", "Nhấn F để tương tác", Vector2.zero);
            BESUIEditorUtils.SetPrivateField(prompt, "promptRoot", panel);
            BESUIEditorUtils.SetPrivateField(prompt, "promptText", text);
            panel.SetActive(false);
        }

        static void BuildHudNavBar(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("HudNavBar");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyTopNavRegion(rect);
            var nav = go.AddComponent<HudNavBarUI>();

            var evt = BESUIEditorUtils.CreateIconButton(go.transform, "EventBtn", manifest?.navEvent, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing * 5f, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var map = BESUIEditorUtils.CreateIconButton(go.transform, "MapBtn", manifest?.navMap, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing * 4f, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var wish = BESUIEditorUtils.CreateIconButton(go.transform, "WishBtn", manifest?.navWish, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing * 3f, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var team = BESUIEditorUtils.CreateIconButton(go.transform, "TeamBtn", manifest?.navTeam, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing * 2f, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var inv = BESUIEditorUtils.CreateIconButton(go.transform, "InventoryBtn", manifest?.navInventory, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var chr = BESUIEditorUtils.CreateIconButton(go.transform, "CharacterBtn", manifest?.navCharacter, new Vector2(HUDLayoutTokens.NavRightMostX, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var weapon = BESUIEditorUtils.CreateIconButton(go.transform, "WeaponBtn", manifest?.navWeapon, new Vector2(-10, -56), new Vector2(40, 40));
            var art = BESUIEditorUtils.CreateIconButton(go.transform, "ArtifactsBtn", manifest?.navArtifacts, new Vector2(-58, -56), new Vector2(40, 40));
            weapon.gameObject.SetActive(false);
            art.gameObject.SetActive(false);

            BESUIEditorUtils.SetPrivateField(nav, "inventoryButton", inv);
            BESUIEditorUtils.SetPrivateField(nav, "characterButton", chr);
            BESUIEditorUtils.SetPrivateField(nav, "mapButton", map);
            BESUIEditorUtils.SetPrivateField(nav, "weaponButton", weapon);
            BESUIEditorUtils.SetPrivateField(nav, "wishButton", wish);
            BESUIEditorUtils.SetPrivateField(nav, "teamButton", team);
            BESUIEditorUtils.SetPrivateField(nav, "eventButton", evt);
            BESUIEditorUtils.SetPrivateField(nav, "artifactsButton", art);
        }

        static void BuildPartyStrip(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("PartyStrip");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyPartyStripRegion(rect);
            var stripUi = go.AddComponent<PartyStripUI>();
            var frames = new Image[4];
            var portraits = new Image[4];
            var buttons = new Button[4];
            var names = new TMP_Text[4];
            var numbers = new TMP_Text[4];
            var pillFrame = manifest?.partySlotFrame ?? LoadEditorFrame("Rectangle 39782.png");

            for (var i = 0; i < 4; i++)
            {
                var slotGo = new GameObject($"PartySlot{i + 1}");
                slotGo.transform.SetParent(go.transform, false);
                var slotRect = slotGo.AddComponent<RectTransform>();
                slotRect.sizeDelta = HUDLayoutTokens.PartySlotSize;
                slotRect.anchoredPosition = new Vector2(0, 118 - i * HUDLayoutTokens.PartySlotSpacing);
                var img = slotGo.AddComponent<Image>();
                frames[i] = img;
                HUDPrimitiveStyles.ApplySolidPanel(img, HUDPrimitiveStyles.PartyPillBackground);
                HUDPrimitiveStyles.TryApplySmallFrame(img, pillFrame);

                buttons[i] = slotGo.AddComponent<Button>();

                var portraitMaskGo = new GameObject("PortraitMask");
                portraitMaskGo.transform.SetParent(slotGo.transform, false);
                var maskRect = portraitMaskGo.AddComponent<RectTransform>();
                maskRect.sizeDelta = new Vector2(44f, 44f);
                maskRect.anchoredPosition = new Vector2(-58f, 0f);
                var maskImg = portraitMaskGo.AddComponent<Image>();
                maskImg.sprite = HUDPrimitiveStyles.GetMinimapFaceSprite();
                maskImg.color = Color.white;
                var mask = portraitMaskGo.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                var portraitGo = new GameObject("Portrait");
                portraitGo.transform.SetParent(portraitMaskGo.transform, false);
                var pRect = portraitGo.AddComponent<RectTransform>();
                UIAnchorPresets.StretchFull(pRect);
                portraits[i] = portraitGo.AddComponent<Image>();
                portraits[i].color = new Color(0.35f, 0.35f, 0.4f, 0.6f);

                var ringGo = new GameObject("ActiveRing");
                ringGo.transform.SetParent(slotGo.transform, false);
                var ringRect = ringGo.AddComponent<RectTransform>();
                ringRect.sizeDelta = new Vector2(48f, 48f);
                ringRect.anchoredPosition = new Vector2(-58f, 0f);
                var ringImg = ringGo.AddComponent<Image>();
                ringImg.sprite = HUDPrimitiveStyles.GetMinimapRingSprite();
                ringImg.color = new Color(1f, 0.92f, 0.55f, 0f);
                ringImg.raycastTarget = false;

                names[i] = BESUIEditorUtils.CreateText(slotGo.transform, "Name", "Character name", new Vector2(8f, 0f), 13f, TextAlignmentOptions.MidlineLeft);

                var numGo = new GameObject($"PartySlotNumber{i + 1}");
                numGo.transform.SetParent(parent, false);
                var numRect = numGo.AddComponent<RectTransform>();
                numRect.anchorMin = numRect.anchorMax = new Vector2(1f, 0.5f);
                numRect.pivot = new Vector2(1f, 0.5f);
                numRect.sizeDelta = new Vector2(24f, 24f);
                numRect.anchoredPosition = new Vector2(
                    HUDLayoutTokens.PartyNumberScreenInset.x,
                    HUDLayoutTokens.PartyRailPos.y + 118f - i * HUDLayoutTokens.PartySlotSpacing);
                numbers[i] = numGo.AddComponent<TextMeshProUGUI>();
                numbers[i].alignment = TextAlignmentOptions.MidlineRight;
                numbers[i].fontSize = 14f;
                numbers[i].text = (i + 1).ToString();
                numbers[i].color = Color.white;
            }

            BESUIEditorUtils.SetPrivateField(stripUi, "slotFrames", frames);
            BESUIEditorUtils.SetPrivateField(stripUi, "portraits", portraits);
            BESUIEditorUtils.SetPrivateField(stripUi, "slotButtons", buttons);
            BESUIEditorUtils.SetPrivateField(stripUi, "slotNames", names);
            BESUIEditorUtils.SetPrivateField(stripUi, "slotNumbers", numbers);
        }

        static Sprite LoadEditorFrame(string fileName) =>
            BESUIEditorUtils.LoadSpriteInFolder(fileName, UIAssetPaths.Frames);

        static void BuildSkillBar(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("SkillBar");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.ApplySkillBarRegion(rect);
            var skillUi = go.AddComponent<SkillBarUI>();
            var frames = new Image[SkillBarUI.VisibleSlotCount];
            var icons = new Image[SkillBarUI.VisibleSlotCount];
            var cooldowns = new Image[SkillBarUI.VisibleSlotCount];
            var keyLabels = new TMP_Text[SkillBarUI.VisibleSlotCount];
            var layouts = new[]
            {
                (pos: HUDLayoutTokens.SkillZPos, size: HUDLayoutTokens.SkillZSize, key: "Z"),
                (pos: HUDLayoutTokens.SkillEPos, size: HUDLayoutTokens.SkillESize, key: "E"),
                (pos: HUDLayoutTokens.SkillQPos, size: HUDLayoutTokens.SkillQSize, key: "Q"),
            };

            for (var i = 0; i < SkillBarUI.VisibleSlotCount; i++)
            {
                var layout = layouts[i];
                var slotGo = new GameObject($"SkillSlot{i + 1}");
                slotGo.transform.SetParent(go.transform, false);
                var slotRect = slotGo.AddComponent<RectTransform>();
                slotRect.sizeDelta = layout.size;
                slotRect.anchoredPosition = layout.pos;
                var img = slotGo.AddComponent<Image>();
                frames[i] = img;
                HUDPrimitiveStyles.ApplySolidPanel(img, HUDPrimitiveStyles.SlotBackground);
                HUDPrimitiveStyles.TryApplySmallFrame(img, manifest?.skillSlotFrame);

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(slotGo.transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.sizeDelta = layout.size * 0.68f;
                icons[i] = iconGo.AddComponent<Image>();

                var cdGo = new GameObject("Cooldown");
                cdGo.transform.SetParent(slotGo.transform, false);
                var cdRect = cdGo.AddComponent<RectTransform>();
                UIAnchorPresets.StretchFull(cdRect);
                cooldowns[i] = cdGo.AddComponent<Image>();
                cooldowns[i].color = new Color(0, 0, 0, 0.5f);
                cooldowns[i].type = Image.Type.Filled;
                cooldowns[i].fillMethod = Image.FillMethod.Vertical;
                cooldowns[i].fillAmount = 0f;

                var keyGo = BESUIEditorUtils.CreateText(slotGo.transform, "KeyLabel", layout.key, new Vector2(0, -layout.size.y * 0.55f), 11f, TMPro.TextAlignmentOptions.Center);
                keyLabels[i] = keyGo;
                keyGo.color = HUDPrimitiveStyles.SkillKeyLabel;
            }

            BESUIEditorUtils.SetPrivateField(skillUi, "slotFrames", frames);
            BESUIEditorUtils.SetPrivateField(skillUi, "skillIcons", icons);
            BESUIEditorUtils.SetPrivateField(skillUi, "cooldownOverlays", cooldowns);
            BESUIEditorUtils.SetPrivateField(skillUi, "keyLabels", keyLabels);
            go.AddComponent<SkillBarDriver>();
        }

        static RectTransform CreateMapDot(Transform parent, string name, Sprite sprite, Color fallback)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = sprite != null ? new Vector2(16, 16) : new Vector2(10, 10);
            var img = go.AddComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
            }
            else
                img.color = fallback;
            return rect;
        }

        static InventoryUI BuildInventoryPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "InventoryUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgInventory), UIScreenBackgroundId.Inventory);
            var inv = go.AddComponent<InventoryUI>();
            BESUIEditorUtils.CreateText(go.transform, "Title", "Inventory", new Vector2(0, 420), 24f);
            var itemsTab = BESUIEditorUtils.CreateButton(go.transform, "ItemsTab", "Items", new Vector2(-520, 360), new Vector2(120, 36));
            var matsTab = BESUIEditorUtils.CreateButton(go.transform, "MaterialsTab", "Materials", new Vector2(-380, 360), new Vector2(120, 36));
            var closeBtn = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));

            var gridGo = new GameObject("ItemGrid");
            gridGo.transform.SetParent(go.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyInventoryGrid(gridRect);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(96, 96);
            grid.spacing = new Vector2(12, 12);

            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIItemSlot.prefab");
            var row = BESUIEditorUtils.CreateText(go.transform, "ItemRowPrefab", "Item x1", new Vector2(420, 0), 16f, TextAlignmentOptions.Left);
            row.gameObject.SetActive(false);

            BESUIEditorUtils.SetPrivateField(inv, "panel", go);
            BESUIEditorUtils.SetPrivateField(inv, "listContainer", gridGo.transform);
            BESUIEditorUtils.SetPrivateField(inv, "itemRowPrefab", row);
            BESUIEditorUtils.SetPrivateField(inv, "closeButton", closeBtn);
            BESUIEditorUtils.SetPrivateField(inv, "itemsTabButton", itemsTab);
            BESUIEditorUtils.SetPrivateField(inv, "materialsTabButton", matsTab);
            if (slotPrefab != null)
                BESUIEditorUtils.SetPrivateField(inv, "itemSlotPrefab", slotPrefab);
            go.SetActive(false);
            return inv;
        }

        static CharacterProfileUI BuildCharacterPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "CharacterProfileUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgCharacterProfile), UIScreenBackgroundId.CharacterProfile);
            var profile = go.AddComponent<CharacterProfileUI>();
            var previewGo = new GameObject("CharacterPreview");
            previewGo.transform.SetParent(go.transform, false);
            var previewRect = previewGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyCharacterPreview(previewRect);
            BESUIEditorUtils.CreateBackground(previewGo.transform, BESUIEditorUtils.LoadBg(UIAssetPaths.BgSoonviewCharacter), "PreviewBg");
            BESUIEditorUtils.AttachScreenBackground(previewGo, UIScreenBackgroundId.CharacterPreview, false);
            var rawGo = new GameObject("PreviewImage");
            rawGo.transform.SetParent(previewGo.transform, false);
            var rawRect = rawGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rawRect);
            var rawImg = rawGo.AddComponent<RawImage>();
            var previewRenderer = previewGo.AddComponent<CharacterPreviewRenderer>();
            BESUIEditorUtils.SetPrivateField(previewRenderer, "targetImage", rawImg);

            var statsGo = new GameObject("StatsPanel");
            statsGo.transform.SetParent(go.transform, false);
            var statsRect = statsGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyCharacterStats(statsRect);
            var name = BESUIEditorUtils.CreateText(statsGo.transform, "Name", "NAME CHARACTER", new Vector2(0, 160), 24f, TextAlignmentOptions.Left);
            var level = BESUIEditorUtils.CreateText(statsGo.transform, "Level", "Lv.99 / 100", new Vector2(0, 120), 18f, TextAlignmentOptions.Left);
            var atk = BESUIEditorUtils.CreateText(statsGo.transform, "ATK", "ATK: 9999", new Vector2(0, 60), 16f, TextAlignmentOptions.Left);
            var hp = BESUIEditorUtils.CreateText(statsGo.transform, "HP", "HP: 9999", new Vector2(0, 30), 16f, TextAlignmentOptions.Left);
            var def = BESUIEditorUtils.CreateText(statsGo.transform, "DEF", "DEF: 9999", new Vector2(0, 0), 16f, TextAlignmentOptions.Left);
            var critRate = BESUIEditorUtils.CreateText(statsGo.transform, "CritRate", "Crit Rate: 99%", new Vector2(0, -30), 16f, TextAlignmentOptions.Left);
            var critDmg = BESUIEditorUtils.CreateText(statsGo.transform, "CritDmg", "Crit DMG: 999%", new Vector2(0, -60), 16f, TextAlignmentOptions.Left);
            var closeBtn = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));

            var partyTexts = new TMP_Text[4];
            for (var i = 0; i < 4; i++)
                partyTexts[i] = BESUIEditorUtils.CreateText(go.transform, $"PartySlot{i + 1}", $"{i + 1:D2} Character", new Vector2(-760, 200 - i * 80), 14f, TextAlignmentOptions.Left);

            var equipGo = new GameObject("EquipmentUI");
            equipGo.transform.SetParent(statsGo.transform, false);
            var equip = equipGo.AddComponent<EquipmentUI>();
            var weaponName = BESUIEditorUtils.CreateText(equipGo.transform, "WeaponName", "Iron Sword", new Vector2(0, -100), 16f, TextAlignmentOptions.Left);
            var weaponAtk = BESUIEditorUtils.CreateText(equipGo.transform, "WeaponAtk", "ATK: 120", new Vector2(0, -130), 16f, TextAlignmentOptions.Left);
            BESUIEditorUtils.SetPrivateField(equip, "weaponNameText", weaponName);
            BESUIEditorUtils.SetPrivateField(equip, "weaponAtkText", weaponAtk);

            BESUIEditorUtils.SetPrivateField(profile, "panel", go);
            BESUIEditorUtils.SetPrivateField(profile, "nameText", name);
            BESUIEditorUtils.SetPrivateField(profile, "levelText", level);
            BESUIEditorUtils.SetPrivateField(profile, "atkText", atk);
            BESUIEditorUtils.SetPrivateField(profile, "hpText", hp);
            BESUIEditorUtils.SetPrivateField(profile, "defText", def);
            BESUIEditorUtils.SetPrivateField(profile, "critRateText", critRate);
            BESUIEditorUtils.SetPrivateField(profile, "critDmgText", critDmg);
            BESUIEditorUtils.SetPrivateField(profile, "equipmentUI", equip);
            BESUIEditorUtils.SetPrivateField(profile, "partySlotTexts", partyTexts);
            BESUIEditorUtils.SetPrivateField(profile, "previewRenderer", previewRenderer);
            BESUIEditorUtils.SetPrivateField(profile, "closeButton", closeBtn);
            go.SetActive(false);
            return profile;
        }

        static GameMapUI BuildWorldMapPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "GameMapUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgEventScene), UIScreenBackgroundId.WorldMap);
            var map = go.AddComponent<GameMapUI>();
            BESUIEditorUtils.CreateText(go.transform, "Title", "World Map", new Vector2(0, 420), 24f);
            var markersGo = new GameObject("MapMarkers");
            markersGo.transform.SetParent(go.transform, false);
            var mRect = markersGo.AddComponent<RectTransform>();
            UIAnchorPresets.Center(mRect, new Vector2(600, 400));
            var closeBtn = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIMapMarker.prefab");
            BESUIEditorUtils.SetPrivateField(map, "panel", go);
            BESUIEditorUtils.SetPrivateField(map, "markersContainer", markersGo.transform);
            BESUIEditorUtils.SetPrivateField(map, "mapMarkerPrefab", markerPrefab);
            BESUIEditorUtils.SetPrivateField(map, "closeButton", closeBtn);
            go.SetActive(false);
            return map;
        }

        static WeaponScreenUI BuildWeaponPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "WeaponScreenUI",
                BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeaponInfo) ?? BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeapon),
                UIScreenBackgroundId.Weapon);
            var weapon = go.AddComponent<WeaponScreenUI>();
            var grid = new GameObject("GridContainer");
            grid.transform.SetParent(go.transform, false);
            var gridRect = grid.AddComponent<RectTransform>();
            gridRect.anchoredPosition = new Vector2(-620, 0);
            gridRect.sizeDelta = new Vector2(360, 600);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(80, 80);
            layout.spacing = new Vector2(8, 8);

            var name = BESUIEditorUtils.CreateText(go.transform, "WeaponName", "Name of Weapon", new Vector2(420, 220), 22f, TextAlignmentOptions.Left);
            var desc = BESUIEditorUtils.CreateText(go.transform, "WeaponDesc", "Weapon description", new Vector2(420, 160), 14f, TextAlignmentOptions.TopLeft);
            var atk = BESUIEditorUtils.CreateText(go.transform, "ATK", "ATK 9999", new Vector2(420, 100), 16f, TextAlignmentOptions.Left);
            var hp = BESUIEditorUtils.CreateText(go.transform, "HP", "HP 9999", new Vector2(420, 70), 16f, TextAlignmentOptions.Left);
            var level = BESUIEditorUtils.CreateText(go.transform, "Level", "Lv. 100 / 100", new Vector2(420, 40), 16f, TextAlignmentOptions.Left);
            var refine = BESUIEditorUtils.CreateText(go.transform, "Refine", "Refinement Rank 5", new Vector2(420, 10), 16f, TextAlignmentOptions.Left);
            var switchBtn = BESUIEditorUtils.CreateButton(go.transform, "SwitchBtn", "Switch", new Vector2(300, -180), new Vector2(140, 40));
            var removeBtn = BESUIEditorUtils.CreateButton(go.transform, "RemoveBtn", "Remove", new Vector2(480, -180), new Vector2(140, 40));
            var enhanceBtn = BESUIEditorUtils.CreateButton(go.transform, "EnhanceBtn", "Enhance", new Vector2(390, -240), new Vector2(160, 40));
            var closeBtn = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));

            BESUIEditorUtils.SetPrivateField(weapon, "root", go);
            BESUIEditorUtils.SetPrivateField(weapon, "gridContainer", grid.transform);
            BESUIEditorUtils.SetPrivateField(weapon, "weaponNameText", name);
            BESUIEditorUtils.SetPrivateField(weapon, "weaponDescText", desc);
            BESUIEditorUtils.SetPrivateField(weapon, "atkText", atk);
            BESUIEditorUtils.SetPrivateField(weapon, "hpText", hp);
            BESUIEditorUtils.SetPrivateField(weapon, "levelText", level);
            BESUIEditorUtils.SetPrivateField(weapon, "refineText", refine);
            BESUIEditorUtils.SetPrivateField(weapon, "switchButton", switchBtn);
            BESUIEditorUtils.SetPrivateField(weapon, "removeButton", removeBtn);
            BESUIEditorUtils.SetPrivateField(weapon, "enhanceButton", enhanceBtn);
            BESUIEditorUtils.SetPrivateField(weapon, "closeButton", closeBtn);
            go.SetActive(false);
            return weapon;
        }

        static ArtifactsUI BuildArtifactsPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "ArtifactsUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgArtifacts), UIScreenBackgroundId.Artifacts);
            var ui = go.AddComponent<ArtifactsUI>();
            var grid = new GameObject("GridContainer");
            grid.transform.SetParent(go.transform, false);
            var gridRect = grid.AddComponent<RectTransform>();
            gridRect.anchoredPosition = new Vector2(-500, 0);
            gridRect.sizeDelta = new Vector2(400, 500);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(72, 72);
            layout.spacing = new Vector2(8, 8);
            var detailName = BESUIEditorUtils.CreateText(go.transform, "DetailName", "Artifact", new Vector2(300, 120), 20f, TextAlignmentOptions.Left);
            var detailDesc = BESUIEditorUtils.CreateText(go.transform, "DetailDesc", "Description", new Vector2(300, 40), 14f, TextAlignmentOptions.TopLeft);
            var equipBtn = BESUIEditorUtils.CreateButton(go.transform, "EquipBtn", "Equip", new Vector2(300, -120), new Vector2(140, 40));
            var closeBtn = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "gridContainer", grid.transform);
            BESUIEditorUtils.SetPrivateField(ui, "detailNameText", detailName);
            BESUIEditorUtils.SetPrivateField(ui, "detailDescText", detailDesc);
            BESUIEditorUtils.SetPrivateField(ui, "equipButton", equipBtn);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", closeBtn);
            go.SetActive(false);
            return ui;
        }

        static TeamSetupUI BuildTeamPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "TeamSetupUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgTeamSetup), UIScreenBackgroundId.TeamSetup);
            var ui = go.AddComponent<TeamSetupUI>();
            var slotsGo = new GameObject("TeamSlots");
            slotsGo.transform.SetParent(go.transform, false);
            var sRect = slotsGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyTeamSlotRow(sRect);
            var layout = slotsGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            var pickerGo = new GameObject("RosterPicker");
            pickerGo.transform.SetParent(go.transform, false);
            var pRect = pickerGo.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0, -200);
            pRect.sizeDelta = new Vector2(400, 120);
            var pickerLayout = pickerGo.AddComponent<HorizontalLayoutGroup>();
            pickerLayout.spacing = 8;
            var teamSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UITeamSlot.prefab");
            var confirm = BESUIEditorUtils.CreateButton(go.transform, "ConfirmBtn", "Confirm", new Vector2(0, -360), new Vector2(180, 44));
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "slotsContainer", slotsGo.transform);
            BESUIEditorUtils.SetPrivateField(ui, "teamSlotPrefab", teamSlotPrefab);
            BESUIEditorUtils.SetPrivateField(ui, "rosterPickerContainer", pickerGo.transform);
            BESUIEditorUtils.SetPrivateField(ui, "confirmButton", confirm);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static EventUI BuildEventPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "EventUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgEventCheckIn), UIScreenBackgroundId.EventCheckIn);
            var ui = go.AddComponent<EventUI>();
            var title = BESUIEditorUtils.CreateText(go.transform, "Title", "Daily Check-In", new Vector2(0, 320), 24f);
            var desc = BESUIEditorUtils.CreateText(go.transform, "Desc", "Claim your daily reward.", new Vector2(0, 260), 16f);
            var daysGo = new GameObject("DaySlots");
            daysGo.transform.SetParent(go.transform, false);
            var dRect = daysGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyEventDayRow(dRect);
            var dayLayout = daysGo.AddComponent<HorizontalLayoutGroup>();
            dayLayout.spacing = 8;
            dayLayout.childAlignment = TextAnchor.MiddleCenter;
            var dayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIDayCheckInSlot.prefab");
            var checkIn = BESUIEditorUtils.CreateButton(go.transform, "CheckInBtn", "Check In", new Vector2(0, 80), new Vector2(200, 52), UIAnchorPresets.ApplyEventCheckInBtn);
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "eventData", AssetDatabase.LoadAssetAtPath<EventDefinition>("Assets/_Project/Data/UI/DefaultEvent.asset"));
            BESUIEditorUtils.SetPrivateField(ui, "titleText", title);
            BESUIEditorUtils.SetPrivateField(ui, "descText", desc);
            BESUIEditorUtils.SetPrivateField(ui, "daySlotsContainer", daysGo.transform);
            BESUIEditorUtils.SetPrivateField(ui, "daySlotPrefab", dayPrefab);
            BESUIEditorUtils.SetPrivateField(ui, "checkInButton", checkIn);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static WishUI BuildWishPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "WishUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgWish), UIScreenBackgroundId.Wish);
            var ui = go.AddComponent<WishUI>();
            var banner = BESUIEditorUtils.CreateText(go.transform, "Banner", "Character Wish", new Vector2(0, 360), 24f);
            var coins = BESUIEditorUtils.CreateText(go.transform, "Coins", "Money 99999", new Vector2(600, 460), 16f, TextAlignmentOptions.Right);
            var gems = BESUIEditorUtils.CreateText(go.transform, "Gems", "GEM 1600", new Vector2(760, 460), 16f, TextAlignmentOptions.Right);
            InstantiateAtom(go.transform, "CoinPill", UIAssetPaths.AtomPrefabs + "/UICurrencyPill.prefab", new Vector2(560, 460), new Vector2(160, 36));
            InstantiateAtom(go.transform, "GemPill", UIAssetPaths.AtomPrefabs + "/UICurrencyPill.prefab", new Vector2(720, 460), new Vector2(160, 36));
            var result = BESUIEditorUtils.CreateText(go.transform, "Result", "Select Wish x1 or x10", new Vector2(0, -200), 16f);
            var resultGo = new GameObject("ResultCards");
            resultGo.transform.SetParent(go.transform, false);
            var rRect = resultGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyWishResultPanel(rRect);
            var rLayout = resultGo.AddComponent<HorizontalLayoutGroup>();
            rLayout.spacing = 8;
            var resultCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIResultCard.prefab");
            var one = BESUIEditorUtils.CreateButton(go.transform, "WishOne", "Wish x1", new Vector2(-120, 120), new Vector2(160, 44), UIAnchorPresets.ApplyWishPullButtons);
            var ten = BESUIEditorUtils.CreateButton(go.transform, "WishTen", "Wish x10", new Vector2(120, 120), new Vector2(160, 44));
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "banner", AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>("Assets/_Project/Data/UI/DefaultGachaBanner.asset"));
            BESUIEditorUtils.SetPrivateField(ui, "bannerText", banner);
            BESUIEditorUtils.SetPrivateField(ui, "coinsText", coins);
            BESUIEditorUtils.SetPrivateField(ui, "gemsText", gems);
            BESUIEditorUtils.SetPrivateField(ui, "resultText", result);
            BESUIEditorUtils.SetPrivateField(ui, "resultCardsContainer", resultGo.transform);
            BESUIEditorUtils.SetPrivateField(ui, "resultCardPrefab", resultCardPrefab);
            BESUIEditorUtils.SetPrivateField(ui, "wishOneButton", one);
            BESUIEditorUtils.SetPrivateField(ui, "wishTenButton", ten);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static DialogueUI BuildDialoguePanel(Transform parent)
        {
            var dialogueGo = new GameObject("DialogueUI");
            dialogueGo.transform.SetParent(parent, false);
            var dialogue = dialogueGo.AddComponent<DialogueUI>();
            var panel = CreateFullScreenPanel(dialogueGo.transform, "DialoguePanel", BESUIEditorUtils.LoadBg(UIAssetPaths.BgInteraction), UIScreenBackgroundId.Dialogue);
            panel.GetComponent<RectTransform>().anchorMin = new Vector2(0.05f, 0.02f);
            panel.GetComponent<RectTransform>().anchorMax = new Vector2(0.95f, 0.38f);
            panel.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            panel.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            panel.SetActive(false);

            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(panel.transform, false);
            var portraitRect = portraitGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyDialoguePortrait(portraitRect);
            var portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.sprite = BESUIEditorUtils.LoadSpriteFlexible("image 306.png");
            portraitImg.preserveAspect = true;
            portraitImg.color = Color.white;

            var speaker = BESUIEditorUtils.CreateText(panel.transform, "Speaker", "NPC", new Vector2(-500, 120), 18f, TextAlignmentOptions.Left);
            var line = BESUIEditorUtils.CreateText(panel.transform, "Line", "...", new Vector2(-500, 60), 16f, TextAlignmentOptions.TopLeft);
            var chatInputGo = new GameObject("ChatInput");
            chatInputGo.transform.SetParent(panel.transform, false);
            var chatRect = chatInputGo.AddComponent<RectTransform>();
            chatRect.sizeDelta = new Vector2(420, 36);
            chatRect.anchoredPosition = new Vector2(-120, -60);
            chatInputGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(chatInputGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(textRect);
            textRect.offsetMin = new Vector2(8, 4);
            textRect.offsetMax = new Vector2(-8, -4);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            var input = chatInputGo.AddComponent<TMP_InputField>();
            input.textComponent = text;
            var sendBtn = BESUIEditorUtils.CreateButton(panel.transform, "SendButton", "Send", new Vector2(180, -60), new Vector2(100, 36));
            var closeBtn = BESUIEditorUtils.CreateButton(panel.transform, "CloseButton", "Close", new Vector2(320, -60), new Vector2(100, 36));
            var choices = new GameObject("Choices").transform;
            choices.SetParent(panel.transform, false);
            var choiceBtn = BESUIEditorUtils.CreateButton(choices, "ChoiceTemplate", "Choice", Vector2.zero, new Vector2(200, 36));
            choiceBtn.gameObject.SetActive(false);

            BESUIEditorUtils.SetPrivateField(dialogue, "panel", panel);
            BESUIEditorUtils.SetPrivateField(dialogue, "speakerText", speaker);
            BESUIEditorUtils.SetPrivateField(dialogue, "dialogueText", line);
            BESUIEditorUtils.SetPrivateField(dialogue, "chatInput", input);
            BESUIEditorUtils.SetPrivateField(dialogue, "sendButton", sendBtn);
            BESUIEditorUtils.SetPrivateField(dialogue, "closeButton", closeBtn);
            BESUIEditorUtils.SetPrivateField(dialogue, "choicesContainer", choices);
            BESUIEditorUtils.SetPrivateField(dialogue, "choiceButtonPrefab", choiceBtn);
            return dialogue;
        }

        static LoadingScreenUI BuildLoadingPanel(Transform parent)
        {
            var bg = BESUIEditorUtils.LoadBg(UIAssetPaths.BgLoading)
                ?? BESUIEditorUtils.LoadBg(UIAssetPaths.BgLoadingDots);
            var go = CreateFullScreenPanel(parent, "LoadingScreenUI", bg, UIScreenBackgroundId.Loading);
            var ui = go.AddComponent<LoadingScreenUI>();
            var barGo = new GameObject("ProgressBarHost");
            barGo.transform.SetParent(go.transform, false);
            var barRect = barGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyLoadingProgress(barRect);
            var bar = BESUIEditorUtils.CreateFilledSlider(barGo.transform, "ProgressBar", Vector2.zero, new Vector2(520, 20), new Color(0.95f, 0.78f, 0.28f));
            var status = BESUIEditorUtils.CreateText(go.transform, "Status", "Loading...", new Vector2(0, 240), 18f);
            var tip = BESUIEditorUtils.CreateText(go.transform, "Tip", "Tip: Explore the open world.", new Vector2(0, 160), 14f);
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "progressBar", bar);
            BESUIEditorUtils.SetPrivateField(ui, "statusText", status);
            BESUIEditorUtils.SetPrivateField(ui, "tipText", tip);
            go.SetActive(false);
            return ui;
        }

        static WeaponEnhanceUI BuildWeaponEnhancePanel(Transform parent)
        {
            var bg = BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeaponEnhance)
                ?? BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeaponEnhanceAlt);
            var go = CreateFullScreenPanel(parent, "WeaponEnhanceUI", bg, UIScreenBackgroundId.WeaponEnhance);
            var ui = go.AddComponent<WeaponEnhanceUI>();
            var before = BESUIEditorUtils.CreateText(go.transform, "BeforeAtk", "ATK 120", new Vector2(-200, 40), 18f);
            var after = BESUIEditorUtils.CreateText(go.transform, "AfterAtk", "ATK 144", new Vector2(200, 40), 18f);
            var mats = BESUIEditorUtils.CreateText(go.transform, "Materials", "Materials consumed", new Vector2(0, -80), 16f);
            var confirm = BESUIEditorUtils.CreateButton(go.transform, "ConfirmBtn", "Enhance", new Vector2(0, -180), new Vector2(160, 44));
            var back = BESUIEditorUtils.CreateButton(go.transform, "BackBtn", "Back", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "beforeAtkText", before);
            BESUIEditorUtils.SetPrivateField(ui, "afterAtkText", after);
            BESUIEditorUtils.SetPrivateField(ui, "materialsText", mats);
            BESUIEditorUtils.SetPrivateField(ui, "confirmButton", confirm);
            BESUIEditorUtils.SetPrivateField(ui, "backButton", back);
            go.SetActive(false);
            return ui;
        }

        static WeaponRankUpUI BuildWeaponRankUpPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "WeaponRankUpUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeaponRankUp), UIScreenBackgroundId.WeaponRankUp);
            var ui = go.AddComponent<WeaponRankUpUI>();
            var rank = BESUIEditorUtils.CreateText(go.transform, "Rank", "Rank Up", new Vector2(0, 120), 22f);
            var result = BESUIEditorUtils.CreateText(go.transform, "Result", "Success!", new Vector2(0, 40), 16f);
            var confirm = BESUIEditorUtils.CreateButton(go.transform, "ConfirmBtn", "Continue", new Vector2(0, -120), new Vector2(160, 44));
            var back = BESUIEditorUtils.CreateButton(go.transform, "BackBtn", "Back", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "rankText", rank);
            BESUIEditorUtils.SetPrivateField(ui, "resultText", result);
            BESUIEditorUtils.SetPrivateField(ui, "confirmButton", confirm);
            BESUIEditorUtils.SetPrivateField(ui, "backButton", back);
            go.SetActive(false);
            return ui;
        }

        static WeaponRefineUI BuildWeaponRefinePanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "WeaponRefineUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeaponRefine), UIScreenBackgroundId.WeaponRefine);
            var ui = go.AddComponent<WeaponRefineUI>();
            var level = BESUIEditorUtils.CreateText(go.transform, "RefineLevel", "Refinement Rank 5", new Vector2(0, 80), 20f);
            var effect = BESUIEditorUtils.CreateText(go.transform, "Effect", "+12% ATK", new Vector2(0, 20), 16f);
            var done = BESUIEditorUtils.CreateButton(go.transform, "DoneBtn", "Done", new Vector2(0, -120), new Vector2(160, 44));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "refineLevelText", level);
            BESUIEditorUtils.SetPrivateField(ui, "effectText", effect);
            BESUIEditorUtils.SetPrivateField(ui, "doneButton", done);
            go.SetActive(false);
            return ui;
        }

        static PlayerProfileUI BuildPlayerProfilePanel(Transform parent)
        {
            var bg = BESUIEditorUtils.LoadBg(UIAssetPaths.BgUsernamePlayer)
                ?? BESUIEditorUtils.LoadBg(UIAssetPaths.BgPersonal);
            var go = CreateFullScreenPanel(parent, "PlayerProfileUI", bg, UIScreenBackgroundId.PlayerProfile);
            var ui = go.AddComponent<PlayerProfileUI>();
            var username = BESUIEditorUtils.CreateText(go.transform, "Username", "Username PLayer", new Vector2(0, 80), 20f);
            var server = BESUIEditorUtils.CreateText(go.transform, "Server", "Server: Asian", new Vector2(0, 20), 16f);
            var uid = BESUIEditorUtils.CreateText(go.transform, "UID", "UID: 100000001", new Vector2(0, -20), 14f);
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(360, 200), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "usernameText", username);
            BESUIEditorUtils.SetPrivateField(ui, "serverText", server);
            BESUIEditorUtils.SetPrivateField(ui, "uidText", uid);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static SettingsUI BuildSettingsPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "SettingsUI", null);
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.92f);
            var ui = go.AddComponent<SettingsUI>();
            var panelGo = new GameObject("SettingsContent");
            panelGo.transform.SetParent(go.transform, false);
            var pRect = panelGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplySettingsPanel(pRect);
            var volRow = InstantiateAtom(panelGo.transform, "VolumeRow", UIAssetPaths.AtomPrefabs + "/UISettingsRow.prefab", new Vector2(0, 40), new Vector2(400, 40));
            var fsRow = InstantiateAtom(panelGo.transform, "FullscreenRow", UIAssetPaths.AtomPrefabs + "/UISettingsRow.prefab", new Vector2(0, -20), new Vector2(400, 40));
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            if (volRow != null) BESUIEditorUtils.SetPrivateField(ui, "volumeRow", volRow.GetComponent<UISettingsRow>());
            if (fsRow != null) BESUIEditorUtils.SetPrivateField(ui, "fullscreenRow", fsRow.GetComponent<UISettingsRow>());
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static ServerPickerUI BuildServerPickerPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "ServerPickerUI", null);
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
            var ui = go.AddComponent<ServerPickerUI>();
            BESUIEditorUtils.CreateText(go.transform, "Title", "Select Server", new Vector2(0, 120), 22f);
            var optionsGo = new GameObject("ServerOptions");
            optionsGo.transform.SetParent(go.transform, false);
            var oRect = optionsGo.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyServerPickerPanel(oRect);
            var layout = optionsGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            var serverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIServerOption.prefab");
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "optionsContainer", optionsGo.transform);
            BESUIEditorUtils.SetPrivateField(ui, "serverOptionPrefab", serverPrefab);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static GameObject CreateFullScreenPanel(Transform parent, string name, Sprite bgSprite, UIScreenBackgroundId? screenId = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            if (bgSprite != null)
                BESUIEditorUtils.CreateBackground(go.transform, bgSprite, "Background");
            else
                go.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            if (screenId.HasValue)
                BESUIEditorUtils.AttachScreenBackground(go, screenId.Value);

            return go;
        }

        static RectTransform CreateIcon(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(10, 10);
            go.AddComponent<Image>().color = color;
            return rect;
        }

        static GameObject InstantiateAtom(Transform parent, string name, string prefabPath, Vector2 pos, Vector2 size)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            var rect = instance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = pos;
                rect.sizeDelta = size;
            }

            return instance;
        }
    }
}
#endif
