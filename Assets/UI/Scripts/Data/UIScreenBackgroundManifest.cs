using UnityEngine;

namespace BES.UI
{
    public enum UIScreenBackgroundId
    {
        MainMenu,
        GameplayHud,
        Inventory,
        CharacterProfile,
        CharacterPreview,
        WorldMap,
        Weapon,
        WeaponEnhance,
        WeaponRankUp,
        WeaponRefine,
        Artifacts,
        TeamSetup,
        TeamSlotPicker,
        EventCheckIn,
        Wish,
        Dialogue,
        Loading,
        PlayerProfile,
        QuestTracker
    }

    [CreateAssetMenu(fileName = "UIScreenBackgroundManifest", menuName = "BES/UI Screen Background Manifest")]
    public class UIScreenBackgroundManifest : ScriptableObject
    {
        public Sprite mainMenu;
        public Sprite gameplayHud;
        public Sprite inventory;
        public Sprite characterProfile;
        public Sprite characterPreview;
        public Sprite worldMap;
        public Sprite weapon;
        public Sprite weaponEnhance;
        public Sprite weaponRankUp;
        public Sprite weaponRefine;
        public Sprite artifacts;
        public Sprite teamSetup;
        public Sprite teamSlotPicker;
        public Sprite eventCheckIn;
        public Sprite wish;
        public Sprite dialogue;
        public Sprite loading;
        public Sprite playerProfile;
        public Sprite questTracker;

        public Sprite GetSprite(UIScreenBackgroundId id)
        {
            return id switch
            {
                UIScreenBackgroundId.MainMenu => mainMenu,
                UIScreenBackgroundId.GameplayHud => gameplayHud,
                UIScreenBackgroundId.Inventory => inventory,
                UIScreenBackgroundId.CharacterProfile => characterProfile,
                UIScreenBackgroundId.CharacterPreview => characterPreview,
                UIScreenBackgroundId.WorldMap => worldMap,
                UIScreenBackgroundId.Weapon => weapon,
                UIScreenBackgroundId.WeaponEnhance => weaponEnhance,
                UIScreenBackgroundId.WeaponRankUp => weaponRankUp,
                UIScreenBackgroundId.WeaponRefine => weaponRefine,
                UIScreenBackgroundId.Artifacts => artifacts,
                UIScreenBackgroundId.TeamSetup => teamSetup,
                UIScreenBackgroundId.TeamSlotPicker => teamSlotPicker,
                UIScreenBackgroundId.EventCheckIn => eventCheckIn,
                UIScreenBackgroundId.Wish => wish,
                UIScreenBackgroundId.Dialogue => dialogue,
                UIScreenBackgroundId.Loading => loading,
                UIScreenBackgroundId.PlayerProfile => playerProfile,
                UIScreenBackgroundId.QuestTracker => questTracker,
                _ => null
            };
        }
    }
}
