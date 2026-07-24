using UnityEngine;

namespace BES.UI
{
    [CreateAssetMenu(fileName = "HUDSpriteManifest", menuName = "BES/HUD Sprite Manifest")]
    public class HUDSpriteManifest : ScriptableObject
    {
        [Header("Mini-map")]
        public Sprite minimapMap;
        public Sprite minimapFrame;
        public Sprite minimapRing;
        public Sprite playerDot;
        public Sprite objectiveDot;
        public Sprite lockIcon;
        public Sprite lockBtnFrame;
        public Sprite chatBubbleIcon;
        public Sprite chatEnterFrame;
        public Sprite portraitChipRing;

        [Header("Status bars")]
        public Sprite hpBarBackground;
        public Sprite hpBarFill;
        public Sprite staminaBarBackground;
        public Sprite staminaBarFill;
        public Sprite manaBarBackground;
        public Sprite manaBarFill;

        [Header("Top navigation")]
        public Sprite navBarBackground;
        public Sprite navInventory;
        public Sprite navBag;
        public Sprite navCharacter;
        public Sprite navPersonal;
        public Sprite navMap;
        public Sprite navWish;
        public Sprite navTeam;
        public Sprite navBattlePass;
        public Sprite navEvent;
        public Sprite navArtifacts;
        public Sprite navWeapon;
        public Sprite settingsIcon;
        public Sprite guideLineIcon;
        public Sprite missionIcon;

        [Header("Party & skills")]
        public Sprite partySlotFrame;
        public Sprite partyPortraitDefault;
        public Sprite skillSlotFrame;
        public Sprite skillIconAttack;
        public Sprite skillIconSkill1;
        public Sprite skillIconSkill2;
        public Sprite skillIconDodge;

        [Header("Quest")]
        public Sprite questTrackerFrame;
        public Sprite questBookIcon;
        public Sprite questStarIcon;
        public Sprite compassArrow;

        [Header("Interact")]
        public Sprite interactPromptFrame;
    }
}
