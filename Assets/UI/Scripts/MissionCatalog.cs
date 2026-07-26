using System.Collections.Generic;

namespace BES.UI
{
    public readonly struct MissionDefinition
    {
        public readonly string id;
        public readonly string cardSpriteName;
        public readonly string claimSpriteName;
        public readonly bool claimable;

        public MissionDefinition(string id, string cardSpriteName, string claimSpriteName, bool claimable)
        {
            this.id = id;
            this.cardSpriteName = cardSpriteName;
            this.claimSpriteName = claimSpriteName;
            this.claimable = claimable;
        }
    }

    public static class MissionCatalog
    {
        static readonly MissionDefinition[] Missions =
        {
            new("daily_login", "Group 427323029", "Group 427323020", true),
            new("daily_battle", "Group 427323031", "Group 427323021", true),
            new("main_story", "Group 427323033", "Group 427323022", true),
            new("weekly_training", "Group 427323032", "Group 427323023", true),
            new("event_visit", "Group 427323031", "Group 427323020", true)
        };

        public static IReadOnlyList<MissionDefinition> DefaultMissions() => Missions;
    }
}
