#if UNITY_EDITOR
using BES.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BES.Editor
{
    public static class BESUIAtomBuilder
    {
        public static void BuildAll()
        {
            BuildSimpleAtom("UIButtonPrimary", new Color(0.95f, 0.78f, 0.28f, 0.95f));
            BuildSimpleAtom("UIButtonGhost", new Color(0.15f, 0.13f, 0.22f, 0.7f));
            BuildSimpleAtom("UICloseButton", new Color(0.2f, 0.18f, 0.28f, 0.9f));
            BuildSimpleAtom("UIItemSlot", new Color(0.32f, 0.68f, 0.95f, 0.85f));
            BuildSimpleAtom("UIPortraitSlot", new Color(0.2f, 0.2f, 0.3f, 0.9f));
            BuildSimpleAtom("UICurrencyPill", new Color(0.12f, 0.11f, 0.18f, 0.95f));
            BuildDayCheckInSlot();
            BuildTeamSlot();
            BuildServerOption();
            BuildSettingsRow();
            BuildMapMarker();
            BuildWeaponSlot();
            BuildResultCard();
            BuildGachaCard();
            BuildQuestCard();
            BuildQuestRewardItem();
        }

        static void BuildSimpleAtom(string name, Color color)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(120, 40);
            go.AddComponent<Image>().color = color;
            if (name.Contains("Button") || name.Contains("Close"))
                go.AddComponent<Button>();
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/" + name + ".prefab");
        }

        static void BuildDayCheckInSlot()
        {
            var go = new GameObject("UIDayCheckInSlot");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(88, 96);
            var frame = go.AddComponent<Image>();
            frame.color = new Color(0.2f, 0.18f, 0.28f, 0.95f);
            go.AddComponent<Button>();
            var day = BESUIEditorUtils.CreateText(go.transform, "DayLabel", "Day 1", new Vector2(0, 20), 14f);
            var reward = BESUIEditorUtils.CreateText(go.transform, "RewardLabel", "+60", new Vector2(0, -20), 12f);
            var slot = go.AddComponent<UIDayCheckInSlot>();
            BESUIEditorUtils.SetPrivateField(slot, "dayLabel", day);
            BESUIEditorUtils.SetPrivateField(slot, "rewardLabel", reward);
            BESUIEditorUtils.SetPrivateField(slot, "frame", frame);
            BESUIEditorUtils.SetPrivateField(slot, "button", go.GetComponent<Button>());
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIDayCheckInSlot.prefab");
        }

        static void BuildTeamSlot()
        {
            var go = new GameObject("UITeamSlot");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 140);
            var frame = go.AddComponent<Image>();
            frame.color = new Color(0.18f, 0.16f, 0.24f, 0.95f);
            go.AddComponent<Button>();
            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(go.transform, false);
            var pRect = portraitGo.AddComponent<RectTransform>();
            pRect.sizeDelta = new Vector2(72, 72);
            pRect.anchoredPosition = new Vector2(0, 16);
            var portrait = portraitGo.AddComponent<Image>();
            portrait.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);
            var name = BESUIEditorUtils.CreateText(go.transform, "NameLabel", "Character", new Vector2(0, -48), 12f);
            var slot = go.AddComponent<UITeamSlot>();
            BESUIEditorUtils.SetPrivateField(slot, "portrait", portrait);
            BESUIEditorUtils.SetPrivateField(slot, "nameLabel", name);
            BESUIEditorUtils.SetPrivateField(slot, "button", go.GetComponent<Button>());
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UITeamSlot.prefab");
        }

        static void BuildServerOption()
        {
            var go = new GameObject("UIServerOption");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(320, 44);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.11f, 0.18f, 0.95f);
            go.AddComponent<Button>();
            var highlightGo = new GameObject("Highlight");
            highlightGo.transform.SetParent(go.transform, false);
            var hRect = highlightGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(hRect);
            var highlight = highlightGo.AddComponent<Image>();
            highlight.color = new Color(0.95f, 0.78f, 0.28f, 0.25f);
            highlight.enabled = false;
            var name = BESUIEditorUtils.CreateText(go.transform, "ServerName", "Asian", Vector2.zero, 16f);
            var opt = go.AddComponent<UIServerOption>();
            BESUIEditorUtils.SetPrivateField(opt, "serverName", name);
            BESUIEditorUtils.SetPrivateField(opt, "button", go.GetComponent<Button>());
            BESUIEditorUtils.SetPrivateField(opt, "highlight", highlight);
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIServerOption.prefab");
        }

        static void BuildSettingsRow()
        {
            var go = new GameObject("UISettingsRow");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(400, 40);
            var label = BESUIEditorUtils.CreateText(go.transform, "Label", "Setting", new Vector2(-120, 0), 16f, TextAlignmentOptions.Left);
            var toggleGo = new GameObject("Toggle");
            toggleGo.transform.SetParent(go.transform, false);
            toggleGo.AddComponent<RectTransform>().anchoredPosition = new Vector2(140, 0);
            toggleGo.AddComponent<Toggle>();
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(go.transform, false);
            var sRect = sliderGo.AddComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(200, 20);
            sRect.anchoredPosition = new Vector2(80, 0);
            sliderGo.AddComponent<Slider>();
            sliderGo.SetActive(false);
            var row = go.AddComponent<UISettingsRow>();
            BESUIEditorUtils.SetPrivateField(row, "label", label);
            BESUIEditorUtils.SetPrivateField(row, "toggle", toggleGo.GetComponent<Toggle>());
            BESUIEditorUtils.SetPrivateField(row, "slider", sliderGo.GetComponent<Slider>());
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UISettingsRow.prefab");
        }

        static void BuildMapMarker()
        {
            var go = new GameObject("UIMapMarker");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(140, 48);
            go.AddComponent<Button>();
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            iconGo.AddComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
            var icon = iconGo.AddComponent<Image>();
            icon.color = Color.white;
            var label = BESUIEditorUtils.CreateText(go.transform, "RegionLabel", "Region", new Vector2(20, 0), 14f, TextAlignmentOptions.Left);
            var marker = go.AddComponent<UIMapMarker>();
            BESUIEditorUtils.SetPrivateField(marker, "regionLabel", label);
            BESUIEditorUtils.SetPrivateField(marker, "markerIcon", icon);
            BESUIEditorUtils.SetPrivateField(marker, "button", go.GetComponent<Button>());
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIMapMarker.prefab");
        }

        static void BuildWeaponSlot()
        {
            var go = new GameObject("UIWeaponSlot");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
            var frame = go.AddComponent<Image>();
            frame.color = new Color(0.2f, 0.18f, 0.28f, 0.95f);
            go.AddComponent<Button>();
            var selGo = new GameObject("Selection");
            selGo.transform.SetParent(go.transform, false);
            var sRect = selGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(sRect);
            var sel = selGo.AddComponent<Image>();
            sel.color = new Color(0.95f, 0.78f, 0.28f, 0.4f);
            sel.enabled = false;
            var label = BESUIEditorUtils.CreateText(go.transform, "Label", "Wpn", Vector2.zero, 10f);
            var slot = go.AddComponent<UIWeaponSlot>();
            BESUIEditorUtils.SetPrivateField(slot, "icon", frame);
            BESUIEditorUtils.SetPrivateField(slot, "label", label);
            BESUIEditorUtils.SetPrivateField(slot, "button", go.GetComponent<Button>());
            BESUIEditorUtils.SetPrivateField(slot, "selectionFrame", sel);
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIWeaponSlot.prefab");
        }

        static void BuildResultCard()
        {
            var go = new GameObject("UIResultCard");
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(64, 96);
            go.AddComponent<Image>().color = new Color(0.2f, 0.18f, 0.28f, 0.95f);
            BESUIEditorUtils.CreateText(go.transform, "Label", "4★", Vector2.zero, 12f);
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIResultCard.prefab");
        }
        static void BuildGachaCard()
        {
            var go = new GameObject("UIGachaCard");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(108, 180);
            var card = go.AddComponent<GachaCardUI>();

            var glowGo = new GameObject("RarityGlow");
            glowGo.transform.SetParent(go.transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(glowRect);
            glowRect.offsetMin = new Vector2(-14f, -18f);
            glowRect.offsetMax = new Vector2(14f, 18f);
            var glow = glowGo.AddComponent<Image>();
            glow.color = new Color(1f, 0.75f, 0.18f, 0.65f);
            glow.raycastTarget = false;

            var artGo = new GameObject("Artwork");
            artGo.transform.SetParent(go.transform, false);
            var artRect = artGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(artRect);
            artRect.offsetMin = new Vector2(8f, 10f);
            artRect.offsetMax = new Vector2(-8f, -10f);
            var artwork = artGo.AddComponent<RawImage>();
            artwork.color = Color.white;
            artwork.raycastTarget = false;

            var frameGo = new GameObject("Frame");
            frameGo.transform.SetParent(go.transform, false);
            var frameRect = frameGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(frameRect);
            var frame = frameGo.AddComponent<Image>();
            frame.color = new Color(1f, 0.75f, 0.18f, 0.95f);
            frame.raycastTarget = true;

            var name = BESUIEditorUtils.CreateText(go.transform, "Name", "Reward", new Vector2(0f, -68f), 12f);
            name.rectTransform.sizeDelta = new Vector2(130f, 28f);
            var rarity = BESUIEditorUtils.CreateText(go.transform, "Rarity", "4 Star", new Vector2(0f, 70f), 11f);
            rarity.rectTransform.sizeDelta = new Vector2(120f, 22f);

            var infoGo = new GameObject("HiddenInfo");
            infoGo.transform.SetParent(go.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            UIAnchorPresets.BottomCenter(infoRect, new Vector2(150f, 58f), new Vector2(0f, -44f));
            infoGo.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.92f);
            var detail = BESUIEditorUtils.CreateText(infoGo.transform, "Detail", "Reward detail", Vector2.zero, 10f);
            UIAnchorPresets.StretchFull(detail.rectTransform);
            detail.rectTransform.offsetMin = new Vector2(8f, 4f);
            detail.rectTransform.offsetMax = new Vector2(-8f, -4f);
            detail.alignment = TextAlignmentOptions.Center;
            infoGo.SetActive(false);

            BESUIEditorUtils.SetPrivateField(card, "cardRoot", rect);
            BESUIEditorUtils.SetPrivateField(card, "artworkImage", artwork);
            BESUIEditorUtils.SetPrivateField(card, "rarityGlow", glow);
            BESUIEditorUtils.SetPrivateField(card, "frameImage", frame);
            BESUIEditorUtils.SetPrivateField(card, "nameText", name);
            BESUIEditorUtils.SetPrivateField(card, "rarityText", rarity);
            BESUIEditorUtils.SetPrivateField(card, "detailText", detail);
            BESUIEditorUtils.SetPrivateField(card, "hiddenInfoRoot", infoGo);
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIGachaCard.prefab");
        }

        static void BuildQuestCard()
        {
            var go = new GameObject("UIQuestCard");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500f, 72f);
            var card = go.AddComponent<QuestCardUI>();

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.2f, 0.36f, 0.82f);
            bg.raycastTarget = true;

            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(go.transform, false);
            var borderRect = borderGo.AddComponent<RectTransform>();
            UIAnchorPresets.StretchFull(borderRect);
            var border = borderGo.AddComponent<Image>();
            border.color = new Color(1f, 0.84f, 0.25f, 0.95f);
            border.raycastTarget = false;
            border.enabled = false;

            var title = BESUIEditorUtils.CreateText(go.transform, "Title", "Name of quest", new Vector2(14f, 14f), 15f, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            title.rectTransform.pivot = new Vector2(0f, 0.5f);
            title.rectTransform.sizeDelta = new Vector2(-28f, 26f);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.9f, 0.25f, 0.95f);

            var info = BESUIEditorUtils.CreateText(go.transform, "Info", "Quest info", new Vector2(14f, -14f), 12f, TextAlignmentOptions.Left);
            info.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            info.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            info.rectTransform.pivot = new Vector2(0f, 0.5f);
            info.rectTransform.sizeDelta = new Vector2(-28f, 24f);
            info.color = new Color(1f, 1f, 1f, 0.82f);

            BESUIEditorUtils.SetPrivateField(card, "background", bg);
            BESUIEditorUtils.SetPrivateField(card, "border", border);
            BESUIEditorUtils.SetPrivateField(card, "titleText", title);
            BESUIEditorUtils.SetPrivateField(card, "infoText", info);
            BESUIEditorUtils.SetPrivateField(card, "hoverScale", 1.05f);
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIQuestCard.prefab");
        }

        static void BuildQuestRewardItem()
        {
            var go = new GameObject("UIQuestRewardItem");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(88f, 112f);
            var reward = go.AddComponent<QuestRewardItemUI>();

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.45f, 0.65f, 1f, 0.35f);
            bg.raycastTarget = false;

            var itemGo = new GameObject("ItemImage");
            itemGo.transform.SetParent(go.transform, false);
            var itemRect = itemGo.AddComponent<RectTransform>();
            UIAnchorPresets.Center(itemRect, new Vector2(58f, 58f));
            itemRect.anchoredPosition = new Vector2(0f, 18f);
            var item = itemGo.AddComponent<RawImage>();
            item.texture = null;
            item.color = new Color(1f, 1f, 1f, 0.12f);
            item.raycastTarget = false;

            var starGo = new GameObject("Star");
            starGo.transform.SetParent(go.transform, false);
            var starRect = starGo.AddComponent<RectTransform>();
            UIAnchorPresets.BottomCenter(starRect, new Vector2(58f, 14f), new Vector2(0f, 22f));
            var star = starGo.AddComponent<Image>();
            star.color = new Color(1f, 0.75f, 0.18f, 0.95f);
            star.raycastTarget = false;

            var name = BESUIEditorUtils.CreateText(go.transform, "Name", "Item", new Vector2(0f, -42f), 10f, TextAlignmentOptions.Center);
            name.rectTransform.sizeDelta = new Vector2(84f, 22f);
            name.color = Color.white;

            BESUIEditorUtils.SetPrivateField(reward, "background", bg);
            BESUIEditorUtils.SetPrivateField(reward, "star", star);
            BESUIEditorUtils.SetPrivateField(reward, "itemImage", item);
            BESUIEditorUtils.SetPrivateField(reward, "itemNameText", name);
            BESUIEditorUtils.SavePrefab(go, UIAssetPaths.AtomPrefabs + "/UIQuestRewardItem.prefab");
        }
    }
}
#endif
