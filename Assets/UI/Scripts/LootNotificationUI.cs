using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using BES.Gameplay;

namespace BES.UI
{
    public class LootNotificationUI : MonoBehaviour
    {
        private static GameObject canvasInstance;
        private static RectTransform containerRect;

        // Phương thức tĩnh hiển thị thông báo thu thập vật phẩm
        public static void Show(string itemId, int amount)
        {
            EnsureCanvasCreated();

            ItemDatabase db = Resources.Load<ItemDatabase>("Data/ItemDatabase");
            string displayName = itemId;
            Sprite iconSprite = null;
            int rarity = 1;

            if (db != null)
            {
                ItemDefinition def = db.Get(itemId);
                if (def != null)
                {
                    displayName = def.displayName;
                    iconSprite = def.icon;
                    rarity = def.rarity;
                }
            }

            // Dịch thuật thông minh & Gán icon tương ứng cho các vật phẩm rương (không có sẵn trong db)
            if (itemId == "item_exp_gold")
            {
                displayName = "Lọ EXP Vàng";
                rarity = 5; // Legendary (Vàng kim)
                if (db != null) iconSprite = db.Get("wish_relic_5")?.icon; // Lấy icon Relic Vàng kim cực đẹp làm hình hiển thị
            }
            else if (itemId == "potion_heal")
            {
                displayName = "Bình Hồi Máu";
                rarity = 2; // Green (Uncommon)
                if (db != null) iconSprite = db.Get("wish_material_3")?.icon; // Lấy icon Wish Fragment làm bình hồi máu
            }

            // Tạo đối tượng hiển thị thông báo
            GameObject itemGo = new GameObject("LootNotificationItem");
            itemGo.transform.SetParent(containerRect, false);
            
            var notificationItem = itemGo.AddComponent<LootNotificationItem>();
            notificationItem.Setup(displayName, amount, iconSprite, rarity);
        }

        private static void EnsureCanvasCreated()
        {
            if (canvasInstance != null) return;

            canvasInstance = GameObject.Find("LootNotificationCanvas");
            if (canvasInstance != null)
            {
                containerRect = canvasInstance.transform.Find("Container") as RectTransform;
                return;
            }

            canvasInstance = new GameObject("LootNotificationCanvas");
            var canvas = canvasInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasInstance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasInstance.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasInstance);

            GameObject containerGo = new GameObject("Container");
            containerGo.transform.SetParent(canvasInstance.transform, false);
            
            containerRect = containerGo.AddComponent<RectTransform>();
            // Thiết lập Panel Container nằm ở khoảng 55% - 78% chiều rộng màn hình (ngay bên trái danh sách đội hình)
            // Tránh tình trạng đè chồng lên thẻ nhân vật ở cạnh phải màn hình
            containerRect.anchorMin = new Vector2(0.52f, 0.15f);
            containerRect.anchorMax = new Vector2(0.78f, 0.85f);
            containerRect.pivot = new Vector2(1f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;

            var layout = containerGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
        }
    }

    public class LootNotificationItem : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private RectTransform visualPanelRect;

        public void Setup(string itemName, int amount, Sprite iconSprite, int rarity)
        {
            // 1. Parent (LootNotificationItem) có kích thước cố định để VerticalLayoutGroup xếp chồng chuẩn xác
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(360f, 56f);

            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // 2. VisualPanel: Chứa toàn bộ hình ảnh hiển thị, là đối tượng được chạy hiệu ứng Slide-in
            GameObject panelGo = new GameObject("VisualPanel");
            panelGo.transform.SetParent(transform, false);
            visualPanelRect = panelGo.AddComponent<RectTransform>();
            visualPanelRect.anchorMin = Vector2.zero;
            visualPanelRect.anchorMax = Vector2.one;
            visualPanelRect.sizeDelta = Vector2.zero;

            // A. Ảnh nền tối mờ
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(panelGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.06f, 0.06f, 0.06f, 0.88f); // Màu đen tuyền xịn

            // B. Thanh viền màu Rarity bên trái (chuẩn thiết kế Destiny/Genshin)
            GameObject rarityBorder = new GameObject("RarityBorder");
            rarityBorder.transform.SetParent(panelGo.transform, false);
            var borderRect = rarityBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.pivot = new Vector2(0f, 0.5f);
            borderRect.anchoredPosition = Vector3.zero;
            borderRect.sizeDelta = new Vector2(5f, 0f);

            var borderImage = rarityBorder.AddComponent<Image>();
            borderImage.color = GetRarityColor(rarity);

            // C. Icon 2D của vật phẩm
            if (iconSprite != null)
            {
                GameObject iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(panelGo.transform, false);
                var iconRect = iconGo.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.04f, 0.5f);
                iconRect.anchorMax = new Vector2(0.04f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = Vector3.zero;
                iconRect.sizeDelta = new Vector2(40f, 40f);

                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;
            }

            // D. Chữ hiển thị
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.18f, 0f);
            textRect.anchorMax = new Vector2(0.96f, 1f);
            textRect.sizeDelta = Vector2.zero;

            var txt = textGo.AddComponent<TextMeshProUGUI>();
            txt.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            txt.fontSize = 17f;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            
            string hexColor = ColorUtility.ToHtmlStringRGB(GetRarityColor(rarity));
            txt.text = $"Nhận được: <color=#{hexColor}>{itemName}</color> <color=#FFD700>x{amount}</color>";

            // 3. Khởi chạy Coroutine hiệu ứng chuyển động của riêng VisualPanel
            StartCoroutine(NotificationAnimationRoutine());
        }

        private IEnumerator NotificationAnimationRoutine()
        {
            // A. Slide-in & Fade-in của VisualPanel theo trục X (trục Y giữ nguyên hoàn hảo do VerticalLayoutGroup quản lý ở cha)
            float elapsed = 0f;
            float duration = 0.25f;
            Vector3 targetPos = Vector3.zero;
            Vector3 startPos = new Vector3(180f, 0f, 0f); // Trượt từ phải qua 180px
            visualPanelRect.localPosition = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.Sin(t * Mathf.PI * 0.5f); // Smooth step
                visualPanelRect.localPosition = Vector3.Lerp(startPos, targetPos, t);
                canvasGroup.alpha = t;
                yield return null;
            }

            visualPanelRect.localPosition = targetPos;
            canvasGroup.alpha = 1f;

            // B. Hiển thị tĩnh trong 2.4 giây
            yield return new WaitForSeconds(2.4f);

            // C. Fade-out
            elapsed = 0f;
            duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            // Hủy đối tượng
            Destroy(gameObject);
        }

        private Color GetRarityColor(int rarity)
        {
            switch (rarity)
            {
                case 1: return new Color(0.75f, 0.75f, 0.75f); // Trắng
                case 2: return new Color(0.12f, 0.75f, 0.12f); // Xanh lá
                case 3: return new Color(0.1f, 0.5f, 0.95f);  // Xanh dương
                case 4: return new Color(0.6f, 0.15f, 0.95f); // Tím
                case 5: return new Color(1f, 0.65f, 0f);      // Vàng kim (Legendary)
                default: return Color.white;
            }
        }
    }
}
