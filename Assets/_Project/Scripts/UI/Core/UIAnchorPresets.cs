using UnityEngine;

namespace BES.UI
{
    public static class UIAnchorPresets
    {
        public const float RefWidth = 1920f;
        public const float RefHeight = 1080f;

        public static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void TopLeft(RectTransform rect, Vector2 size, Vector2 anchoredPos)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        public static void TopRight(RectTransform rect, Vector2 size, Vector2 anchoredPos)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        public static void BottomCenter(RectTransform rect, Vector2 size, Vector2 anchoredPos)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        public static void BottomRight(RectTransform rect, Vector2 size, Vector2 insetFromCorner)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(-insetFromCorner.x, insetFromCorner.y);
        }

        public static void BottomLeft(RectTransform rect, Vector2 size, Vector2 insetFromCorner)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = insetFromCorner;
        }

        public static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        public static void RightCenter(RectTransform rect, Vector2 size, Vector2 anchoredPos)
        {
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        // Main play.png @ 1920×1080 — delegates to HUDLayoutTokens
        public static void ApplyMiniMapRegion(RectTransform rect) => TopLeft(rect, HUDLayoutTokens.MiniMapSize, HUDLayoutTokens.MiniMapPos);
        public static void ApplyQuestTrackerRegion(RectTransform rect) => TopLeft(rect, HUDLayoutTokens.QuestTrackerSize, HUDLayoutTokens.QuestTrackerPos);
        public static void ApplyPortraitChipRegion(RectTransform rect) => TopLeft(rect, HUDLayoutTokens.PortraitChipSize, HUDLayoutTokens.PortraitChipPos);
        public static void ApplyLockBtnRegion(RectTransform rect) => TopLeft(rect, HUDLayoutTokens.LockBtnSize, HUDLayoutTokens.LockBtnPos);
        public static void ApplyPartyStripRegion(RectTransform rect) => RightCenter(rect, HUDLayoutTokens.PartyRailSize, HUDLayoutTokens.PartyRailPos);
        public static void ApplySkillBarRegion(RectTransform rect) => BottomRight(rect, HUDLayoutTokens.SkillClusterSize, HUDLayoutTokens.SkillClusterInset);
        public static void ApplyHudBarsRegion(RectTransform rect) => BottomCenter(rect, HUDLayoutTokens.StatusClusterSize, HUDLayoutTokens.StatusClusterPos);
        public static void ApplyTopNavRegion(RectTransform rect) => TopRight(rect, HUDLayoutTokens.NavBarSize, HUDLayoutTokens.NavBarInset);
        public static void ApplyInteractPromptRegion(RectTransform rect) => BottomCenter(rect, new Vector2(480, 48), new Vector2(0, 120));
        public static void ApplyChatRegion(RectTransform rect) => BottomLeft(rect, HUDLayoutTokens.ChatSize, HUDLayoutTokens.ChatInset);
        public static void ApplyCharacterTabBar(RectTransform rect) => RightCenter(rect, new Vector2(72, 360), new Vector2(-12, 0));

        // Overlay widget regions @ 1920×1080
        public static void ApplyCloseButtonTopRight(RectTransform rect) => TopRight(rect, new Vector2(48, 48), new Vector2(-40, -40));
        public static void ApplyInventoryGrid(RectTransform rect) => Center(rect, new Vector2(520, 520));
        public static void ApplyInventoryTabs(RectTransform rect) => TopLeft(rect, new Vector2(320, 48), new Vector2(120, -80));
        public static void ApplyCharacterPreview(RectTransform rect) => Center(rect, new Vector2(480, 520));
        public static void ApplyCharacterStats(RectTransform rect) => RightCenter(rect, new Vector2(360, 400), new Vector2(-200, 0));
        public static void ApplyMapMarkerCreation(RectTransform rect) => Center(rect, new Vector2(120, 48));
        public static void ApplyMapMarkerRuins(RectTransform rect) { Center(rect, new Vector2(120, 48)); rect.anchoredPosition = new Vector2(180, 40); }
        public static void ApplyMapMarkerForest(RectTransform rect) { Center(rect, new Vector2(120, 48)); rect.anchoredPosition = new Vector2(-120, 120); }
        public static void ApplyWeaponGrid(RectTransform rect) => LeftCenter(rect, new Vector2(360, 600));
        public static void ApplyWeaponDetail(RectTransform rect) => RightCenter(rect, new Vector2(420, 520), new Vector2(-200, 0));
        public static void ApplyEventDayRow(RectTransform rect) => BottomCenter(rect, new Vector2(720, 120), new Vector2(0, 180));
        public static void ApplyEventCheckInBtn(RectTransform rect) => BottomCenter(rect, new Vector2(200, 52), new Vector2(0, 80));
        public static void ApplyTeamSlotRow(RectTransform rect) => Center(rect, new Vector2(800, 160));
        public static void ApplyWishPullButtons(RectTransform rect) => BottomCenter(rect, new Vector2(360, 52), new Vector2(0, 120));
        public static void ApplyWishResultPanel(RectTransform rect) => BottomCenter(rect, new Vector2(720, 160), new Vector2(0, 280));
        public static void ApplyDialoguePortrait(RectTransform rect) => BottomLeft(rect, new Vector2(200, 240), new Vector2(40, 40));
        public static void ApplyLoadingProgress(RectTransform rect) => BottomCenter(rect, new Vector2(520, 20), new Vector2(0, 200));
        public static void ApplyServerPickerPanel(RectTransform rect) => Center(rect, new Vector2(400, 320));
        public static void ApplySettingsPanel(RectTransform rect) => Center(rect, new Vector2(480, 400));

        public static void LeftCenter(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(120, 0);
        }

        public static void TopCenter(RectTransform rect, Vector2 size, Vector2 anchoredPos)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
        }

        // Start.png @ 1920×1080
        public static void ApplyMainMenuClickHit(RectTransform rect) => BottomCenter(rect, new Vector2(560, 72), new Vector2(0, 500));
        public static void ApplyMainMenuRegionHit(RectTransform rect) => BottomCenter(rect, new Vector2(220, 52), new Vector2(0, 320));
        public static void ApplyMainMenuEventHit(RectTransform rect) => BottomRight(rect, new Vector2(56, 56), new Vector2(48, 128));
        public static void ApplyMainMenuPowerHit(RectTransform rect) => BottomLeft(rect, new Vector2(56, 56), new Vector2(48, 48));
        public static void ApplyMainMenuProfileHit(RectTransform rect) => BottomRight(rect, new Vector2(56, 56), new Vector2(48, 208));
        public static void ApplyMainMenuSettingsHit(RectTransform rect) => BottomRight(rect, new Vector2(56, 56), new Vector2(48, 48));
        public static void ApplyMainMenuQuitHit(RectTransform rect) => ApplyMainMenuPowerHit(rect);
        public static void ApplyMainMenuJournalHit(RectTransform rect) => ApplyMainMenuEventHit(rect);
    }
}
