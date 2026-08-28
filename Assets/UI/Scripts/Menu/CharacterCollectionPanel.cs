using System.Collections.Generic;
using BES.Core;
using BES.Gameplay;
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
        [Header("Owned character selector")]
        [SerializeField] RectTransform characterSelectorContent;
        [SerializeField] Vector2 selectorCardSize = new Vector2(92f, 92f);
        RectTransform galleryContent;
        GameObject galleryCardTemplate;
        Image detailPortrait;
        Image levelPortrait;
        TMP_Text detailName;
        [Header("Character information stat texts")]
        [SerializeField] TMP_Text detailLevelText;
        [SerializeField] TMP_Text detailHealthText;
        [SerializeField] TMP_Text detailAttackText;
        [SerializeField] TMP_Text detailDefenseText;
        [SerializeField] TMP_Text detailSpeedText;
        [SerializeField] TMP_Text detailEnergyText;
        [SerializeField] TMP_Text detailCritRateText;
        [SerializeField] TMP_Text detailCritDamageText;
        [SerializeField] TMP_Text detailElementText;
        [SerializeField] TMP_Text detailRoleText;
        TMP_Text legacyDetailStats;
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

        public enum CharacterCollectionDestination
        {
            Detail,
            Level,
            Equipment,
            Skill,
            Constellation
        }

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
            ResolveDatabase();
            database?.NormalizeCharacterCombatDefaults();
            modal ??= GetComponent<SimpleModalPanel>();
            EnsureExistingUI();
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
            ResolveDatabase();
            GameEvents.OnPartyChanged += RefreshGallery;
            if (runtimeRoot != null)
            {
                RefreshGallery();
                RefreshCharacterSelectors();
                if (!string.IsNullOrEmpty(selectedCharacterId))
                    RefreshCharacter();
            }
        }

        void OnDisable() => GameEvents.OnPartyChanged -= RefreshGallery;

        public void OpenGallery()
        {
            ResolveDatabase();
            if (!EnsureExistingUI()) return;
            ShowPage(galleryPage);
            RefreshGallery();
            modal?.Open();
        }

        public void OpenCharacter(string characterId)
        {
            ResolveDatabase();
            if (!EnsureExistingUI()) return;
            selectedCharacterId = CharacterOwnership.ResolveOwnedId(characterId, database);
            CharacterOwnership.Focus(selectedCharacterId);
            RefreshCharacter();
            RefreshCharacterSelectors();
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

        public void OpenDestination(CharacterCollectionDestination destination, string characterId = null)
        {
            ResolveDatabase();
            if (!EnsureExistingUI()) return;
            selectedCharacterId = CharacterOwnership.ResolveOwnedId(string.IsNullOrWhiteSpace(characterId) ? selectedCharacterId : characterId, database);
            CharacterOwnership.Focus(selectedCharacterId);
            RefreshCharacter();
            RefreshCharacterSelectors();

            switch (destination)
            {
                case CharacterCollectionDestination.Level:
                    ShowPage(levelPage);
                    break;
                case CharacterCollectionDestination.Equipment:
                    ShowPage(artifactPage);
                    break;
                case CharacterCollectionDestination.Skill:
                    ShowPage(detailPage);
                    break;
                case CharacterCollectionDestination.Constellation:
                    ShowPage(constellationPage);
                    break;
                default:
                    ShowPage(detailPage);
                    break;
            }

            modal?.Open();
        }

        bool EnsureExistingUI()
        {
            if (runtimeRoot != null) return true;
            if (CacheExistingUI()) return true;
            Debug.LogWarning("[BES] CharacterCollectionPanel is missing editable prefab UI. Runtime UI creation is disabled; create CharacterCollectionRuntime/CharacterPage/InformationContent in prefab and assign fields in Unity.");
            return false;
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
                    levelValue.text = $"Cáº¥p hiá»‡n táº¡i: {currentLevel}/{levelCap}\nCáº§n Ä‘á»™t phÃ¡ Ä‘á»ƒ tÄƒng giá»›i háº¡n";
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
                    levelValue.text = $"Cáº¥p hiá»‡n táº¡i: {currentLevel}/{levelCap}\nKhÃ´ng Ä‘á»§ lá» EXP tÆ°Æ¡ng á»©ng!";
                return;
            }

            if (PlayerWallet.Instance == null || PlayerWallet.Instance.Coins < goldCost)
            {
                if (levelValue != null)
                    levelValue.text = $"Cáº¥p hiá»‡n táº¡i: {currentLevel}/{levelCap}\nKhÃ´ng Ä‘á»§ VÃ ng Ä‘á»ƒ nÃ¢ng cáº¥p!";
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
            characterSelectorContent ??= FindDeep(existing, "OwnedCharacterSelector") as RectTransform;
            characterSelectorContent ??= FindDeep(existing, "OwnedCharacterList") as RectTransform;
            characterSelectorContent ??= FindDeep(existing, "CharacterOwnedList") as RectTransform;
            characterSelectorContent ??= FindDeep(existing, "CharacterList") as RectTransform;
            detailPortrait = FindDeep(existing, "SelectedPortrait")?.GetComponent<Image>();
            levelPortrait = FindDeep(existing, "LevelPortrait")?.GetComponent<Image>();
            detailName = FindDeep(existing, "CharacterName")?.GetComponent<TMP_Text>();
            detailLevelText = FindText(existing, "CharacterLevelStatText", "LevelStatText", "InfoLevelText");
            detailHealthText = FindText(existing, "CharacterHealthStatText", "HealthStatText", "InfoHealthText");
            detailAttackText = FindText(existing, "CharacterAttackStatText", "AttackStatText", "InfoAttackText");
            detailDefenseText = FindText(existing, "CharacterDefenseStatText", "DefenseStatText", "InfoDefenseText");
            detailSpeedText = FindText(existing, "CharacterSpeedStatText", "SpeedStatText", "InfoSpeedText");
            detailEnergyText = FindText(existing, "CharacterEnergyStatText", "EnergyStatText", "InfoEnergyText", "NapNangLuongText");
            detailCritRateText = FindText(existing, "CharacterCritRateStatText", "CritRateStatText", "InfoCritRateText", "BaoKichText");
            detailCritDamageText = FindText(existing, "CharacterCritDamageStatText", "CritDamageStatText", "InfoCritDamageText", "SatThuongBaoKichText");
            detailElementText = FindText(existing, "CharacterElementText", "ElementText", "InfoElementText", "HeNhanVatText");
            detailRoleText = FindText(existing, "CharacterRoleText", "RoleText", "InfoRoleText", "VaiTroText");
            legacyDetailStats = FindDeep(existing, "CharacterStats")?.GetComponent<TMP_Text>();
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
            detailName = detailDescription = levelName = levelValue = emptyLabel = selectedNameLabel = null;
            detailLevelText = detailHealthText = detailAttackText = detailDefenseText = detailSpeedText = legacyDetailStats = null;
            detailEnergyText = detailCritRateText = detailCritDamageText = detailElementText = detailRoleText = null;
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
            Wire("AffinityTab", () => { ShowPage(affinityPage); RefreshAffinity(); });
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
            emptyLabel = AddText(galleryPage.transform, "EmptyGallery", "ChÆ°a sá»Ÿ há»¯u nhÃ¢n váº­t nÃ o.\nHÃ£y vÃ o Rate Up Ä‘á»ƒ triá»‡u há»“i.", new Vector2(.34f, .4f), new Vector2(.88f, .6f), 30);
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
            detailLevelText = AddText(detailPage.transform, "CharacterLevelStatText", string.Empty, new Vector2(.66f, .63f), new Vector2(.94f, .68f), 25);
            detailHealthText = AddText(detailPage.transform, "CharacterHealthStatText", string.Empty, new Vector2(.66f, .58f), new Vector2(.94f, .63f), 25);
            detailAttackText = AddText(detailPage.transform, "CharacterAttackStatText", string.Empty, new Vector2(.66f, .53f), new Vector2(.94f, .58f), 25);
            detailDefenseText = AddText(detailPage.transform, "CharacterDefenseStatText", string.Empty, new Vector2(.66f, .48f), new Vector2(.94f, .53f), 25);
            detailSpeedText = AddText(detailPage.transform, "CharacterSpeedStatText", string.Empty, new Vector2(.66f, .43f), new Vector2(.94f, .48f), 25);
            detailDescription = AddText(detailPage.transform, "CharacterDescription", string.Empty, new Vector2(.66f, .23f), new Vector2(.94f, .41f), 22);
            AddButton(detailPage.transform, "LevelButton", "NÃ‚NG LEVEL", new Vector2(.70f, .12f), new Vector2(.90f, .20f), OpenLevel);
        }

        void BuildLevelOverlay()
        {
            levelPortrait = AddImage(levelPage.transform, "LevelPortrait", new Vector2(.29f, .12f), new Vector2(.58f, .82f));
            levelName = AddText(levelPage.transform, "LevelCharacterName", string.Empty, new Vector2(.64f, .68f), new Vector2(.93f, .77f), 36);
            levelValue = AddText(levelPage.transform, "LevelValue", string.Empty, new Vector2(.64f, .48f), new Vector2(.93f, .66f), 28);
            AddText(levelPage.transform, "LevelItemHint", "Váº­t pháº©m nÃ¢ng cáº¥p Ä‘Æ°á»£c láº¥y tá»« TÃºi Ä‘á»“", new Vector2(.64f, .28f), new Vector2(.93f, .42f), 22);
            AddButton(levelPage.transform, "BackToDetail", "QUAY Láº I", new Vector2(.70f, .14f), new Vector2(.90f, .22f), () => ShowPage(detailPage));
        }

        void RefreshGallery()
        {
            ResolveDatabase();
            RefreshCharacterSelectors();
            if (galleryContent == null) return;
            generatedCards.Clear();
            var owned = GetDisplayCharacters();
            owned.Sort((a, b) => SortValue(b).CompareTo(SortValue(a)));
            PopulateExistingCharacterSlots(galleryContent, owned, true);
            if (emptyLabel != null) emptyLabel.gameObject.SetActive(owned.Count == 0);
        }

        int SortValue(CharacterEntry entry) => gallerySortMode switch
        {
            GallerySortMode.Constellation => CharacterProgressionState.GetConstellation(entry.id),
            GallerySortMode.Quality => entry.quality,
            GallerySortMode.Affinity => CharacterProgressionState.GetAffinity(entry.id),
            _ => entry.combatPower > 0
                ? entry.combatPower
                : entry.attack * 10 + entry.maxHealth + CharacterProgressionState.GetLevel(entry.id) * 5
        };

        void RefreshCharacterSelectors()
        {
            ResolveDatabase();
            if (characterSelectorContent == null) return;
            generatedSelectors.Clear();

            var owned = GetDisplayCharacters();
            owned.Sort((a, b) => SortValue(b).CompareTo(SortValue(a)));
            PopulateExistingCharacterSlots(characterSelectorContent, owned, false);
        }

        List<CharacterEntry> GetDisplayCharacters()
        {
            ResolveDatabase();
            var owned = new List<CharacterEntry>(CharacterOwnership.GetOwnedEntries(database));
            if (owned.Count > 0 || database?.characters == null)
                return owned;

            foreach (var entry in database.characters)
                if (entry != null && entry.playable)
                    owned.Add(entry);
            return owned;
        }

        void PopulateExistingCharacterSlots(RectTransform parent, IReadOnlyList<CharacterEntry> owned, bool galleryStyle)
        {
            if (parent == null) return;
            var slotIndex = 0;
            foreach (Transform child in parent)
            {
                if (child == null || child.name.Contains("Template")) continue;

                var slotObject = child.gameObject;
                var entry = slotIndex < owned.Count ? owned[slotIndex] : null;
                slotObject.SetActive(entry != null);

                if (entry != null)
                {
                    PopulateCharacterSlot(slotObject, entry, galleryStyle);
                    if (!galleryStyle) generatedSelectors.Add(slotObject);
                    else generatedCards.Add(slotObject);
                }

                slotIndex++;
            }
        }

        void PopulateCharacterSlot(GameObject slotObject, CharacterEntry entry, bool galleryStyle)
        {
            if (slotObject == null || entry == null) return;

            var id = entry.id;
            slotObject.name = id;

            var background = slotObject.GetComponent<Image>();
            if (background != null)
            {
                if (galleryStyle)
                    background.sprite = BackgroundFor(entry.rarity);
                background.color = id == selectedCharacterId ? Color.white : new Color(.68f, .68f, .68f, 1f);
            }

            var portrait = FindDeep(slotObject.transform, "Portrait")?.GetComponent<Image>()
                ?? FindDeep(slotObject.transform, "CharacterPortrait")?.GetComponent<Image>()
                ?? FindDeep(slotObject.transform, "Icon")?.GetComponent<Image>()
                ?? slotObject.GetComponent<Image>();
            if (portrait != null)
            {
                portrait.sprite = CharacterChibiSprite(entry);
                portrait.preserveAspect = true;
                portrait.color = Color.white;
                portrait.enabled = portrait.sprite != null;
            }

            var element = FindDeep(slotObject.transform, "Element")?.GetComponent<Image>()
                ?? FindDeep(slotObject.transform, "ElementIcon")?.GetComponent<Image>();
            if (element != null)
            {
                element.sprite = entry.elementIcon;
                element.enabled = entry.elementIcon != null;
            }

            var name = FindDeep(slotObject.transform, "CharacterName")?.GetComponent<TMP_Text>()
                ?? FindDeep(slotObject.transform, "Name")?.GetComponent<TMP_Text>();
            if (name != null) name.text = entry.displayName;

            var level = FindDeep(slotObject.transform, "CharacterLevel")?.GetComponent<TMP_Text>()
                ?? FindDeep(slotObject.transform, "Level")?.GetComponent<TMP_Text>();
            if (level != null) level.text = $"Lv.{CharacterProgressionState.GetLevel(entry.id)}";

            var button = slotObject.GetComponent<Button>() ?? slotObject.GetComponentInChildren<Button>(true);
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (galleryStyle) OpenCharacter(id);
                    else SelectCharacter(id);
                });
            }
        }

        void SelectCharacter(string characterId)
        {
            ResolveDatabase();
            if (database?.FindCharacter(characterId) == null) return;
            selectedCharacterId = CharacterIdentity.Canonical(characterId, database);
            CharacterOwnership.Focus(selectedCharacterId);
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
            if (portrait != null) { portrait.sprite = CharacterChibiSprite(entry); portrait.preserveAspect = true; }
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

        void ResolveDatabase()
        {
            if (database != null) return;
            database = Resources.Load<MenuContentDatabase>("Data/MenuContentDatabase");
#if UNITY_EDITOR
            if (database == null)
                database = UnityEditor.AssetDatabase.LoadAssetAtPath<MenuContentDatabase>("Assets/Scenes/MenuContentDatabase.asset");
#endif
        }

        void RefreshCharacter()
        {
            var entry = database?.FindCharacter(selectedCharacterId);
            if (entry == null) return;
            if (detailPortrait != null) detailPortrait.sprite = CharacterFullBodySprite(entry);
            if (levelPortrait != null) levelPortrait.sprite = CharacterFullBodySprite(entry);
            if (detailName != null) detailName.text = entry.displayName;
            var currentLevel = CharacterProgressionState.GetLevel(entry.id);
            var levelCap = CharacterProgressionState.GetLevelCap(entry.id);
            var stats = CalculateDisplayStats(entry, currentLevel);
            SetText(detailLevelText, currentLevel.ToString());
            SetText(detailHealthText, stats.health.ToString());
            SetText(detailAttackText, stats.attack.ToString());
            SetText(detailDefenseText, stats.defense.ToString());
            SetText(detailSpeedText, stats.speed.ToString());
            SetText(detailEnergyText, $"{Mathf.Max(1, entry.energyTurns)} lượt");
            SetText(detailCritRateText, $"{Mathf.RoundToInt(Mathf.Clamp01(entry.critRate) * 100f)}%");
            SetText(detailCritDamageText, $"{Mathf.RoundToInt(Mathf.Max(1f, entry.critDamageMultiplier) * 100f)}%");
            SetText(detailElementText, entry.element);
            SetText(detailRoleText, entry.description);
            if (legacyDetailStats != null)
            {
                legacyDetailStats.text = string.Empty;
                legacyDetailStats.gameObject.SetActive(false);
            }
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
            CharacterOwnership.Focus(entry.id);
            RefreshAffinity();
        }

        static CharacterDisplayStats CalculateDisplayStats(CharacterEntry entry, int currentLevel)
        {
            var constellation = CharacterProgressionState.GetConstellation(entry.id);
            var statScale = 1f + Mathf.Max(0, currentLevel - 1) * 0.055f + constellation * 0.035f;
            var artifactCount = entry.equippedArtifacts?.Count ?? 0;
            var artifactAttack = artifactCount * 6;
            var artifactHealth = artifactCount * 35;
            var weaponAttack = EquippedWeaponState.Instance != null ? EquippedWeaponState.Instance.GetDisplayAtk(entry.id) : 0;
            var weaponBonus = EquippedWeaponState.Instance != null ? EquippedWeaponState.Instance.GetRuntimeBonus(entry.id) : new WeaponRuntimeBonus();

            return new CharacterDisplayStats
            {
                health = Mathf.Max(1, Mathf.RoundToInt(entry.maxHealth * statScale * (1f + weaponBonus.healthPercent / 100f)) + artifactHealth + Mathf.RoundToInt(weaponBonus.healthFlat)),
                attack = Mathf.Max(1, Mathf.RoundToInt(entry.attack * statScale * (1f + weaponBonus.attackPercent / 100f)) + weaponAttack + artifactAttack + Mathf.RoundToInt(weaponBonus.attackFlat)),
                defense = Mathf.Max(0, Mathf.RoundToInt(entry.defense * statScale * (1f + weaponBonus.defensePercent / 100f)) + Mathf.RoundToInt(artifactAttack * .08f) + Mathf.RoundToInt(weaponBonus.defenseFlat)),
                speed = Mathf.Max(1, entry.speed + Mathf.FloorToInt(Mathf.Max(0, currentLevel - 1) / 20f) + Mathf.RoundToInt(weaponBonus.speedFlat))
            };
        }

        struct CharacterDisplayStats
        {
            public int health;
            public int attack;
            public int defense;
            public int speed;
        }
        void RefreshAffinity()
        {
            if (affinityPage == null || string.IsNullOrEmpty(selectedCharacterId)) return;
            var entry = database?.FindCharacter(selectedCharacterId);
            var name = FindDeep(affinityPage.transform, "AffinityCharacterName")?.GetComponent<TMP_Text>();
            var value = FindDeep(affinityPage.transform, "AffinityValue")?.GetComponent<TMP_Text>();
            var gifts = FindDeep(affinityPage.transform, "AffinityGiftHint")?.GetComponent<TMP_Text>();
            var score = CharacterProgressionState.GetAffinity(selectedCharacterId);
            if (name != null) name.text = entry != null ? entry.displayName : selectedCharacterId;
            if (value != null) value.text = $"Giao cảm {score}/100\n{CharacterProgressionState.GetAffinityDisposition(selectedCharacterId)}";
            if (gifts != null)
                gifts.text = "Dùng vật phẩm giao cảm trong Túi đồ (tab Kỷ niệm) để tăng giao cảm cho nhân vật đang chọn.";
        }

        void EnsureAffinityOverlay()
        {
            // Runtime UI creation is intentionally disabled. Create and assign these objects in the prefab instead:
            // AffinityCharacterName, AffinityValue, AffinityGiftHint, UseAffinityGift.
        }

        void UseAffinityGiftFromBag()
        {
            var inventory = GameManager.Instance?.Inventory;
            if (inventory == null || string.IsNullOrEmpty(selectedCharacterId)) return;
            foreach (var pair in inventory.Items)
            {
                if (pair.Value <= 0) continue;
                var definition = inventory.GetDefinition(pair.Key);
                var isGift = definition != null && (definition.itemType == ItemType.Quest || definition.affinityGain != 0);
                if (!isGift) continue;
                if (CharacterOwnership.TryUseInventoryOnCharacter(pair.Key, selectedCharacterId))
                {
                    RefreshAffinity();
                    RefreshCharacter();
                    return;
                }
            }
            var hint = FindDeep(affinityPage.transform, "AffinityGiftHint")?.GetComponent<TMP_Text>();
            if (hint != null) hint.text = "TÃºi Ä‘á»“ chÆ°a cÃ³ quÃ  giao cáº£m. Nháº­n tá»« Wish, nhiá»‡m vá»¥ hoáº·c kho Ä‘á»“.";
        }

        static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value;
        }

        static TMP_Text FindText(Transform root, params string[] names)
        {
            if (root == null || names == null) return null;
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var found = FindDeep(root, name)?.GetComponent<TMP_Text>();
                if (found != null) return found;
            }
            return null;
        }

        string ResolveOwnedCharacter(string requested) => CharacterOwnership.ResolveOwnedId(requested, database);

        void ShowPage(GameObject page)
        {
            if (galleryPage != null) galleryPage.SetActive(page == galleryPage);
            if (characterPage != null) characterPage.SetActive(page != galleryPage);
            if (detailPage != null) detailPage.SetActive(page == detailPage);
            if (levelPage != null) levelPage.SetActive(page == levelPage);
            if (affinityPage != null)
            {
                affinityPage.SetActive(page == affinityPage);
                if (page == affinityPage) RefreshAffinity();
            }
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
            var characterName = AddText(card.transform, "CharacterName", "TÃªn nhÃ¢n váº­t", new Vector2(.04f, .08f), new Vector2(.96f, .18f), 17);
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
            weaponPageStats = AddText(weaponPageFrame.transform, "WeaponStatsText", "ATK: 0\nHP: 0\nLv. 1 / 80\nTinh luyá»‡n 1", new Vector2(.05f, .40f), new Vector2(.95f, .65f), 24);
            weaponPageStats.alignment = TextAlignmentOptions.TopLeft;

            // 6. Create description text
            weaponPageDesc = AddText(weaponPageFrame.transform, "WeaponDescText", "Weapon description and skill effect", new Vector2(.05f, .12f), new Vector2(.95f, .38f), 20);
            weaponPageDesc.alignment = TextAlignmentOptions.TopLeft;

            // 7. Add buttons: "Thay Ä‘á»•i", "Tinh luyá»‡n", "NÃ¢ng cáº¥p"
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
            AddText(weaponListPage.transform, "Title", "Chá»n VÅ© KhÃ­", new Vector2(.05f, .88f), new Vector2(.95f, .97f), 26).alignment = TextAlignmentOptions.Center;

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
                weaponPageStats.text = $"Táº¥n cÃ´ng: {displayAtk}\nHP tá»‘i Ä‘a: {activeWeapon.baseHp}\nCáº¥p: {lvl} / {activeWeapon.maxLevel}\nTinh luyá»‡n: {refi}";
            }

            if (weaponPageDesc != null)
            {
                weaponPageDesc.text = $"<b>[Thuá»™c tÃ­nh phá»¥]</b> {activeWeapon.subStatName}: +{currentSubStatValue:F1}%\n\n" +
                    $"<b>[Ká»¹ nÄƒng]</b> {WeaponScreenUI.GetWeaponSkillDescription(activeWeapon.weaponId, refi)}";
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

            var equipped = EquippedWeaponState.Instance;
            var itemDb = Resources.Load<Gameplay.ItemDatabase>("Data/ItemDatabase");
            var weaponDb = Resources.Load<WeaponDatabase>("Data/WeaponDatabase");
            if (equipped == null || weaponDb == null) return;

            var slotIndex = 0;
            foreach (var instance in equipped.OwnedWeaponInstances)
            {
                if (instance == null) continue;
                var weaponId = instance.weaponId;
                var weapon = weaponDb.FindExact(weaponId);
                if (weapon == null) continue;
                if (slotIndex >= grid.childCount) break;

                var itemDef = itemDb?.Get(weaponId);
                var slotTransform = grid.GetChild(slotIndex++);
                var slot = slotTransform.gameObject;
                slot.SetActive(true);

                var bgImage = slot.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = weaponSlotFrameSprite != null ? weaponSlotFrameSprite : bgImage.sprite;
                    bgImage.color = Color.white;
                }

                var iconImage = FindDeep(slot.transform, "Icon")?.GetComponent<Image>()
                    ?? FindDeep(slot.transform, "WeaponIcon")?.GetComponent<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = itemDef?.icon;
                    iconImage.preserveAspect = true;
                    iconImage.enabled = iconImage.sprite != null;
                }

                var highlight = FindDeep(slot.transform, "Highlight")?.gameObject;
                if (highlight != null)
                    highlight.SetActive(equipped.EquippedWeaponInstanceId == instance.instanceId);

                var button = slot.GetComponent<Button>() ?? slot.GetComponentInChildren<Button>(true);
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        equipped.EquipInstance(instance.instanceId, selectedCharacterId);
                        weaponListPage.SetActive(false);
                        RefreshWeaponUI();
                        RefreshCharacter();
                        GameManager.Instance?.SaveGame();
                    });
                }
            }

            for (var i = slotIndex; i < grid.childCount; i++)
            {
                var child = grid.GetChild(i);
                if (child != null) child.gameObject.SetActive(false);
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

        static Sprite CharacterFullBodySprite(CharacterEntry entry)
        {
            if (entry == null) return null;
            return entry.fullBody != null ? entry.fullBody :
                   entry.chibi != null ? entry.chibi :
                   entry.portrait;
        }

        static Sprite CharacterChibiSprite(CharacterEntry entry)
        {
            if (entry == null) return null;
            return entry.chibi != null ? entry.chibi :
                   entry.fullBody != null ? entry.fullBody :
                   entry.portrait;
        }
    }
}

