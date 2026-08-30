using UnityEngine;

namespace BES.UI
{
    /// <summary>
    /// Tọa độ HUD đo từ Main play.png @ 1920×1080 — không dùng full-screen mockup.
    /// </summary>
    public static class HUDLayoutTokens
    {
        public const float RefWidth = 1920f;
        public const float RefHeight = 1080f;

        public static readonly Vector2 PortraitChipSize = new(48f, 48f);
        public static readonly Vector2 PortraitChipPos = new(20f, -20f);

        public static readonly Vector2 MiniMapSize = new(156f, 156f);
        public static readonly Vector2 MiniMapPos = new(76f, -20f);

        public static readonly Vector2 LockBtnSize = new(28f, 28f);
        public static readonly Vector2 LockBtnPos = new(240f, -24f);

        public static readonly Vector2 QuestTrackerSize = new(420f, 72f);
        public static readonly Vector2 QuestTrackerPos = new(20f, -188f);

        public static readonly Vector2 NavBarSize = new(500f, 56f);
        public static readonly Vector2 NavBarInset = new(-20f, -20f);
        public const float NavIconSize = 48f;
        public const float NavIconSpacing = 58f;
        public const float NavRightMostX = -10f;

        public static readonly Vector2 PartyRailSize = new(200f, 360f);
        public static readonly Vector2 PartyRailPos = new(-20f, 40f);
        public static readonly Vector2 PartySlotSize = new(188f, 68f);
        public const float PartySlotSpacing = 78f;
        public static readonly Vector2 PartyNumberScreenInset = new(-16f, 0f);

        public static readonly Vector2 StatusClusterSize = new(1200f, 56f);
        public static readonly Vector2 StatusClusterPos = new(0f, 28f);
        public static readonly Vector2 HealthBarSize = new(900f, 24f);
        public static readonly Vector2 HealthBarPos = new(0f, 14f);
        public static readonly Vector2 LevelTextPos = new(-520f, 20f);
        public static readonly Vector2 RegionTextPos = new(-520f, -6f);

        public static readonly Vector2 SkillClusterSize = new(200f, 200f);
        public static readonly Vector2 SkillClusterInset = new(108f, 108f);

        public static readonly Vector2 SkillZPos = new(-24f, 138f);
        public static readonly Vector2 SkillZSize = new(48f, 48f);
        public static readonly Vector2 SkillEPos = new(-124f, 62f);
        public static readonly Vector2 SkillESize = new(64f, 64f);
        public static readonly Vector2 SkillQPos = new(-48f, 48f);
        public static readonly Vector2 SkillQSize = new(92f, 92f);

        public static readonly Vector2 ChatSize = new(220f, 44f);
        public static readonly Vector2 ChatInset = new(24f, 108f);
    }
}
