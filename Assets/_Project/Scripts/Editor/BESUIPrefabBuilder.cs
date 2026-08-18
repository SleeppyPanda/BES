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
            Debug.LogWarning("[BES] UI Prefab Builder is disabled. Edit UI prefabs directly in Unity.");
        }

        public static void BuildAllPrefabs()
        {
            Debug.LogWarning("[BES] BuildAllPrefabs skipped. Edit UI prefabs directly in Unity.");
        }

        public static void BuildAtomPrefabs() =>
            Debug.LogWarning("[BES] BuildAtomPrefabs skipped. Edit atom prefabs directly in Unity.");

        public static GameObject InstantiateGameplayHud()
        {
            var path = UIAssetPaths.ScreenPrefabs + "/GameplayHUD.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[BES] Missing prefab at {path}. UI builder is disabled; create/fix it in Unity.");
                return null;
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        public static GameObject InstantiateMainMenu()
        {
            var path = UIAssetPaths.ScreenPrefabs + "/MainMenuScreen.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[BES] Missing prefab at {path}. UI builder is disabled; create/fix it in Unity.");
                return null;
            }

            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        public static void BuildMainMenuPrefab()
        {
            Debug.LogWarning("[BES] BuildMainMenuPrefab skipped. Edit MainMenuScreen.prefab directly in Unity.");
        }

        public static void BuildGameplayHudPrefab()
        {
            Debug.LogWarning("[BES] BuildGameplayHudPrefab skipped. Edit GameplayHUD.prefab directly in Unity.");
        }

        static GameObject BuildMainMenuRoot()
        {
            var canvasGo = BESUIEditorUtils.CreateCanvasRoot("MainMenuScreen", out _);
            BESUIEditorUtils.CreateBackground(canvasGo.transform, BESUIEditorUtils.LoadBg(UIAssetPaths.BgStart));
            BESUIEditorUtils.AttachScreenBackground(canvasGo, UIScreenBackgroundId.MainMenu, false);

            var controllerGo = new GameObject("MainMenuController");
            controllerGo.transform.SetParent(canvasGo.transform, false);
            var controller = controllerGo.AddComponent<MainMenuController>();

            var logo = CreateMainMenuLogo(canvasGo.transform);
            var clickBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "ClickToBegin", new Vector2(0, 500), new Vector2(560, 72));
            var regionBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "RegionButton", new Vector2(0, 320), new Vector2(220, 52));
            var eventBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "EventButton", new Vector2(812, -412), new Vector2(56, 56));
            var quitBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "QuitButton", new Vector2(-884, -492), new Vector2(56, 56));
            var profileBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "ProfileButton", new Vector2(812, -332), new Vector2(56, 56));
            var settingsBtn = BESUIEditorUtils.CreateMenuHitArea(canvasGo.transform, "SettingsButton", new Vector2(812, -492), new Vector2(56, 56));
            AddButtonLabel(clickBtn.transform, "Click to begin", 18f);
            AddButtonLabel(regionBtn.transform, "Region", 18f);
            AddButtonLabel(eventBtn.transform, "Event", 14f);
            AddButtonLabel(quitBtn.transform, "Logout", 13f);
            AddButtonLabel(profileBtn.transform, "Account", 13f);
            AddButtonLabel(settingsBtn.transform, "Settings", 12f);

            var profilePanel = BuildPlayerProfilePanel(canvasGo.transform);
            var settingsPanel = BuildSettingsPanel(canvasGo.transform);
            var serverPicker = BuildServerPickerPanel(canvasGo.transform);
            var eventPanel = BuildEventPanel(canvasGo.transform);

            BESUIEditorUtils.SetPrivateField(controller, "logoObject", logo);
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

        static GameObject CreateMainMenuLogo(Transform parent)
        {
            var logoGo = new GameObject("Logo");
            logoGo.transform.SetParent(parent, false);
            var rect = logoGo.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, new Vector2(870, 578));
            rect.anchoredPosition = new Vector2(0, 196);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/UI - UX/Logo game.png");
            var image = logoGo.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return logoGo;
        }

        static void AddButtonLabel(Transform parent, string label, float fontSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            rect.offsetMin = new Vector2(4f, 2f);
            rect.offsetMax = new Vector2(-4f, -2f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
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
            BuildHudCornerButtons(hudLayer.transform, manifest);
            BuildHudNavBar(hudLayer.transform, manifest);
            BuildPartyStrip(hudLayer.transform, manifest);
            BuildSkillBar(hudLayer.transform, manifest);
            BuildChatBox(hudLayer.transform, manifest);

            // Layer 1 overlays
            var overlayLayer = CreateChild(canvasGo.transform, "OverlayLayer");
            var inventory = BuildInventoryPanel(overlayLayer.transform);
            var character = BuildCharacterPanel(overlayLayer.transform);
            var map = BuildWorldMapPanel(overlayLayer.transform, manifest);

            // Layer 2 meta
            var metaLayer = CreateChild(canvasGo.transform, "MetaLayer");
            var evt = BuildEventPanel(metaLayer.transform);
            var battlePass = BuildBattlePassPanel(metaLayer.transform);
            var wish = BuildWishPanel(metaLayer.transform);
            var settings = BuildSettingsPanel(metaLayer.transform);

            // Layer 3 modals + weapon flow
            var modalLayer = CreateChild(canvasGo.transform, "ModalLayer");
            var dialogue = BuildDialoguePanel(modalLayer.transform);
            var loading = BuildLoadingPanel(modalLayer.transform);

            BESUIEditorUtils.SetPrivateField(nav, "hud", hudLayer.GetComponentInChildren<HUDController>(true));
            BESUIEditorUtils.SetPrivateField(nav, "miniMap", hudLayer.GetComponentInChildren<MiniMapUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "questLogUI", hudLayer.GetComponentInChildren<QuestLogUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "interactPrompt", hudLayer.GetComponentInChildren<InteractPromptUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "hudNavBar", hudLayer.GetComponentInChildren<HudNavBarUI>(true));
            BESUIEditorUtils.SetPrivateField(nav, "inventoryUI", inventory);
            BESUIEditorUtils.SetPrivateField(nav, "characterProfileUI", character);
            BESUIEditorUtils.SetPrivateField(nav, "gameMapUI", map);
            BESUIEditorUtils.SetPrivateField(nav, "eventUI", evt);
            BESUIEditorUtils.SetPrivateField(nav, "battlePassUI", battlePass);
            BESUIEditorUtils.SetPrivateField(nav, "wishUI", wish);
            BESUIEditorUtils.SetPrivateField(nav, "settingsUI", settings);
            BESUIEditorUtils.SetPrivateField(nav, "dialogueUI", dialogue);
            BESUIEditorUtils.SetPrivateField(nav, "loadingScreenUI", loading);

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

        static RawImage AddRawArtworkPanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, size);
            rect.anchoredPosition = anchoredPosition;

            var raw = go.AddComponent<RawImage>();
            raw.texture = null;
            raw.color = color ?? new Color(1f, 1f, 1f, 0f);
            raw.raycastTarget = false;
            return raw;
        }

        static RawImage AddStretchRawArtworkPanel(Transform parent, string name, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);

            var raw = go.AddComponent<RawImage>();
            raw.texture = null;
            raw.color = color ?? new Color(1f, 1f, 1f, 0f);
            raw.raycastTarget = false;
            return raw;
        }

        static void BuildHudSection(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyHudBarsRegion(rect);

            var hud = go.AddComponent<HUDController>();

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

            var stamina = BESUIEditorUtils.CreateFilledSlider(go.transform, "StaminaBar", new Vector2(HUDLayoutTokens.HealthBarPos.x, HUDLayoutTokens.HealthBarPos.y - 24f), new Vector2(HUDLayoutTokens.HealthBarSize.x, 10f),
                null, null, HUDPrimitiveStyles.StaminaBarFill);
            ApplyPrimitiveBar(stamina, HUDPrimitiveStyles.HpBarBackground, HUDPrimitiveStyles.StaminaBarFill);
            var staminaValue = BESUIEditorUtils.CreateText(stamina.transform, "StaminaValue", "100/100", Vector2.zero, 12f, TextAlignmentOptions.Center);
            staminaValue.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            var region = BESUIEditorUtils.CreateText(go.transform, "RegionText", string.Empty, HUDLayoutTokens.RegionTextPos, 13f, TextAlignmentOptions.BottomLeft);
            region.color = new Color(1f, 1f, 1f, 0.75f);

            BESUIEditorUtils.SetPrivateField(hud, "healthBar", health);
            BESUIEditorUtils.SetPrivateField(hud, "staminaBar", stamina);
            BESUIEditorUtils.SetPrivateField(hud, "hpValueText", hpValue);
            BESUIEditorUtils.SetPrivateField(hud, "staminaValueText", staminaValue);
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
            var tracker = go.AddComponent<QuestTrackerUI>();
            TopLeftHudWidgets.ApplyQuestTracker(go.transform, manifest);

            var imageGo = new GameObject("QuestImage");
            imageGo.transform.SetParent(go.transform, false);
            var imageRect = imageGo.AddComponent<RectTransform>();
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(0f, 1f);
            imageRect.pivot = new Vector2(0f, 1f);
            imageRect.anchoredPosition = new Vector2(0f, -42f);
            imageRect.sizeDelta = new Vector2(44f, 44f);
            var questImage = imageGo.AddComponent<RawImage>();
            questImage.texture = null;
            questImage.color = new Color(1f, 1f, 1f, 0.16f);
            questImage.raycastTarget = false;
            BESUIEditorUtils.SetPrivateField(tracker, "questImage", questImage);
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
            UIAnchorPresets.Center(panelRect, new Vector2(1800f, 990f));
            panel.AddComponent<Image>().color = new Color(0.03f, 0.14f, 0.25f, 0.88f);

            var fixedA = AddStretchRawArtworkPanel(panel.transform, "FixedArtworkA", new Color(1f, 1f, 1f, 0.06f));
            fixedA.transform.SetAsFirstSibling();
            var fixedB = AddRawArtworkPanel(panel.transform, "FixedArtworkB", new Vector2(220f, 120f), new Vector2(-710f, 404f), new Color(1f, 1f, 1f, 0.1f));

            var title = BESUIEditorUtils.CreateText(panel.transform, "PanelTitle", "In Progress", new Vector2(-775f, 425f), 26f, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            var closeBtn = BESUIEditorUtils.CreateButton(panel.transform, "BackBtn", "Back", new Vector2(810f, 425f), new Vector2(96f, 44f));
            var navigateBtn = BESUIEditorUtils.CreateButton(panel.transform, "NavigateBtn", "Navigate", new Vector2(700f, -410f), new Vector2(220f, 58f));

            var divider = new GameObject("Divider");
            divider.transform.SetParent(panel.transform, false);
            var dividerRect = divider.AddComponent<RectTransform>();
            UIAnchorPresets.Center(dividerRect, new Vector2(3f, 840f));
            dividerRect.anchoredPosition = new Vector2(-250f, -20f);
            divider.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.45f);

            var scrollGo = new GameObject("QuestListScroll");
            scrollGo.transform.SetParent(panel.transform, false);
            var scrollRectTransform = scrollGo.AddComponent<RectTransform>();
            UIAnchorPresets.Center(scrollRectTransform, new Vector2(560f, 780f));
            scrollRectTransform.anchoredPosition = new Vector2(-610f, -40f);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(viewportRect);
            viewportGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.02f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 900f);
            var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 16, 8, 16);
            contentLayout.spacing = 12f;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            var storyContainer = CreateQuestSection(contentGo.transform, "Story Quest");
            var commissionContainer = CreateQuestSection(contentGo.transform, "Commission Quests");
            var worldContainer = CreateQuestSection(contentGo.transform, "World Quest");

            var detailRoot = new GameObject("QuestDetail");
            detailRoot.transform.SetParent(panel.transform, false);
            var detailRect = detailRoot.AddComponent<RectTransform>();
            UIAnchorPresets.Center(detailRect, new Vector2(980f, 760f));
            detailRect.anchoredPosition = new Vector2(360f, -40f);

            var questTitle = BESUIEditorUtils.CreateText(detailRoot.transform, "QuestTitle", "NAME OF QUEST", new Vector2(-450f, 330f), 24f, TextAlignmentOptions.Left);
            questTitle.fontStyle = FontStyles.Bold;
            var locationImage = AddRawArtworkPanel(detailRoot.transform, "QuestLocationImage", new Vector2(34f, 34f), new Vector2(-465f, 286f), new Color(1f, 1f, 1f, 0.18f));
            var locationText = BESUIEditorUtils.CreateText(detailRoot.transform, "QuestLocationText", "Quest location", new Vector2(-260f, 286f), 16f, TextAlignmentOptions.Left);
            locationText.rectTransform.sizeDelta = new Vector2(460f, 36f);
            locationText.color = new Color(1f, 0.86f, 0.28f, 0.95f);
            var detailText = BESUIEditorUtils.CreateText(detailRoot.transform, "QuestDetailText", "Quest detail", new Vector2(-450f, 190f), 16f, TextAlignmentOptions.TopLeft);
            detailText.rectTransform.sizeDelta = new Vector2(900f, 250f);

            var rewardLabel = BESUIEditorUtils.CreateText(detailRoot.transform, "RewardLabel", "Quest Reward", new Vector2(-450f, -180f), 20f, TextAlignmentOptions.Left);
            rewardLabel.fontStyle = FontStyles.Bold;
            var rewardGo = new GameObject("RewardContainer");
            rewardGo.transform.SetParent(detailRoot.transform, false);
            var rewardRect = rewardGo.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rewardRect, new Vector2(650f, 120f));
            rewardRect.anchoredPosition = new Vector2(-120f, -270f);
            var rewardLayout = rewardGo.AddComponent<HorizontalLayoutGroup>();
            rewardLayout.spacing = 18f;
            rewardLayout.childForceExpandWidth = false;
            rewardLayout.childForceExpandHeight = false;

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIQuestCard.prefab")?.GetComponent<QuestCardUI>();
            var rewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIQuestRewardItem.prefab")?.GetComponent<QuestRewardItemUI>();

            BESUIEditorUtils.SetPrivateField(log, "panel", panel);
            BESUIEditorUtils.SetPrivateField(log, "storyQuestContainer", storyContainer);
            BESUIEditorUtils.SetPrivateField(log, "commissionQuestContainer", commissionContainer);
            BESUIEditorUtils.SetPrivateField(log, "worldQuestContainer", worldContainer);
            BESUIEditorUtils.SetPrivateField(log, "questCardPrefab", cardPrefab);
            BESUIEditorUtils.SetPrivateField(log, "closeButton", closeBtn);
            BESUIEditorUtils.SetPrivateField(log, "navigateButton", navigateBtn);
            BESUIEditorUtils.SetPrivateField(log, "fixedArtworkA", fixedA);
            BESUIEditorUtils.SetPrivateField(log, "fixedArtworkB", fixedB);
            BESUIEditorUtils.SetPrivateField(log, "locationImage", locationImage);
            BESUIEditorUtils.SetPrivateField(log, "questTitleText", questTitle);
            BESUIEditorUtils.SetPrivateField(log, "questLocationText", locationText);
            BESUIEditorUtils.SetPrivateField(log, "questDetailText", detailText);
            BESUIEditorUtils.SetPrivateField(log, "rewardContainer", rewardGo.transform);
            BESUIEditorUtils.SetPrivateField(log, "rewardItemPrefab", rewardPrefab);
            panel.SetActive(false);
        }

        static Transform CreateQuestSection(Transform parent, string label)
        {
            var sectionRoot = new GameObject(label.Replace(" ", string.Empty) + "Section");
            sectionRoot.transform.SetParent(parent, false);
            var rootRect = sectionRoot.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0f, 180f);
            var rootLayout = sectionRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 8f;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;
            sectionRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sectionLabel = BESUIEditorUtils.CreateText(sectionRoot.transform, "Header", label, Vector2.zero, 21f, TextAlignmentOptions.Left);
            sectionLabel.fontStyle = FontStyles.Bold;
            sectionLabel.rectTransform.sizeDelta = new Vector2(0f, 34f);

            var containerGo = new GameObject("Cards");
            containerGo.transform.SetParent(sectionRoot.transform, false);
            var containerRect = containerGo.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0f, 96f);
            var layout = containerGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            containerGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return containerGo.transform;
        }

#if false
        static void BuildQuestLogLegacy(Transform parent)
        {
            var go = new GameObject("QuestLog");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            var log = go.AddComponent<QuestLogUI>();
            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            UIAnchorPresets.TopLeft(panelRect, new Vector2(420, 420), new Vector2(20, -300));
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

#endif
        static void BuildHudCornerButtons(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("HudCornerButtons");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(rect);
            var controls = go.AddComponent<HudCornerButtonsUI>();

            var settings = BESUIEditorUtils.CreateIconButton(go.transform, "SettingsBtn", manifest?.settingsIcon, new Vector2(20f, -20f), new Vector2(44f, 44f));
            UIAnchorPresets.TopLeft(settings.GetComponent<RectTransform>(), new Vector2(44f, 44f), new Vector2(20f, -20f));

            var guide = BESUIEditorUtils.CreateIconButton(go.transform, "GuideLineBtn", manifest?.guideLineIcon, new Vector2(244f, -78f), new Vector2(36f, 36f));
            UIAnchorPresets.TopLeft(guide.GetComponent<RectTransform>(), new Vector2(36f, 36f), new Vector2(244f, -78f));

            var mission = BESUIEditorUtils.CreateIconButton(go.transform, "MissionBtn", manifest?.missionIcon, new Vector2(20f, -252f), new Vector2(44f, 44f));
            UIAnchorPresets.TopLeft(mission.GetComponent<RectTransform>(), new Vector2(44f, 44f), new Vector2(20f, -252f));

            BESUIEditorUtils.SetPrivateField(controls, "settingsButton", settings);
            BESUIEditorUtils.SetPrivateField(controls, "guideLineButton", guide);
            BESUIEditorUtils.SetPrivateField(controls, "missionButton", mission);
        }

        static void BuildChatBox(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("ChatBox");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.BottomLeft(rect, new Vector2(520, 260), new Vector2(24f, 24f));
            var chat = go.AddComponent<ChatBoxUI>();

            var button = BESUIEditorUtils.CreateIconButton(go.transform, "ChatButton", manifest?.chatBubbleIcon, Vector2.zero, new Vector2(44f, 44f));
            UIAnchorPresets.BottomLeft(button.GetComponent<RectTransform>(), new Vector2(44f, 44f), Vector2.zero);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var panelRect = panel.AddComponent<RectTransform>();
            UIAnchorPresets.BottomLeft(panelRect, new Vector2(480f, 220f), new Vector2(52f, 0f));
            panel.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.1f, 0.88f);

            var history = BESUIEditorUtils.CreateText(panel.transform, "History", string.Empty, Vector2.zero, 13f, TextAlignmentOptions.BottomLeft);
            UIAnchorPresets.StretchFull(history.rectTransform);
            history.rectTransform.offsetMin = new Vector2(12f, 48f);
            history.rectTransform.offsetMax = new Vector2(-12f, -12f);

            var input = CreateTMPInput(panel.transform, "Input", "Chat...", new Vector2(-48f, 20f), new Vector2(360f, 36f));
            var send = BESUIEditorUtils.CreateButton(panel.transform, "SendBtn", "Send", new Vector2(188f, 20f), new Vector2(76f, 36f));

            BESUIEditorUtils.SetPrivateField(chat, "chatButton", button);
            BESUIEditorUtils.SetPrivateField(chat, "panel", panel);
            BESUIEditorUtils.SetPrivateField(chat, "historyText", history);
            BESUIEditorUtils.SetPrivateField(chat, "inputField", input);
            BESUIEditorUtils.SetPrivateField(chat, "sendButton", send);
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

            var evt = BESUIEditorUtils.CreateIconButton(go.transform, "EventBtn", manifest?.navEvent, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing * 4f, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var battlePass = BESUIEditorUtils.CreateIconButton(go.transform, "BattlePassBtn", manifest?.navBattlePass ?? manifest?.navTeam, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing * 3f, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var wish = BESUIEditorUtils.CreateIconButton(go.transform, "WishBtn", manifest?.navWish, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing * 2f, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var bag = BESUIEditorUtils.CreateIconButton(go.transform, "BagBtn", manifest?.navBag ?? manifest?.navInventory, new Vector2(HUDLayoutTokens.NavRightMostX - HUDLayoutTokens.NavIconSpacing, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));
            var personal = BESUIEditorUtils.CreateIconButton(go.transform, "PersonalBtn", manifest?.navPersonal ?? manifest?.navCharacter, new Vector2(HUDLayoutTokens.NavRightMostX, 0f), new Vector2(HUDLayoutTokens.NavIconSize, HUDLayoutTokens.NavIconSize));

            BESUIEditorUtils.SetPrivateField(nav, "wishButton", wish);
            BESUIEditorUtils.SetPrivateField(nav, "eventButton", evt);
            BESUIEditorUtils.SetPrivateField(nav, "battlePassButton", battlePass);
            BESUIEditorUtils.SetPrivateField(nav, "bagButton", bag);
            BESUIEditorUtils.SetPrivateField(nav, "personalButton", personal);
        }

        static void BuildPartyStrip(Transform parent, HUDSpriteManifest manifest)
        {
            var go = new GameObject("PartyStrip");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.ApplyPartyStripRegion(rect);
            var stripUi = go.AddComponent<PartyStripUI>();
            var slotRoots = new RectTransform[4];
            var frames = new Image[4];
            var portraits = new Image[4];
            var buttons = new Button[4];
            var names = new TMP_Text[4];
            var numbers = new TMP_Text[4];
            var healthBars = new Slider[4];
            var pillFrame = manifest?.partySlotFrame ?? LoadEditorFrame("Rectangle 39782.png");

            for (var i = 0; i < 4; i++)
            {
                var slotGo = new GameObject($"PartySlot{i + 1}");
                slotGo.transform.SetParent(go.transform, false);
                var slotRect = slotGo.AddComponent<RectTransform>();
                slotRect.sizeDelta = HUDLayoutTokens.PartySlotSize;
                slotRect.anchoredPosition = new Vector2(0, 118 - i * HUDLayoutTokens.PartySlotSpacing);
                slotRoots[i] = slotRect;
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
                names[i].rectTransform.sizeDelta = new Vector2(104f, 22f);

                healthBars[i] = BESUIEditorUtils.CreateFilledSlider(slotGo.transform, "HealthBar", new Vector2(36f, -20f), new Vector2(102f, 8f),
                    null, null, HUDPrimitiveStyles.HpBarFill);
                ApplyPrimitiveBar(healthBars[i], new Color(0.04f, 0.05f, 0.06f, 0.85f), HUDPrimitiveStyles.HpBarFill);

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

            BESUIEditorUtils.SetPrivateField(stripUi, "slotRoots", slotRoots);
            BESUIEditorUtils.SetPrivateField(stripUi, "slotFrames", frames);
            BESUIEditorUtils.SetPrivateField(stripUi, "portraits", portraits);
            BESUIEditorUtils.SetPrivateField(stripUi, "slotButtons", buttons);
            BESUIEditorUtils.SetPrivateField(stripUi, "slotNames", names);
            BESUIEditorUtils.SetPrivateField(stripUi, "slotNumbers", numbers);
            BESUIEditorUtils.SetPrivateField(stripUi, "healthBars", healthBars);
            BESUIEditorUtils.SetPrivateField(stripUi, "activeScale", 1.2f);
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
                cooldowns[i].fillMethod = Image.FillMethod.Radial360;
                cooldowns[i].fillOrigin = (int)Image.Origin360.Top;
                cooldowns[i].fillClockwise = true;
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
            AddRawArtworkPanel(go.transform, "InventoryArtworkRaw", new Vector2(520, 680), new Vector2(450, -10));

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

        static GameMapUI BuildWorldMapPanel(Transform parent, HUDSpriteManifest manifest)
        {
            var go = CreateFullScreenPanel(parent, "GameMapUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgEventScene), UIScreenBackgroundId.WorldMap);
            var map = go.AddComponent<GameMapUI>();
            BESUIEditorUtils.CreateText(go.transform, "Title", "World Map", new Vector2(0, 420), 24f);
            var mapArtwork = AddRawArtworkPanel(go.transform, "WorldMapArtworkRaw", new Vector2(1160, 720), new Vector2(0, -20), new Color(1f, 1f, 1f, 0.04f));
            if (manifest?.minimapMap != null)
            {
                mapArtwork.texture = manifest.minimapMap.texture;
                mapArtwork.color = Color.white;
            }
            var mapRect = mapArtwork.GetComponent<RectTransform>();
            var markersGo = new GameObject("MapMarkers");
            markersGo.transform.SetParent(go.transform, false);
            var mRect = markersGo.AddComponent<RectTransform>();
            UIAnchorPresets.Center(mRect, new Vector2(1160, 720));
            mRect.anchoredPosition = new Vector2(0, -20);
            var closeBtn = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            var markerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIMapMarker.prefab");
            var markerSlots = CreateWorldMapMarkerSlots(markersGo.transform, markerPrefab);
            var playerDot = CreateMapDot(markersGo.transform, "PlayerPosition", null, new Color(0.35f, 1f, 0.45f, 0.95f));
            playerDot.sizeDelta = new Vector2(18f, 18f);
            var status = BESUIEditorUtils.CreateText(go.transform, "TeleportStatus", string.Empty, new Vector2(0, -420), 16f);
            BESUIEditorUtils.SetPrivateField(map, "panel", go);
            BESUIEditorUtils.SetPrivateField(map, "markersContainer", markersGo.transform);
            BESUIEditorUtils.SetPrivateField(map, "mapRect", mapRect);
            BESUIEditorUtils.SetPrivateField(map, "mapImage", mapArtwork);
            BESUIEditorUtils.SetPrivateField(map, "playerIcon", playerDot);
            BESUIEditorUtils.SetPrivateField(map, "mapMarkerPrefab", markerPrefab);
            BESUIEditorUtils.SetPrivateField(map, "markerSlots", markerSlots);
            BESUIEditorUtils.SetPrivateField(map, "closeButton", closeBtn);
            BESUIEditorUtils.SetPrivateField(map, "statusText", status);
            go.SetActive(false);
            return map;
        }

        static UIMapMarker[] CreateWorldMapMarkerSlots(Transform parent, GameObject markerPrefab)
        {
            var slots = new UIMapMarker[3];
            var positions = new[]
            {
                new Vector2(-180f, -40f),
                new Vector2(150f, 20f),
                new Vector2(-40f, 130f)
            };

            for (var i = 0; i < slots.Length; i++)
            {
                GameObject slot;
                if (markerPrefab != null)
                    slot = (GameObject)PrefabUtility.InstantiatePrefab(markerPrefab, parent);
                else
                {
                    slot = new GameObject($"TeleportMarker_{i + 1}");
                    slot.transform.SetParent(parent, false);
                    slot.AddComponent<RectTransform>().sizeDelta = new Vector2(180, 44);
                    var button = slot.AddComponent<Button>();
                    var label = BESUIEditorUtils.CreateText(slot.transform, "Label", $"Teleport {i + 1}", Vector2.zero, 14f);
                    var marker = slot.AddComponent<UIMapMarker>();
                    BESUIEditorUtils.SetPrivateField(marker, "regionLabel", label);
                    BESUIEditorUtils.SetPrivateField(marker, "button", button);
                }

                slot.name = $"TeleportMarker_{i + 1}";
                var rect = slot.GetComponent<RectTransform>();
                if (rect != null)
                    rect.anchoredPosition = positions[i];
                slots[i] = slot.GetComponent<UIMapMarker>();
            }

            return slots;
        }

        static WeaponScreenUI BuildWeaponPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "WeaponScreenUI",
                BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeaponInfo) ?? BESUIEditorUtils.LoadBg(UIAssetPaths.BgWeapon),
                UIScreenBackgroundId.Weapon);
            var weapon = go.AddComponent<WeaponScreenUI>();
            AddRawArtworkPanel(go.transform, "WeaponArtworkRaw", new Vector2(500, 620), new Vector2(300, -20));
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
            AddRawArtworkPanel(go.transform, "TeamArtworkRaw", new Vector2(900, 480), new Vector2(0, 40));
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
            AddRawArtworkPanel(go.transform, "EventArtworkRaw", new Vector2(900, 500), new Vector2(0, 20));
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

        static BattlePassUI BuildBattlePassPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "BattlePassUI", null);
            var ui = go.AddComponent<BattlePassUI>();
            AddRawArtworkPanel(go.transform, "BattlePassArtworkRaw", new Vector2(980, 620), new Vector2(0, 0));
            var title = BESUIEditorUtils.CreateText(go.transform, "Title", "Battle Pass", new Vector2(0, 320), 26f);
            var progress = BESUIEditorUtils.CreateText(go.transform, "Progress", "Progress 0 / 100", new Vector2(0, 260), 16f);
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "titleText", title);
            BESUIEditorUtils.SetPrivateField(ui, "progressText", progress);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static WishUI BuildWishPanel(Transform parent)
        {
            var go = CreateFullScreenPanel(parent, "WishUI", BESUIEditorUtils.LoadBg(UIAssetPaths.BgWish), UIScreenBackgroundId.Wish);
            var ui = go.AddComponent<WishUI>();
            AddStretchRawArtworkPanel(go.transform, "WishArtworkRaw");
            var banner = BESUIEditorUtils.CreateText(go.transform, "Banner", "Character Wish", new Vector2(0, 360), 24f);
            var coins = BESUIEditorUtils.CreateText(go.transform, "Coins", "Money 99999", new Vector2(560, 460), 16f, TextAlignmentOptions.Right);
            var gems = BESUIEditorUtils.CreateText(go.transform, "Gems", "GEM 1600", new Vector2(740, 460), 16f, TextAlignmentOptions.Right);
            var resultGo = new GameObject("ResultCards");
            resultGo.transform.SetParent(go.transform, false);
            var rRect = resultGo.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rRect, new Vector2(1000, 520));
            rRect.anchoredPosition = new Vector2(0, 20);
            var resultCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIAssetPaths.AtomPrefabs + "/UIGachaCard.prefab");
            var result = BESUIEditorUtils.CreateText(go.transform, "Result", "Select Wish x1 or x10", new Vector2(0, -360), 16f);

            var controls = new GameObject("Controls");
            controls.transform.SetParent(go.transform, false);
            var controlsRect = controls.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(controlsRect);
            var money = BESUIEditorUtils.CreateButton(controls.transform, "MoneyBtn", "Money", new Vector2(560, 460), new Vector2(140, 34));
            var gem = BESUIEditorUtils.CreateButton(controls.transform, "GemBtn", "GEM", new Vector2(740, 460), new Vector2(140, 34));
            var close = BESUIEditorUtils.CreateButton(controls.transform, "BackBtn", "Back", new Vector2(880, 460), new Vector2(68, 44));
            var one = BESUIEditorUtils.CreateButton(controls.transform, "WishOne", "Wish x 1", new Vector2(-140, -420), new Vector2(170, 42));
            var ten = BESUIEditorUtils.CreateButton(controls.transform, "WishTen", "Wish x 10", new Vector2(140, -420), new Vector2(170, 42));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "banner", AssetDatabase.LoadAssetAtPath<GachaBannerDefinition>("Assets/_Project/Data/UI/DefaultGachaBanner.asset"));
            BESUIEditorUtils.SetPrivateField(ui, "bannerText", banner);
            BESUIEditorUtils.SetPrivateField(ui, "coinsText", coins);
            BESUIEditorUtils.SetPrivateField(ui, "gemsText", gems);
            BESUIEditorUtils.SetPrivateField(ui, "resultText", result);
            BESUIEditorUtils.SetPrivateField(ui, "moneyButton", money);
            BESUIEditorUtils.SetPrivateField(ui, "gemButton", gem);
            BESUIEditorUtils.SetPrivateField(ui, "resultCardsContainer", resultGo.transform);
            BESUIEditorUtils.SetPrivateField(ui, "resultCardPrefab", resultCardPrefab);
            BESUIEditorUtils.SetPrivateField(ui, "wishOneButton", one);
            BESUIEditorUtils.SetPrivateField(ui, "wishTenButton", ten);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            BESUIEditorUtils.SetPrivateField(ui, "controlsRoot", controls);
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
            var input = CreateTMPInput(go.transform, "AccountNameInput", "Account name", new Vector2(0, -92), new Vector2(320, 44));
            var create = BESUIEditorUtils.CreateButton(go.transform, "CreateAccountButton", "Create account", new Vector2(0, -152), new Vector2(220, 44));
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(360, 200), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            BESUIEditorUtils.SetPrivateField(ui, "usernameText", username);
            BESUIEditorUtils.SetPrivateField(ui, "serverText", server);
            BESUIEditorUtils.SetPrivateField(ui, "uidText", uid);
            BESUIEditorUtils.SetPrivateField(ui, "usernameInput", input);
            BESUIEditorUtils.SetPrivateField(ui, "createAccountButton", create);
            BESUIEditorUtils.SetPrivateField(ui, "closeButton", close);
            go.SetActive(false);
            return ui;
        }

        static TMP_InputField CreateTMPInput(Transform parent, string name, string placeholderText, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            UIAnchorPresets.Center(rect, size);
            rect.anchoredPosition = anchoredPos;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.05f, 0.07f, 0.11f, 0.72f);

            var text = BESUIEditorUtils.CreateText(go.transform, "Text", string.Empty, Vector2.zero, 18f, TextAlignmentOptions.MidlineLeft);
            UIAnchorPresets.StretchFull(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(14f, 0f);
            text.rectTransform.offsetMax = new Vector2(-14f, 0f);

            var placeholder = BESUIEditorUtils.CreateText(go.transform, "Placeholder", placeholderText, Vector2.zero, 18f, TextAlignmentOptions.MidlineLeft);
            UIAnchorPresets.StretchFull(placeholder.rectTransform);
            placeholder.rectTransform.offsetMin = new Vector2(14f, 0f);
            placeholder.rectTransform.offsetMax = new Vector2(-14f, 0f);
            placeholder.color = new Color(1f, 1f, 1f, 0.42f);

            var input = go.AddComponent<TMP_InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
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
            var musicVolRow = InstantiateAtom(panelGo.transform, "MusicVolumeRow", UIAssetPaths.AtomPrefabs + "/UISettingsRow.prefab", new Vector2(0, 80), new Vector2(400, 40));
            var sfxVolRow = InstantiateAtom(panelGo.transform, "SfxVolumeRow", UIAssetPaths.AtomPrefabs + "/UISettingsRow.prefab", new Vector2(0, 20), new Vector2(400, 40));
            var fsRow = InstantiateAtom(panelGo.transform, "FullscreenRow", UIAssetPaths.AtomPrefabs + "/UISettingsRow.prefab", new Vector2(0, -40), new Vector2(400, 40));
            var close = BESUIEditorUtils.CreateButton(go.transform, "CloseBtn", "X", new Vector2(880, 460), new Vector2(48, 48));
            BESUIEditorUtils.SetPrivateField(ui, "root", go);
            if (musicVolRow != null) BESUIEditorUtils.SetPrivateField(ui, "musicVolumeRow", musicVolRow.GetComponent<UISettingsRow>());
            if (sfxVolRow != null) BESUIEditorUtils.SetPrivateField(ui, "sfxVolumeRow", sfxVolRow.GetComponent<UISettingsRow>());
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
            {
                var bgGo = new GameObject("Background");
                bgGo.transform.SetParent(go.transform, false);
                var bgRect = bgGo.AddComponent<RectTransform>();
                UIAnchorPresets.StretchFull(bgRect);
                var bg = bgGo.AddComponent<RawImage>();
                bg.texture = bgSprite.texture;
                bg.color = Color.white;
                bg.raycastTarget = true;
            }
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
