using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BES.UI
{
    [System.Serializable]
    public class CashShopSpriteEntry
    {
        public string name;
        public Sprite sprite;
    }

    public class CashShopUI : MonoBehaviour
    {
        const string AssetRoot = "Assets/Art Ui/Moi/Cash SHop/";
        const string UnicodeAssetRoot = "Assets/Art Ui/Mới/Cash SHop/";

        [SerializeField] GameObject panel;
        [SerializeField] Button closeButton;
        [SerializeField] Sprite closeSprite;
        [SerializeField] bool closeOnEscape = true;
        [SerializeField] bool hideLegacyChildren = true;
        [SerializeField] CashShopTab startTab = CashShopTab.DiamondPurchase;
        [SerializeField] List<CashShopSpriteEntry> spriteLibrary = new();

        CashShopTab currentTab;
        bool legacyChildrenHidden;
        TMP_Text titleText;
        Image shopBackground;
        RectTransform contentRoot;
        readonly List<GameObject> runtimeItems = new();

        public bool IsOpen => panel != null && panel.activeSelf;

        void Awake()
        {
            currentTab = startTab;
            EnsureRuntimeBindings();
            if (panel != null)
                panel.SetActive(false);
        }

        void Update()
        {
            if (closeOnEscape && IsOpen && UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                Close();
        }

        public void Open()
        {
            EnsureRuntimeBindings();
            if (panel == null)
                return;

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Refresh();
        }

        public void OpenTab(CashShopTab tab)
        {
            currentTab = tab;
            Open();
        }

        public void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public void ShowTab(CashShopTab tab)
        {
            currentTab = tab;
            Refresh();
        }

        void EnsureRuntimeBindings()
        {
            if (panel == null)
                panel = gameObject;

            var root = panel.transform;
            ApplyRect(panel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            EnsureTopCanvas(panel);
            ApplyPanelBlocker(panel);
            EnsureShopBackground(root);
            HideLegacyChildren(root);

            titleText ??= CreateText("RuntimeShopTitle", root, "", 35f, TextAlignmentOptions.TopLeft);
            ApplyRect(titleText.rectTransform, new Vector2(0.01f, 0.90f), new Vector2(0.48f, 0.99f));
            titleText.color = Color.white;

            if (contentRoot == null)
                contentRoot = CreateRect("RuntimeShopContent", root);
            ApplyRect(contentRoot, new Vector2(0.20f, 0.16f), new Vector2(0.94f, 0.78f));

            CreateHeaderCurrency(root, "RuntimeEnergyCurrency", "Group 427322941", 0.40f, () => ShowTab(CashShopTab.LightPurchase));
            CreateHeaderCurrency(root, "RuntimeSolarCurrency", "Group 427322942", 0.52f, () => ShowTab(CashShopTab.SolarPeaExchange));
            CreateHeaderCurrency(root, "RuntimeGemCurrency", "Group 44", 0.64f, () => ShowTab(CashShopTab.DiamondPurchase));
            CreateHeaderCurrency(root, "RuntimeCoinCurrency", "Group 42", 0.76f, () => ShowTab(CashShopTab.GoldenExchange));

            CreateSideTab(root, "RuntimeExchangeTab", "Group 427322742", new Vector2(0.035f, 0.56f), new Vector2(0.19f, 0.63f), () => ShowTab(CashShopTab.GoldenExchange));
            CreateSideTab(root, "RuntimePackTab", "Group 427322916 ( 1 )", new Vector2(0.035f, 0.47f), new Vector2(0.19f, 0.54f), () => ShowTab(CashShopTab.PackShop));
            CreateSideTab(root, "RuntimeLightTab", "Group 427322944 (1)", new Vector2(0.035f, 0.38f), new Vector2(0.19f, 0.45f), () => ShowTab(CashShopTab.LightPurchase));
            CreateSideTab(root, "RuntimeDiamondTab", "Group 4273229451", new Vector2(0.035f, 0.29f), new Vector2(0.19f, 0.36f), () => ShowTab(CashShopTab.DiamondPurchase));

            CreateSubTab(root, "RuntimeSolarSubTab", "Group 427323065", new Vector2(0.28f, 0.77f), new Vector2(0.43f, 0.84f), () => ShowTab(CashShopTab.SolarPeaExchange));
            CreateSubTab(root, "RuntimeTidalSubTab", "Group 427323066", new Vector2(0.46f, 0.77f), new Vector2(0.61f, 0.84f), () => ShowTab(CashShopTab.TidalPeaExchange));
            CreateSubTab(root, "RuntimeGoldenSubTab", "Group 427323067", new Vector2(0.64f, 0.77f), new Vector2(0.79f, 0.84f), () => ShowTab(CashShopTab.GoldenExchange));

            if (!IsRuntimeObject(closeButton))
            {
                if (closeButton != null)
                    closeButton.gameObject.SetActive(false);
                closeButton = CreateCloseButton("RuntimeShopCloseButton", root, Close);
            }
            ApplyRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.94f, 0.82f), new Vector2(0.985f, 0.94f));
            closeButton.transform.SetAsLastSibling();
        }

        void Refresh()
        {
            EnsureRuntimeBindings();
            if (titleText != null)
                titleText.text = CashShopCatalog.TitleFor(currentTab);

            for (var i = runtimeItems.Count - 1; i >= 0; i--)
                Destroy(runtimeItems[i]);
            runtimeItems.Clear();

            var products = CashShopCatalog.ProductsFor(currentTab);
            var columns = currentTab == CashShopTab.TidalPeaExchange ? 4 : 4;
            var width = currentTab == CashShopTab.SolarPeaExchange ? 0.23f : 0.20f;
            var height = 0.31f;
            var startX = currentTab == CashShopTab.SolarPeaExchange ? 0.06f : 0.02f;
            var gapX = currentTab == CashShopTab.SolarPeaExchange ? 0.27f : 0.24f;
            var gapY = 0.38f;

            for (var i = 0; i < products.Count; i++)
            {
                var row = i / columns;
                var col = i % columns;
                var min = new Vector2(startX + col * gapX, 0.58f - row * gapY);
                var max = new Vector2(min.x + width, min.y + height);
                CreateProductCard(products[i], min, max);
            }
        }

        void CreateProductCard(CashShopProduct product, Vector2 min, Vector2 max)
        {
            var go = new GameObject("RuntimeProduct_" + product.id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(contentRoot, false);
            runtimeItems.Add(go);
            ApplyRect(go.GetComponent<RectTransform>(), min, max);
            var image = go.GetComponent<Image>();
            image.sprite = LoadSprite(product.spriteName);
            image.color = image.sprite == null ? new Color(1f, 0.86f, 0.15f, 0.95f) : Color.white;
            image.preserveAspect = true;
        }

        static void ApplyPanelBlocker(GameObject targetPanel)
        {
            var image = targetPanel.GetComponent<Image>();
            if (image == null)
                image = targetPanel.AddComponent<Image>();

            if (image == null)
                return;

            image.sprite = null;
            image.color = new Color(0.04f, 0.03f, 0.03f, 1f);
            image.raycastTarget = true;
        }

        void EnsureShopBackground(Transform root)
        {
            if (shopBackground == null)
            {
                var go = new GameObject("RuntimeShopBackground", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                shopBackground = go.GetComponent<Image>();
                shopBackground.raycastTarget = false;
            }

            shopBackground.sprite = LoadSprite("Group 427322940");
            shopBackground.color = shopBackground.sprite == null ? new Color(0.08f, 0.06f, 0.05f, 1f) : Color.white;
            shopBackground.preserveAspect = false;
            ApplyRect(shopBackground.rectTransform, Vector2.zero, Vector2.one);
            shopBackground.transform.SetAsFirstSibling();
        }

        static void EnsureTopCanvas(GameObject targetPanel)
        {
            var canvas = targetPanel.GetComponent<Canvas>();
            if (canvas == null)
                canvas = targetPanel.AddComponent<Canvas>();

            if (canvas == null)
                return;

            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

            if (targetPanel.GetComponent<GraphicRaycaster>() == null)
                targetPanel.AddComponent<GraphicRaycaster>();
        }

        void CreateHeaderCurrency(Transform root, string name, string spriteName, float x, UnityEngine.Events.UnityAction action)
        {
            if (root.Find(name) != null)
                return;
            var go = CreateImageButton(name, root, spriteName, action);
            ApplyRect(go.GetComponent<RectTransform>(), new Vector2(x, 0.86f), new Vector2(x + 0.11f, 0.92f));
        }

        void CreateSideTab(Transform root, string name, string spriteName, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            if (root.Find(name) != null)
                return;
            var go = CreateImageButton(name, root, spriteName, action);
            ApplyRect(go.GetComponent<RectTransform>(), min, max);
        }

        void CreateSubTab(Transform root, string name, string spriteName, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            if (root.Find(name) != null)
                return;
            var go = CreateImageButton(name, root, spriteName, action);
            ApplyRect(go.GetComponent<RectTransform>(), min, max);
        }

        GameObject CreateImageButton(string name, Transform root, string spriteName, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            var image = go.GetComponent<Image>();
            image.sprite = LoadSprite(spriteName);
            image.color = image.sprite == null ? new Color(1f, 1f, 1f, 0.01f) : Color.white;
            image.preserveAspect = true;
            var button = go.GetComponent<Button>();
            if (action != null)
                button.onClick.AddListener(action);
            return go;
        }

        void HideLegacyChildren(Transform root)
        {
            if (!hideLegacyChildren || legacyChildrenHidden || root == null)
                return;

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!child.name.StartsWith("Runtime", System.StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }

            legacyChildrenHidden = true;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        Button CreateCloseButton(string name, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = closeSprite;
            image.color = closeSprite == null ? new Color(1f, 1f, 1f, 0.01f) : Color.white;
            image.preserveAspect = true;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
            return button;
        }

        Sprite LoadSprite(string spriteName)
        {
            for (var i = 0; i < spriteLibrary.Count; i++)
            {
                var entry = spriteLibrary[i];
                if (entry != null && entry.name == spriteName && entry.sprite != null)
                    return entry.sprite;
            }

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(UnicodeAssetRoot + spriteName + ".png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>(AssetRoot + spriteName + ".png");
#else
            return null;
#endif
        }

        static bool IsRuntimeObject(Component component)
        {
            return component != null && component.gameObject.name.StartsWith("Runtime", System.StringComparison.Ordinal);
        }

        static void ApplyRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
