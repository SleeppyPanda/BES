using System.Collections.Generic;
using BES.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BES.UI.Menu
{
    public enum GallerySortMode { CombatPower, Constellation, Quality, Affinity }

    [System.Serializable]
    public class GalleryRarityBackground
    {
        [Range(1, 6)] public int rarity = 4;
        public Sprite background;
    }

    public class CharacterCollectionPanel : MonoBehaviour
    {
        [Header("Data and navigation")]
        [SerializeField] MenuContentDatabase database;
        [SerializeField] MenuHomeController homeController;
        [SerializeField] SimpleModalPanel modal;
        [SerializeField] SimpleModalPanel wishModal;

        [Header("Visual references")]
        [SerializeField] Sprite galleryReference;
        [SerializeField] Sprite detailReference;
        [SerializeField] Sprite levelReference;
        [SerializeField] Sprite affinityReference;
        [SerializeField] Sprite constellationReference;
        [SerializeField] Sprite artifactReference;
        [SerializeField] Sprite weaponReference;
        [Header("Character tab buttons")]
        [SerializeField] Sprite informationTabSprite;
        [SerializeField] Sprite levelTabSprite;
        [SerializeField] Sprite artifactTabSprite;
        [SerializeField] Sprite weaponTabSprite;
        [SerializeField] Sprite constellationTabSprite;
        [SerializeField] Sprite affinityTabSprite;
        [SerializeField] List<Sprite> characterUiSprites = new();
        [Header("Gallery sprites")]
        [SerializeField] Sprite galleryPanelSprite;
        [SerializeField] Sprite combatPowerButtonSprite;
        [SerializeField] Sprite constellationButtonSprite;
        [SerializeField] Sprite qualityButtonSprite;
        [SerializeField] Sprite affinityButtonSprite;
        [SerializeField] Sprite galleryStarSprite;
        [SerializeField] Sprite emptyStarSprite;
        [SerializeField] List<GalleryRarityBackground> rarityBackgrounds = new();
        [SerializeField] GallerySortMode gallerySortMode;

        readonly List<GameObject> generatedCards = new();
        readonly List<GameObject> generatedSelectors = new();
        GameObject runtimeRoot;
        GameObject galleryPage;
        GameObject characterPage;
        GameObject detailPage;
        GameObject levelPage;
        GameObject affinityPage;
        GameObject constellationPage;
        GameObject artifactPage;
        GameObject artifactListPage;
        GameObject artifactDetailPage;
        GameObject weaponPage;
        GameObject breakthroughPage;
        GameObject detailNavigation;
        RectTransform characterSelectorContent;
        RectTransform galleryContent;
        GameObject galleryCardTemplate;
        Image detailPortrait;
        Image levelPortrait;
        TMP_Text detailName;
        TMP_Text detailStats;
        TMP_Text detailDescription;
        TMP_Text levelName;
        TMP_Text levelValue;
        TMP_Text emptyLabel;
        TMP_Text selectedNameLabel;
        Image selectedElementIcon;
        RectTransform tabIndicator;
        readonly Image[] informationEquipmentSlots = new Image[4];
        readonly Sprite[] informationEmptySlotSprites = new Sprite[4];
        string selectedCharacterId;

        [Header("Weapon UI sprites")]
        [SerializeField] Sprite weaponChangeSprite;
        [SerializeField] Sprite weaponEnhanceSprite;
        [SerializeField] Sprite weaponRefineSprite;
        [SerializeField] Sprite weaponDetailFrameSprite;
        [SerializeField] Sprite weaponSlotFrameSprite;

        GameObject weaponListPage;
        GameObject weaponDetailPage;
        Image weaponPageFrame;
        Image weaponPageSlot;
        Image weaponPageIcon;
        TMP_Text weaponPageName;
        TMP_Text weaponPageStats;
        TMP_Text weaponPageDesc;

        void Awake()
        {
            modal ??= GetComponent<SimpleModalPanel>();
            if (!CacheExistingUI()) BuildRuntimeUI();
            WireRuntimeButtons();
        }

#if UNITY_EDITOR
        public void RebuildEditorHierarchy()
        {
            var existing = transform.Find("CharacterCollectionRuntime");
            if (existing != null) DestroyImmediate(existing.gameObject);
            ResetCachedHierarchy();
            BuildRuntimeUI();
            ShowPage(galleryPage);
        }
#endif

        void OnEnable()
        {
            GameEvents.OnPartyChanged += RefreshGallery;
            if (runtimeRoot != null) RefreshGallery();
        }

        void OnDisable() => GameEvents.OnPartyChanged -= RefreshGallery;

        public void OpenGallery()
        {
            BuildRuntimeUI();
            ShowPage(galleryPage);
            RefreshGallery();
            modal?.Open();
        }

        public void OpenCharacter(string characterId)
        {
            BuildRuntimeUI();
            selectedCharacterId = ResolveOwnedCharacter(characterId);
            RefreshCharacter();
            ShowPage(detailPage);
            modal?.Open();
        }

        public void OpenLevel()
        {
            if (string.IsNullOrEmpty(selectedCharacterId)) return;
            RefreshCharacter();
            ShowPage(levelPage);
        }

        public void OpenLevel(string characterId)
        {
            OpenCharacter(characterId);
            ShowPage(levelPage);
        }

        public void OpenRateUp()
        {
            modal?.Close();
            wishModal?.Open();
        }

        public void AddSelectedCharacterExperience(int amount)
        {
            if (string.IsNullOrEmpty(selectedCharacterId) || amount <= 0) return;

            var currentLevel = CharacterProgressionState.GetLevel(selectedCharacterId);
            var levelCap = CharacterProgressionState.GetLevelCap(selectedCharacterId);
            if (currentLevel >= levelCap)
            {
                if (levelValue != null)
                    levelValue.text = $"Cấp hiện tại: {currentLevel}/{levelCap}\nCần đột phá để tăng giới hạn";
                return;
            }

            string itemId = amount switch
            {
                500 => "item_exp_green",
                2000 => "item_exp_blue",
                5000 => "item_exp_gold",
                _ => ""
            };

            int goldCost = amount switch
            {
                500 => 350,
                2000 => 1400,
                5000 => 3500,
                _ => 0
            };

            if (string.IsNullOrEmpty(itemId)) return;

            var inventory = GameManager.Instance?.Inventory;
            if (inventory == null || inventory.GetCount(itemId) < 1)
            {
                if (levelValue != null)
                    levelValue.text = $"Cấp hiện tại: {currentLevel}/{levelCap}\nKhông đủ lọ EXP tương ứng!";
                return;
            }

            if (PlayerWallet.Instance == null || PlayerWallet.Instance.Coins < goldCost)
            {
                if (levelValue != null)
                    levelValue.text = $"Cấp hiện tại: {currentLevel}/{levelCap}\nKhông đủ Vàng để nâng cấp!";
                return;
            }

            if (inventory.RemoveItem(itemId, 1))
            {
                PlayerWallet.Instance.TrySpendCoins(goldCost);
                CharacterProgressionState.AddExperience(selectedCharacterId, amount);
                RefreshCharacter();
                RefreshGallery();
                GameManager.Instance?.SaveGame();
            }
        }

        public bool BreakthroughSelectedCharacter()
        {
            if (string.IsNullOrEmpty(selectedCharacterId)) return false;
            var succeeded = CharacterProgressionState.TryBreakthrough(selectedCharacterId);
            if (succeeded) RefreshCharacter();
            return succeeded;
        }

        public bool UnlockSelectedConstellation()
        {
            if (string.IsNullOrEmpty(selectedCharacterId)) return false;
            var succeeded = CharacterProgressionState.TryUnlockNextConstellation(selectedCharacterId);
            if (succeeded) RefreshCharacter();
            return succeeded;
        }

        void BuildRuntimeUI()
        {
            if (runtimeRoot != null) return;
            var oldContent = transform.Find("ModalCard");
            if (oldContent != null) oldContent.gameObject.SetActive(false);

            runtimeRoot = Rect("CharacterCollectionRuntime", transform, Vector2.zero, Vector2.one).gameObject;
            galleryPage = BuildPage("GalleryPage", galleryReference);
            characterPage = Rect("CharacterPage", runtimeRoot.transform, Vector2.zero, Vector2.one).gameObject;
            var contents = Rect("TabContents", characterPage.transform, Vector2.zero, Vector2.one);
            detailPage = BuildPage("InformationContent", detailReference, contents);
            levelPage = BuildPage("LevelContent", levelReference, contents);
            affinityPage = BuildPage("AffinityContent", affinityReference, contents);
            constellationPage = BuildPage("ConstellationContent", constellationReference, contents);
            artifactPage = BuildPage("ArtifactContent", artifactReference, contents);
            weaponPage = BuildPage("WeaponContent", weaponReference, contents);
            BuildSharedNavigation();
            BuildGalleryOverlay();
            BuildDetailOverlay();
            BuildLevelOverlay();
            BuildWeaponOverlay();
            RefreshCharacterSelectors();
        }

        bool CacheExistingUI()
        {
            var existing = transform.Find("CharacterCollectionRuntime");
            if (existing == null) return false;
            runtimeRoot = existing.gameObject;
            galleryPage = FindDeep(existing, "GalleryPage")?.gameObject;
            characterPage = FindDeep(existing, "CharacterPage")?.gameObject;
            detailPage = FindDeep(existing, "InformationContent")?.gameObject;
            levelPage = FindDeep(existing, "LevelContent")?.gameObject;
            affinityPage = FindDeep(existing, "AffinityContent")?.gameObject;
            constellationPage = FindDeep(existing, "ConstellationContent")?.gameObject;
            artifactPage = FindDeep(existing, "ArtifactContent")?.gameObject;
            artifactListPage = FindDeep(existing, "ArtifactListContent")?.gameObject;
            artifactDetailPage = FindDeep(existing, "ArtifactDetailContent")?.gameObject;
            weaponPage = FindDeep(existing, "WeaponContent")?.gameObject;
            weaponListPage = FindDeep(existing, "WeaponListContent")?.gameObject;
            weaponDetailPage = FindDeep(existing, "WeaponDetailContent")?.gameObject;
            weaponPageFrame = FindDeep(existing, "WeaponDetailFrame")?.GetComponent<Image>();
            weaponPageSlot = FindDeep(existing, "WeaponSlotFrame")?.GetComponent<Image>();
            weaponPageIcon = FindDeep(existing, "WeaponIcon")?.GetComponent<Image>();
            weaponPageName = FindDeep(existing, "WeaponNameText")?.GetComponent<TMP_Text>();
            weaponPageStats = FindDeep(existing, "WeaponStatsText")?.GetComponent<TMP_Text>();
            weaponPageDesc = FindDeep(existing, "WeaponDescText")?.GetComponent<TMP_Text>();
            breakthroughPage = FindDeep(existing, "BreakthroughContent")?.gameObject;
            detailNavigation = FindDeep(existing, "TabNavigation")?.gameObject;
            galleryContent = FindDeep(existing, "OwnedCharacterContent") as RectTransform;
            galleryCardTemplate = FindDeep(existing, "CharacterCardTemplate")?.gameObject;
            characterSelectorContent = FindDeep(existing, "OwnedCharacterSelector") as RectTransform;
            detailPortrait = FindDeep(existing, "SelectedPortrait")?.GetComponent<Image>();
            levelPortrait = FindDeep(existing, "LevelPortrait")?.GetComponent<Image>();
            detailName = FindDeep(existing, "CharacterName")?.GetComponent<TMP_Text>();
            detailStats = FindDeep(existing, "CharacterStats")?.GetComponent<TMP_Text>();
            detailDescription = FindDeep(existing, "CharacterDescription")?.GetComponent<TMP_Text>();
            levelName = FindDeep(existing, "LevelCharacterName")?.GetComponent<TMP_Text>();
            levelValue = FindDeep(existing, "LevelValue")?.GetComponent<TMP_Text>();
            emptyLabel = FindDeep(existing, "EmptyGallery")?.GetComponent<TMP_Text>();
            selectedNameLabel = FindDeep(existing, "SelectedCharacterName")?.GetComponent<TMP_Text>();
            selectedElementIcon = FindDeep(existing, "SelectedElementIcon")?.GetComponent<Image>();
            tabIndicator = FindDeep(existing, "TabIndicator") as RectTransform;
            for (var i = 0; i < informationEquipmentSlots.Length; i++)
            {
                informationEquipmentSlots[i] = FindDeep(existing, $"InformationEquipmentSlot_{i}")?.GetComponent<Image>();
                informationEmptySlotSprites[i] = informationEquipmentSlots[i]?.sprite;
            }
            return galleryPage != null && detailPage != null && levelPage != null;
        }

        void ResetCachedHierarchy()
        {
            runtimeRoot = galleryPage = characterPage = detailPage = levelPage = affinityPage = constellationPage = null;
            artifactPage = artifactListPage = artifactDetailPage = weaponPage = breakthroughPage = null;
            weaponListPage = weaponDetailPage = null;
            weaponPageFrame = weaponPageSlot = weaponPageIcon = null;
            weaponPageName = weaponPageStats = weaponPageDesc = null;
            detailNavigation = null;
            galleryCardTemplate = null;
            galleryContent = characterSelectorContent = null;
            detailPortrait = levelPortrait = null;
            detailName = detailStats = detailDescription = levelName = levelValue = emptyLabel = selectedNameLabel = null;
            selectedElementIcon = null;
            tabIndicator = null;
            for (var i = 0; i < informationEquipmentSlots.Length; i++)
            {
                informationEquipmentSlots[i] = null;
                informationEmptySlotSprites[i] = null;
            }
        }

        void WireRuntimeButtons()
        {
            if (runtimeRoot == null) return;
            Wire("Close", () => modal?.Close());
            Wire("InformationTab", () => ShowPage(detailPage));
            Wire("LevelTab", () => ShowPage(levelPage));
            Wire("ArtifactTab", () => ShowPage(artifactPage));
            Wire("WeaponTab", () => ShowPage(weaponPage));
            Wire("WeaponChangeBtn", OpenWeaponList);
            Wire("WeaponRefineBtn", RefineEquippedWeapon);
            Wire("WeaponEnhanceBtn", EnhanceEquippedWeapon);
            Wire("ConstellationTab", () => ShowPage(constellationPage));
            Wire("AffinityTab", () => ShowPage(affinityPage));
            Wire("BackToGallery", OpenGallery);
            Wire("LevelButton", OpenLevel);
            Wire("BackToDetail", () => ShowPage(detailPage));
            Wire("OpenArtifactList", () => ShowArtifactSubPage(artifactListPage));
            Wire("OpenArtifactDetail", () => ShowArtifactSubPage(artifactDetailPage));
            Wire("OpenBreakthrough", () => ShowBreakthrough());
            Wire("ConfirmBreakthrough", () => BreakthroughSelectedCharacter());
            Wire("BreakthroughButton", () => BreakthroughSelectedCharacter());
            Wire("ConstellationUpgrade", () => UnlockSelectedConstellation());
            Wire("ConstellationUpgradeButton", () => UnlockSelectedConstellation());
            Wire("AddExp500", () => AddSelectedCharacterExperience(500));
            Wire("AddExp2000", () => AddSelectedCharacterExperience(2000));
            Wire("AddExp5000", () => AddSelectedCharacterExperience(5000));
            for (var i = 0; i < informationEquipmentSlots.Length; i++)
                Wire($"InformationEquipmentSlot_{i}", () => ShowPage(artifactPage));
            Wire("SortCombatPower", () => SetGallerySort(GallerySortMode.CombatPower));
            Wire("SortConstellation", () => SetGallerySort(GallerySortMode.Constellation));
            Wire("SortQuality", () => SetGallerySort(GallerySortMode.Quality));
            Wire("SortAffinity", () => SetGallerySort(GallerySortMode.Affinity));
        }

        void Wire(string objectName, UnityEngine.Events.UnityAction action)
        {
            var button = FindDeep(runtimeRoot.transform, objectName)?.GetComponent<Button>();
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null) return null;
            if (root.name == objectName) return root;
            foreach (Transform child in root)
            {
                var result = FindDeep(child, objectName);
                if (result != null) return result;
            }
            return null;
        }

        GameObject BuildPage(string name, Sprite reference)
        {
            return BuildPage(name, reference, runtimeRoot.transform);
        }

        GameObject BuildPage(string name, Sprite reference, Transform parent)
        {
            var page = Rect(name, parent, Vector2.zero, Vector2.one).gameObject;
            var image = page.AddComponent<Image>();
            image.sprite = reference;
            image.color = reference != null ? Color.white : new Color(.08f, .14f, .24f, .98f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return page;
        }

        void BuildSharedNavigation()
        {
            AddHotspot(runtimeRoot.transform, "Close", new Vector2(.035f, .88f), new Vector2(.085f, .97f), () => modal?.Close());
            detailNavigation = Rect("TabNavigation", characterPage.transform, Vector2.zero, Vector2.one).gameObject;
            AddSpriteButton(detailNavigation.transform, "InformationTab", informationTabSprite, new Vector2(.855f, .70f), new Vector2(.965f, .77f), () => ShowPage(detailPage));
            AddSpriteButton(detailNavigation.transform, "LevelTab", levelTabSprite, new Vector2(.855f, .60f), new Vector2(.965f, .67f), () => ShowPage(levelPage));
            AddSpriteButton(detailNavigation.transform, "ArtifactTab", artifactTabSprite, new Vector2(.855f, .50f), new Vector2(.965f, .57f), () => ShowPage(artifactPage));
            AddSpriteButton(detailNavigation.transform, "WeaponTab", weaponTabSprite, new Vector2(.855f, .40f), new Vector2(.965f, .47f), () => ShowPage(weaponPage));
            AddSpriteButton(detailNavigation.transform, "ConstellationTab", constellationTabSprite, new Vector2(.855f, .30f), new Vector2(.965f, .37f), () => ShowPage(constellationPage));
            AddSpriteButton(detailNavigation.transform, "AffinityTab", affinityTabSprite, new Vector2(.855f, .20f), new Vector2(.965f, .27f), () => ShowPage(affinityPage));
            AddHotspot(detailNavigation.transform, "BackToGallery", new Vector2(.035f, .88f), new Vector2(.085f, .97f), OpenGallery);

            characterSelectorContent = Rect("OwnedCharacterSelector", detailNavigation.transform,
                new Vector2(.052f, .08f), new Vector2(.125f, .85f));
            var layout = characterSelectorContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            selectedNameLabel = AddText(detailNavigation.transform, "SelectedCharacterName", string.Empty,
                new Vector2(.24f, .12f), new Vector2(.48f, .20f), 25);
            selectedNameLabel.alignment = TextAlignmentOptions.Center;
        }

        void BuildGalleryOverlay()
        {
            var panel = AddImage(galleryPage.transform, "GalleryListPanel", new Vector2(.05f, .055f), new Vector2(.96f, .825f));
            panel.sprite = galleryPanelSprite;
            panel.preserveAspect = false;
            panel.color = galleryPanelSprite != null ? Color.white : new Color(.56f, .66f, .79f, .82f);
            AddSpriteButton(galleryPage.transform, "SortCombatPower", combatPowerButtonSprite,
                new Vector2(.083f, .70f), new Vector2(.187f, .753f), () => SetGallerySort(GallerySortMode.CombatPower));
            AddSpriteButton(galleryPage.transform, "SortConstellation", constellationButtonSprite,
                new Vector2(.083f, .62f), new Vector2(.187f, .673f), () => SetGallerySort(GallerySortMode.Constellation));
            AddSpriteButton(galleryPage.transform, "SortQuality", qualityButtonSprite,
                new Vector2(.083f, .54f), new Vector2(.187f, .593f), () => SetGallerySort(GallerySortMode.Quality));
            AddSpriteButton(galleryPage.transform, "SortAffinity", affinityButtonSprite,
                new Vector2(.083f, .46f), new Vector2(.187f, .513f), () => SetGallerySort(GallerySortMode.Affinity));
            var viewport = Rect("OwnedCharacterViewport", galleryPage.transform, new Vector2(.25f, .16f), new Vector2(.96f, .82f));
            var image = viewport.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, .015f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            galleryContent = Rect("OwnedCharacterContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            galleryContent.pivot = new Vector2(.5f, 1f);
            galleryContent.sizeDelta = new Vector2(0f, 800f);
            var grid = galleryContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(190f, 275f);
            grid.spacing = new Vector2(38f, 35f);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            var fitter = galleryContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = galleryContent;
            galleryCardTemplate = BuildGalleryCardTemplate(galleryContent);
            galleryCardTemplate.SetActive(false);
            emptyLabel = AddText(galleryPage.transform, "EmptyGallery", "Chưa sở hữu nhân vật nào.\nHãy vào Rate Up để triệu hồi.", new Vector2(.34f, .4f), new Vector2(.88f, .6f), 30);
        }

        void SetGallerySort(GallerySortMode mode)
        {
            gallerySortMode = mode;
            RefreshGallery();
        }

        void BuildDetailOverlay()
        {
            detailPortrait = AddImage(detailPage.transform, "SelectedPortrait", new Vector2(.30f, .12f), new Vector2(.61f, .82f));
            detailName = AddText(detailPage.transform, "CharacterName", string.Empty, new Vector2(.66f, .69f), new Vector2(.94f, .78f), 38);
            detailStats = AddText(detailPage.transform, "CharacterStats", string.Empty, new Vector2(.66f, .42f), new Vector2(.94f, .68f), 25);
            detailDescription = AddText(detailPage.transform, "CharacterDescription", string.Empty, new Vector2(.66f, .23f), new Vector2(.94f, .41f), 22);
            AddButton(detailPage.transform, "LevelButton", "NÂNG LEVEL", new Vector2(.70f, .12f), new Vector2(.90f, .20f), OpenLevel);
        }

        void BuildLevelOverlay()
        {
            levelPortrait = AddImage(levelPage.transform, "LevelPortrait", new Vector2(.29f, .12f), new Vector2(.58f, .82f));
            levelName = AddText(levelPage.transform, "LevelCharacterName", string.Empty, new Vector2(.64f, .68f), new Vector2(.93f, .77f), 36);
            levelValue = AddText(levelPage.transform, "LevelValue", string.Empty, new Vector2(.64f, .48f), new Vector2(.93f, .66f), 28);
            AddText(levelPage.transform, "LevelItemHint", "Vật phẩm nâng cấp được lấy từ Túi đồ", new Vector2(.64f, .28f), new Vector2(.93f, .42f), 22);
            AddButton(levelPage.transform, "BackToDetail", "QUAY LẠI", new Vector2(.70f, .14f), new Vector2(.90f, .22f), () => ShowPage(detailPage));
        }

        void RefreshGallery()
        {
            if (galleryContent == null) return;
            foreach (var card in generatedCards) if (card != null) Destroy(card);
            generatedCards.Clear();
            var owned = new List<CharacterEntry>();
            var roster = PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            if (roster != null)
            {
                foreach (var member in roster.GetUnlockedRosterMembers())
                {
                    var entry = database?.FindCharacter(member.characterId);
                    if (entry == null) continue;
                    owned.Add(entry);
                }
            }
            owned.Sort((a, b) => SortValue(b).CompareTo(SortValue(a)));
            foreach (var entry in owned) BuildCard(entry);
            if (emptyLabel != null) emptyLabel.gameObject.SetActive(owned.Count == 0);
            RefreshCharacterSelectors();
        }

        int SortValue(CharacterEntry entry) => gallerySortMode switch
        {
            GallerySortMode.Constellation => CharacterProgressionState.GetConstellation(entry.id),
            GallerySortMode.Quality => entry.quality,
            GallerySortMode.Affinity => entry.affinity,
            _ => entry.combatPower > 0
                ? entry.combatPower
                : entry.attack * 10 + entry.maxHealth + CharacterProgressionState.GetLevel(entry.id) * 5
        };

        void RefreshCharacterSelectors()
        {
            if (characterSelectorContent == null) return;
            foreach (var selector in generatedSelectors) if (selector != null) Destroy(selector);
            generatedSelectors.Clear();

            var roster = PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            if (roster == null) return;
            foreach (var member in roster.GetUnlockedRosterMembers())
            {
                var entry = database?.FindCharacter(member.characterId);
                if (entry == null) continue;
                var id = entry.id;
                var selector = new GameObject(id, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                selector.transform.SetParent(characterSelectorContent, false);
                selector.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 92f);
                selector.GetComponent<LayoutElement>().preferredHeight = 92f;
                var image = selector.GetComponent<Image>();
                image.sprite = entry.portrait;
                image.preserveAspect = true;
                image.color = id == selectedCharacterId ? Color.white : new Color(.68f, .68f, .68f, 1f);
                selector.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(id));
                generatedSelectors.Add(selector);
            }
        }

        void SelectCharacter(string characterId)
        {
            if (database?.FindCharacter(characterId) == null) return;
            selectedCharacterId = characterId;
            RefreshCharacter();
            RefreshCharacterSelectors();
        }

        void BuildCard(CharacterEntry entry)
        {
            if (galleryCardTemplate == null) return;
            var card = Instantiate(galleryCardTemplate, galleryContent);
            card.name = entry.id;
            card.SetActive(true);
            var background = card.GetComponent<Image>();
            background.sprite = BackgroundFor(entry.rarity);
            background.color = Color.white;
            var portrait = FindDeep(card.transform, "Portrait")?.GetComponent<Image>();
            if (portrait != null) { portrait.sprite = entry.portrait; portrait.preserveAspect = true; }
            var element = FindDeep(card.transform, "Element")?.GetComponent<Image>();
            if (element != null) { element.sprite = entry.elementIcon; element.enabled = entry.elementIcon != null; }
            var name = FindDeep(card.transform, "CharacterName")?.GetComponent<TMP_Text>();
            var level = FindDeep(card.transform, "CharacterLevel")?.GetComponent<TMP_Text>();
            var raisedStars = Mathf.Clamp(entry.rarity, 0, 6);
            for (var i = 0; i < 6; i++)
            {
                var star = FindDeep(card.transform, $"Star_{i}")?.GetComponent<Image>();
                if (star == null) continue;
                star.gameObject.SetActive(i < entry.rarity);
                star.sprite = i < raisedStars ? galleryStarSprite : emptyStarSprite;
            }
            if (name != null) name.text = entry.displayName;
            if (level != null) level.text = $"Lv.{CharacterProgressionState.GetLevel(entry.id)}";
            card.GetComponent<Button>().onClick.AddListener(() => OpenCharacter(entry.id));
            generatedCards.Add(card);
        }

        Sprite BackgroundFor(int rarity)
        {
            foreach (var mapping in rarityBackgrounds)
                if (mapping != null && mapping.rarity == rarity) return mapping.background;
            return null;
        }

        void RefreshCharacter()
        {
            var entry = database?.FindCharacter(selectedCharacterId);
            if (entry == null) return;
            if (detailPortrait != null) detailPortrait.sprite = entry.fullBody != null ? entry.fullBody : entry.portrait;
            if (levelPortrait != null) levelPortrait.sprite = entry.fullBody != null ? entry.fullBody : entry.portrait;
            if (detailName != null) detailName.text = entry.displayName;
            var currentLevel = CharacterProgressionState.GetLevel(entry.id);
            var levelCap = CharacterProgressionState.GetLevelCap(entry.id);
            if (detailStats != null) detailStats.text = $"Cấp {currentLevel}/{levelCap}\nHP  {entry.maxHealth}\nTấn công  {entry.attack}";
            if (detailDescription != null) detailDescription.text = entry.description;
            if (levelName != null) levelName.text = entry.displayName;
            if (levelValue != null) levelValue.text = currentLevel >= levelCap && levelCap < CharacterProgressionState.AbsoluteMaxLevel
                ? $"Cấp hiện tại: {currentLevel}/{levelCap}\nCần đột phá để tăng giới hạn"
                : $"Cấp hiện tại: {currentLevel}/{levelCap}\nEXP: {CharacterProgressionState.GetExperience(entry.id)}/{CharacterProgressionState.GetExperienceToNextLevel(entry.id)}";
            if (selectedNameLabel != null) selectedNameLabel.text = $"{entry.displayName}  Lv.{currentLevel}";
            if (selectedElementIcon != null)
            {
                selectedElementIcon.sprite = entry.elementIcon;
                selectedElementIcon.enabled = entry.elementIcon != null;
            }
            for (var i = 0; i < informationEquipmentSlots.Length; i++)
            {
                var slot = informationEquipmentSlots[i];
                if (slot == null) continue;
                var equipped = entry.equippedArtifacts != null && i < entry.equippedArtifacts.Count
                    ? entry.equippedArtifacts[i]
                    : null;
                slot.sprite = equipped != null ? equipped : informationEmptySlotSprites[i];
                slot.enabled = slot.sprite != null;
            }
            homeController?.SelectCharacter(entry.id);
        }

        string ResolveOwnedCharacter(string requested)
        {
            var roster = PartyRoster.Instance ?? FindAnyObjectByType<PartyRoster>();
            if (roster != null && roster.IsCharacterUnlocked(requested) && database?.FindCharacter(requested) != null) return requested;
            if (roster != null)
                foreach (var member in roster.GetUnlockedRosterMembers())
                    if (database?.FindCharacter(member.characterId) != null) return member.characterId;
            return database != null && database.characters.Count > 0 ? database.characters[0].id : requested;
        }

        void ShowPage(GameObject page)
        {
            if (galleryPage != null) galleryPage.SetActive(page == galleryPage);
            if (characterPage != null) characterPage.SetActive(page != galleryPage);
            if (detailPage != null) detailPage.SetActive(page == detailPage);
            if (levelPage != null) levelPage.SetActive(page == levelPage);
            if (affinityPage != null) affinityPage.SetActive(page == affinityPage);
            if (constellationPage != null) constellationPage.SetActive(page == constellationPage);
            if (artifactPage != null) artifactPage.SetActive(page == artifactPage);
            
            if (weaponPage != null)
            {
                weaponPage.SetActive(page == weaponPage);
                if (page == weaponPage)
                {
                    RefreshWeaponUI();
                    if (weaponListPage != null) weaponListPage.SetActive(false);
                }
            }

            if (detailNavigation != null) detailNavigation.SetActive(page != galleryPage);
            if (artifactListPage != null) artifactListPage.SetActive(false);
            if (artifactDetailPage != null) artifactDetailPage.SetActive(false);
            if (breakthroughPage != null) breakthroughPage.SetActive(false);
            MoveTabIndicator(page);
        }

        void ShowArtifactSubPage(GameObject subPage)
        {
            ShowPage(artifactPage);
            if (subPage != null) subPage.SetActive(true);
        }

        void ShowBreakthrough()
        {
            ShowPage(levelPage);
            if (breakthroughPage != null) breakthroughPage.SetActive(true);
        }

        void MoveTabIndicator(GameObject page)
        {
            if (tabIndicator == null || detailNavigation == null || page == galleryPage) return;
            var buttonName = page == detailPage ? "InformationTab"
                : page == levelPage ? "LevelTab"
                : page == artifactPage ? "ArtifactTab"
                : page == weaponPage ? "WeaponTab"
                : page == constellationPage ? "ConstellationTab"
                : "AffinityTab";
            var target = FindDeep(detailNavigation.transform, buttonName) as RectTransform;
            if (target == null) return;
            var y = (target.anchorMin.y + target.anchorMax.y) * .5f;
            tabIndicator.anchorMin = new Vector2(target.anchorMax.x + .006f, y - .012f);
            tabIndicator.anchorMax = new Vector2(target.anchorMax.x + .026f, y + .012f);
            tabIndicator.offsetMin = tabIndicator.offsetMax = Vector2.zero;
            tabIndicator.SetAsLastSibling();
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image AddImage(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            image.color = Color.white; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        static TMP_Text AddText(Transform parent, string name, string value, Vector2 min, Vector2 max, float size)
        {
            var text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.color = new Color(.28f, .16f, .11f); text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
            return text;
        }

        GameObject BuildGalleryCardTemplate(Transform parent)
        {
            var card = new GameObject("CharacterCardTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = Color.white;
            var portrait = AddImage(card.transform, "Portrait", new Vector2(.04f, .22f), new Vector2(.96f, .98f));
            portrait.color = Color.white;
            var element = AddImage(card.transform, "Element", new Vector2(0f, .74f), new Vector2(.34f, 1f));
            element.color = Color.white;
            var stars = Rect("Stars", card.transform, new Vector2(.08f, .17f), new Vector2(.92f, .27f));
            var starLayout = stars.gameObject.AddComponent<HorizontalLayoutGroup>();
            starLayout.spacing = 2f;
            starLayout.childAlignment = TextAnchor.MiddleCenter;
            starLayout.childControlWidth = false;
            starLayout.childControlHeight = false;
            starLayout.childForceExpandWidth = false;
            starLayout.childForceExpandHeight = false;
            for (var i = 0; i < 6; i++)
            {
                var star = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                star.transform.SetParent(stars, false);
                star.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);
                var starImage = star.GetComponent<Image>();
                starImage.sprite = emptyStarSprite;
                starImage.preserveAspect = true;
                starImage.raycastTarget = false;
                var starLayoutElement = star.GetComponent<LayoutElement>();
                starLayoutElement.preferredWidth = 24f;
                starLayoutElement.preferredHeight = 24f;
            }
            var characterName = AddText(card.transform, "CharacterName", "Tên nhân vật", new Vector2(.04f, .08f), new Vector2(.96f, .18f), 17);
            characterName.alignment = TextAlignmentOptions.Center;
            characterName.color = Color.white;
            var level = AddText(card.transform, "CharacterLevel", "Lv.1", new Vector2(.04f, 0f), new Vector2(.96f, .10f), 17);
            level.alignment = TextAlignmentOptions.Center;
            level.color = Color.white;
            return card;
        }

        static Button AddSpriteButton(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max,
            UnityEngine.Events.UnityAction action)
        {
            var go = Rect(name, parent, min, max).gameObject;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : new Color(.98f, .94f, .84f, .96f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(action);
            return button;
        }

        static Button AddButton(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var go = Rect(name, parent, min, max).gameObject;
            var image = go.AddComponent<Image>(); image.color = new Color(.98f, .94f, .84f, .96f);
            var button = go.AddComponent<Button>(); button.onClick.AddListener(action);
            var text = AddText(go.transform, "Label", label, Vector2.zero, Vector2.one, 20); text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        static Button AddHotspot(Transform parent, string name, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var go = Rect(name, parent, min, max).gameObject;
            var image = go.AddComponent<Image>(); image.color = new Color(1f, 1f, 1f, .001f);
            var button = go.AddComponent<Button>(); button.onClick.AddListener(action);
            return button;
        }

        void BuildWeaponOverlay()
        {
            if (weaponPage == null) return;

            // 1. Create the details frame
            weaponPageFrame = AddImage(weaponPage.transform, "WeaponDetailFrame", new Vector2(.525f, .075f), new Vector2(.80f, .88f));
            weaponPageFrame.sprite = weaponDetailFrameSprite;
            weaponPageFrame.color = Color.white;
            weaponPageFrame.preserveAspect = false;

            // 2. Create the weapon slot/frame
            weaponPageSlot = AddImage(weaponPageFrame.transform, "WeaponSlotFrame", new Vector2(.05f, .70f), new Vector2(.30f, .95f));
            weaponPageSlot.sprite = weaponSlotFrameSprite;
            weaponPageSlot.color = Color.white;
            weaponPageSlot.preserveAspect = true;

            // 3. Create the weapon icon inside the slot
            var iconGo = Rect("WeaponIcon", weaponPageSlot.transform, new Vector2(.15f, .15f), new Vector2(.85f, .85f));
            weaponPageIcon = iconGo.gameObject.AddComponent<Image>();
            weaponPageIcon.preserveAspect = true;
            weaponPageIcon.raycastTarget = false;

            // 4. Create the weapon name text
            weaponPageName = AddText(weaponPageFrame.transform, "WeaponNameText", "Weapon Name", new Vector2(.35f, .80f), new Vector2(.95f, .95f), 28);
            weaponPageName.alignment = TextAlignmentOptions.MidlineLeft;

            // 5. Create stats text (ATK, HP, Level, Refinement)
            weaponPageStats = AddText(weaponPageFrame.transform, "WeaponStatsText", "ATK: 0\nHP: 0\nLv. 1 / 80\nTinh luyện 1", new Vector2(.05f, .40f), new Vector2(.95f, .65f), 24);
            weaponPageStats.alignment = TextAlignmentOptions.TopLeft;

            // 6. Create description text
            weaponPageDesc = AddText(weaponPageFrame.transform, "WeaponDescText", "Weapon description and skill effect", new Vector2(.05f, .12f), new Vector2(.95f, .38f), 20);
            weaponPageDesc.alignment = TextAlignmentOptions.TopLeft;

            // 7. Add buttons: "Thay đổi", "Tinh luyện", "Nâng cấp"
            AddSpriteButton(weaponPageFrame.transform, "WeaponChangeBtn", weaponChangeSprite, new Vector2(.05f, .02f), new Vector2(.32f, .09f), OpenWeaponList);
            AddSpriteButton(weaponPageFrame.transform, "WeaponRefineBtn", weaponRefineSprite, new Vector2(.36f, .02f), new Vector2(.63f, .09f), RefineEquippedWeapon);
            AddSpriteButton(weaponPageFrame.transform, "WeaponEnhanceBtn", weaponEnhanceSprite, new Vector2(.67f, .02f), new Vector2(.94f, .09f), EnhanceEquippedWeapon);

            // 8. Build the weapon list overlay page (inactive by default)
            weaponListPage = Rect("WeaponListContent", weaponPage.transform, new Vector2(.05f, .055f), new Vector2(.50f, .825f)).gameObject;
            var listBg = weaponListPage.AddComponent<Image>();
            listBg.sprite = galleryPanelSprite;
            listBg.color = Color.white;
            listBg.preserveAspect = false;

            // Title of selection list
            AddText(weaponListPage.transform, "Title", "Chọn Vũ Khí", new Vector2(.05f, .88f), new Vector2(.95f, .97f), 26).alignment = TextAlignmentOptions.Center;

            // Scroll view for weapons
            var viewport = Rect("WeaponViewport", weaponListPage.transform, new Vector2(.05f, .05f), new Vector2(.95f, .85f));
            var mask = viewport.gameObject.AddComponent<Image>();
            mask.color = new Color(1f, 1f, 1f, .015f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var listContent = Rect("WeaponListGrid", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            listContent.pivot = new Vector2(.5f, 1f);
            var grid = listContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100f, 100f);
            grid.spacing = new Vector2(16f, 16f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            var fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = listContent;

            weaponListPage.SetActive(false);
        }

        void RefreshWeaponUI()
        {
            if (weaponPageFrame == null || !weaponPage.activeSelf) return;

            var equipped = EquippedWeaponState.Instance;
            var activeWeapon = equipped?.EquippedWeapon;
            if (activeWeapon == null) return;

            if (weaponPageName != null) weaponPageName.text = activeWeapon.displayName;

            var lvl = equipped.Level;
            var refi = equipped.Refinement;
            var displayAtk = activeWeapon.baseAtk + (lvl - 1) * 8 + refi * 12;
            var currentSubStatValue = activeWeapon.subStatValue + (lvl - 1) * (activeWeapon.subStatValue * 0.05f);

            if (weaponPageStats != null)
            {
                weaponPageStats.text = $"Tấn công: {displayAtk}\nHP tối đa: {activeWeapon.baseHp}\nCấp: {lvl} / {activeWeapon.maxLevel}\nTinh luyện: {refi}";
            }

            if (weaponPageDesc != null)
            {
                weaponPageDesc.text = $"<b>[Thuộc tính phụ]</b> {activeWeapon.subStatName}: +{currentSubStatValue:F1}%\n\n" +
                    $"<b>[Kỹ năng]</b> {WeaponScreenUI.GetWeaponSkillDescription(activeWeapon.weaponId, refi)}";
            }

            // Load weapon icon sprite from ItemDatabase using weaponId
            if (weaponPageIcon != null)
            {
                var itemDb = Resources.Load<Gameplay.ItemDatabase>("Data/ItemDatabase");
                var itemDef = itemDb?.Get(activeWeapon.weaponId);
                weaponPageIcon.sprite = itemDef?.icon;
                weaponPageIcon.enabled = weaponPageIcon.sprite != null;
            }
        }

        void OpenWeaponList()
        {
            if (weaponListPage == null) return;
            weaponListPage.SetActive(!weaponListPage.activeSelf);
            if (weaponListPage.activeSelf)
            {
                RefreshWeaponList();
            }
        }

        void RefreshWeaponList()
        {
            var grid = weaponListPage.transform.Find("WeaponViewport/WeaponListGrid");
            if (grid == null) return;

            // Clear old cards
            foreach (Transform child in grid)
            {
                Destroy(child.gameObject);
            }

            var equipped = EquippedWeaponState.Instance;
            var itemDb = Resources.Load<Gameplay.ItemDatabase>("Data/ItemDatabase");
            var weaponDb = Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
            if (equipped == null || weaponDb == null) return;

            foreach (var weaponId in equipped.OwnedWeaponIds)
            {
                var weapon = weaponDb.GetById(weaponId);
                if (weapon == null) continue;

                var itemDef = itemDb?.Get(weaponId);

                var slot = new GameObject(weaponId, typeof(RectTransform), typeof(Image), typeof(Button));
                slot.transform.SetParent(grid, false);
                slot.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);

                var bgImage = slot.GetComponent<Image>();
                bgImage.sprite = weaponSlotFrameSprite;
                bgImage.color = Color.white;

                var iconGo = Rect("Icon", slot.transform, new Vector2(.15f, .15f), new Vector2(.85f, .85f));
                var iconImage = iconGo.gameObject.AddComponent<Image>();
                iconImage.sprite = itemDef?.icon;
                iconImage.preserveAspect = true;
                iconImage.enabled = iconImage.sprite != null;

                // Highlight if equipped
                if (equipped.EquippedWeaponId == weaponId)
                {
                    var highlight = AddImage(slot.transform, "Highlight", new Vector2(-.05f, -.05f), new Vector2(1.05f, 1.05f));
                    highlight.color = new Color(0.95f, 0.78f, 0.28f, 0.5f);
                    highlight.transform.SetAsFirstSibling();
                }

                slot.GetComponent<Button>().onClick.AddListener(() =>
                {
                    equipped.Equip(weaponId);
                    weaponListPage.SetActive(false);
                    RefreshWeaponUI();
                    RefreshCharacter(); // refresh character stats like ATK
                    GameManager.Instance?.SaveGame();
                });
            }
        }

        void RefineEquippedWeapon()
        {
            var equipped = EquippedWeaponState.Instance;
            if (equipped == null) return;
            equipped.EnhanceRefinement(1);
            RefreshWeaponUI();
            RefreshCharacter();
            GameManager.Instance?.SaveGame();
        }

        void EnhanceEquippedWeapon()
        {
            var equipped = EquippedWeaponState.Instance;
            if (equipped == null) return;
            equipped.EnhanceLevel(5); // increase by 5 levels for testing
            RefreshWeaponUI();
            RefreshCharacter();
            GameManager.Instance?.SaveGame();
        }
    }
}
