#if UNITY_EDITOR
using System.Collections.Generic;
using BES.UI.Menu;
using BES.Gameplay;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.EditorTools
{
    public static class BattleVictoryAndStoryProgressMigration
    {
        const string PrefabPath = "Assets/_Project/UI/Prefabs/Screens/MenuHub.prefab";
        const string MarkerSpritePath = "Assets/Art Ui/Mới/Play Screen story FIX/Cursho tiến độ.png";

        [MenuItem("BES/UI/Build Battle Win And Story Progress")]
        public static void Apply()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var navigator = root.GetComponent<MenuNavigator>();
                var story = root.GetComponentInChildren<StoryModePanelController>(true);
                var battlePanel = Find(root.transform, "BattlePanel");
                var battle = battlePanel != null ? battlePanel.GetComponent<TurnBattleUI>() : null;
                if (navigator == null || story == null || battle == null) return;

                RenameStoryBackground(root.transform);
                WireStoryProgress(root.transform, story);
                
                var winPanel = BuildWinPanel(battlePanel, out var returnButton);
                
                var losePanel = BuildLosePanel(battlePanel, 
                    out var loseReturnButton, 
                    out var loseRetryButton,
                    out var levelBtn,
                    out var equipBtn,
                    out var skillBtn,
                    out var constellationBtn,
                    out var recruitBtn);

                WireBattle(battle, navigator, story, 
                    winPanel, returnButton, 
                    losePanel, loseReturnButton, loseRetryButton,
                    levelBtn, equipBtn, skillBtn, constellationBtn, recruitBtn);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[BES] Battle WinPanel, LosePanel, Story return and story progress wired successfully.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void RenameStoryBackground(Transform root)
        {
            var background = Find(root, "AssignableChapterBackground");
            if (background != null) background.name = "StoryBackground";
        }

        static void WireStoryProgress(Transform root, StoryModePanelController story)
        {
            var progressBar = Find(root, "ProgressBar");
            if (progressBar == null) return;

            var positions = new List<RectTransform>();
            for (var i = 1; i <= 7; i++)
            {
                var position = FindDirect(progressBar, "Position " + i) as RectTransform;
                if (position != null) positions.Add(position);
            }

            var marker = FindDirect(progressBar, "StoryProgressMarker") as RectTransform;
            if (marker == null)
            {
                var go = new GameObject(
                    "StoryProgressMarker",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.layer = LayerMask.NameToLayer("UI");
                marker = go.GetComponent<RectTransform>();
                marker.SetParent(progressBar, false);
                marker.sizeDelta = new Vector2(72f, 72f);
                var image = go.GetComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MarkerSpritePath);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            marker.SetAsLastSibling();

            var serialized = new SerializedObject(story);
            serialized.FindProperty("storyProgressMarker").objectReferenceValue = marker;
            serialized.FindProperty("storyProgressMoveDuration").floatValue = .65f;
            serialized.FindProperty("storyProgressMoveCurve").animationCurveValue =
                AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            var list = serialized.FindProperty("storyProgressPositions");
            list.arraySize = positions.Count;
            for (var i = 0; i < positions.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = positions[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(story);
        }

        static GameObject BuildWinPanel(
            Transform battlePanel,
            out Button returnButton)
        {
            var existing = FindDirect(battlePanel, "WinPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            // Full screen overlay
            var panel = CreateRect("WinPanel", battlePanel, Vector2.zero, Vector2.one);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Rectangle 40232.png");
            panelImage.raycastTarget = true;

            // Wide horizontal banner (cream + red title CHIẾN THẮNG + glow)
            var strip = CreateFixedRect("WinStrip", panel, new Vector2(1920f, 565f), new Vector2(0f, 60f));
            var stripImage = strip.gameObject.AddComponent<Image>();
            stripImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323086.png");
            stripImage.preserveAspect = true;

            // Reward slots container (placed on the strip)
            var rewardsLayout = CreateFixedRect("RewardsLayout", strip, new Vector2(800f, 200f), new Vector2(0f, -40f));
            var layoutGroup = rewardsLayout.gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 30f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/_Project/Resources/Data/ItemDatabase.asset");
            var greenDef = db?.items.Find(x => x.itemId == "item_exp_green");
            var blueDef = db?.items.Find(x => x.itemId == "item_exp_blue");
            var goldDef = db?.items.Find(x => x.itemId == "item_exp_gold");

            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Story Mode/Money count.png");
            var greenSprite = greenDef?.icon;
            var blueSprite = blueDef?.icon;
            var goldSprite = goldDef?.icon;

            var raritySprites = new Sprite[] {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Túi đồ/Rectangle 40188.png"), // Gold frame for gold
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Túi đồ/Rectangle 40196.png"), // Green frame for green exp
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Túi đồ/Rectangle 40187.png"), // Blue frame for blue exp
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Túi đồ/Rectangle 40188.png")  // Gold frame for gold exp
            };

            var itemSprites = new Sprite[] { coinSprite, greenSprite, blueSprite, goldSprite };
            var itemLabels = new string[] { "1.000", "1", "1", "1" };

            for (var i = 0; i < 4; i++)
            {
                // Slot base (rarity frame)
                var itemSlot = CreateFixedRect("RewardSlot_" + i, rewardsLayout, new Vector2(120f, 122f), Vector2.zero);
                var slotImage = itemSlot.gameObject.AddComponent<Image>();
                if (raritySprites[i] != null)
                {
                    slotImage.sprite = raritySprites[i];
                    slotImage.preserveAspect = true;
                }
                else
                {
                    slotImage.color = new Color(.96f, .92f, .82f, 1f);
                }

                // Item Icon (placed inside the slot, slightly scaled down to fit circular frame)
                var itemIcon = CreateFixedRect("Icon", itemSlot, new Vector2(70f, 70f), new Vector2(0f, 5f));
                var iconImg = itemIcon.gameObject.AddComponent<Image>();
                if (itemSprites[i] != null)
                {
                    iconImg.sprite = itemSprites[i];
                    iconImg.preserveAspect = true;
                }
                else
                {
                    iconImg.color = i == 0 ? Color.yellow : i == 1 ? Color.green : i == 2 ? Color.blue : new Color(.8f, .6f, .2f);
                }

                // Amount Text (placed below the slot frame, exactly like the mockup!)
                var amountRect = CreateFixedRect("Amount", itemSlot, new Vector2(120f, 30f), new Vector2(0f, -80f));
                var amountText = amountRect.gameObject.AddComponent<TextMeshProUGUI>();
                amountText.text = itemLabels[i];
                amountText.fontSize = 24f;
                amountText.fontStyle = FontStyles.Bold;
                amountText.alignment = TextAlignmentOptions.Center;
                amountText.color = new Color(.35f, .16f, .13f, 1f);
            }

            // Buttons: Exit (Thoát) and Continue (Tiếp Tục) placed below the banner
            var exitBtnRect = CreateFixedRect(
                "ExitButton",
                panel,
                new Vector2(284f, 85f),
                new Vector2(-220f, -320f));
            var exitImage = exitBtnRect.gameObject.AddComponent<Image>();
            exitImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 51.png");
            exitImage.preserveAspect = true;
            returnButton = exitBtnRect.gameObject.AddComponent<Button>();
            returnButton.targetGraphic = exitImage;

            var continueBtnRect = CreateFixedRect(
                "ContinueButton",
                panel,
                new Vector2(284f, 85f),
                new Vector2(220f, -320f));
            var continueImage = continueBtnRect.gameObject.AddComponent<Image>();
            continueImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323087.png");
            continueImage.preserveAspect = true;
            var continueButton = continueBtnRect.gameObject.AddComponent<Button>();
            continueButton.targetGraphic = continueImage;

            panel.gameObject.SetActive(false);
            panel.SetAsLastSibling();
            return panel.gameObject;
        }

        static GameObject BuildLosePanel(
            Transform battlePanel,
            out Button exitButton,
            out Button retryButton,
            out Button levelBtn,
            out Button equipBtn,
            out Button skillBtn,
            out Button constellationBtn,
            out Button recruitBtn)
        {
            var existing = FindDirect(battlePanel, "LosePanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            // Full screen overlay
            var panel = CreateRect("LosePanel", battlePanel, Vector2.zero, Vector2.one);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Rectangle 40232.png");
            panelImage.raycastTarget = true;

            // Wide horizontal banner (cream + tilted THUA CUỘC boxes + Cách để mạnh hơn header + glow)
            var strip = CreateFixedRect("LoseStrip", panel, new Vector2(1920f, 528f), new Vector2(0f, 60f));
            var stripImage = strip.gameObject.AddComponent<Image>();
            stripImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323096.png");
            stripImage.preserveAspect = true;

            // 5 buttons container
            var strongPanel = CreateFixedRect("StrongPanel", strip, new Vector2(900f, 200f), new Vector2(0f, -70f));

            var labels = new string[] { "Cấp Nhân Vật", "Trang Bị", "Kỹ Năng", "Tinh Mệnh", "Chiêu Mộ" };
            var iconPaths = new string[] {
                "Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323094.png",
                "Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323095.png",
                "Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323089.png",
                "Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323090.png",
                "Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 427323092.png"
            };

            var btns = new Button[5];
            var xCoords = new float[] { -280f, -140f, 0f, 140f, 280f };

            for (var i = 0; i < 5; i++)
            {
                var btnRect = CreateFixedRect("StrongBtn_" + i, strongPanel, new Vector2(121f, 128f), new Vector2(xCoords[i], 15f));
                var btnImg = btnRect.gameObject.AddComponent<Image>();
                var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);
                if (iconSprite != null)
                {
                    btnImg.sprite = iconSprite;
                    btnImg.preserveAspect = true;
                }
                else
                {
                    btnImg.color = Color.white;
                }

                // Add label text below each button (exactly like mockup!)
                var labelRect = CreateFixedRect("Label", btnRect, new Vector2(140f, 30f), new Vector2(0f, -85f));
                var btnLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                btnLabel.text = labels[i];
                btnLabel.fontSize = 18f;
                btnLabel.fontStyle = FontStyles.Bold;
                btnLabel.alignment = TextAlignmentOptions.Center;
                btnLabel.color = Color.white;

                btns[i] = btnRect.gameObject.AddComponent<Button>();
                btns[i].targetGraphic = btnImg;
            }

            levelBtn = btns[0];
            equipBtn = btns[1];
            skillBtn = btns[2];
            constellationBtn = btns[3];
            recruitBtn = btns[4];

            // Action Buttons: Exit (Thoát) and Retry (Chơi Lại)
            var exitBtnRect = CreateFixedRect(
                "ExitButton",
                panel,
                new Vector2(284f, 85f),
                new Vector2(-220f, -320f));
            var exitImage = exitBtnRect.gameObject.AddComponent<Image>();
            exitImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 51.png");
            exitImage.preserveAspect = true;
            exitButton = exitBtnRect.gameObject.AddComponent<Button>();
            exitButton.targetGraphic = exitImage;

            var retryBtnRect = CreateFixedRect(
                "RetryButton",
                panel,
                new Vector2(284f, 85f),
                new Vector2(220f, -320f));
            var retryImage = retryBtnRect.gameObject.AddComponent<Image>();
            retryImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art Ui/Game Việt hóa mới/Thông báo/Group 4273230874456.png");
            retryImage.preserveAspect = true;
            retryButton = retryBtnRect.gameObject.AddComponent<Button>();
            retryButton.targetGraphic = retryImage;

            panel.gameObject.SetActive(false);
            panel.SetAsLastSibling();
            return panel.gameObject;
        }

        static void WireBattle(
            TurnBattleUI battle,
            MenuNavigator navigator,
            StoryModePanelController story,
            GameObject winPanel,
            Button returnButton,
            GameObject losePanel,
            Button loseReturnButton,
            Button loseRetryButton,
            Button levelBtn,
            Button equipBtn,
            Button skillBtn,
            Button constellationBtn,
            Button recruitBtn)
        {
            var serialized = new SerializedObject(battle);
            serialized.FindProperty("winPanel").objectReferenceValue = winPanel;
            serialized.FindProperty("winReturnButton").objectReferenceValue = returnButton;
            serialized.FindProperty("losePanel").objectReferenceValue = losePanel;
            serialized.FindProperty("loseReturnButton").objectReferenceValue = loseReturnButton;
            serialized.FindProperty("loseRetryButton").objectReferenceValue = loseRetryButton;
            serialized.FindProperty("levelBtn").objectReferenceValue = levelBtn;
            serialized.FindProperty("equipBtn").objectReferenceValue = equipBtn;
            serialized.FindProperty("skillBtn").objectReferenceValue = skillBtn;
            serialized.FindProperty("constellationBtn").objectReferenceValue = constellationBtn;
            serialized.FindProperty("recruitBtn").objectReferenceValue = recruitBtn;
            serialized.FindProperty("navigator").objectReferenceValue = navigator;
            serialized.FindProperty("storyModeController").objectReferenceValue = story;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static RectTransform CreateFixedRect(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        static TMP_Text CreateText(
            string name,
            Transform parent,
            string value,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float size)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            return text;
        }

        static Transform FindDirect(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
                if (child.name == name) return child;
            return null;
        }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }
    }
}
#endif
